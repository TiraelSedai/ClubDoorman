using System.Collections.Concurrent;
using System.Runtime.Caching;
using ClubDoorman.Models;
using ClubDoorman.Infrastructure;
using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис модерации сообщений
/// </summary>
public class ModerationService : IModerationService
{
    private readonly SpamHamClassifier _classifier;
    private readonly BadMessageManager _badMessageManager;
    private readonly IUserManager _userManager;
    private readonly AiChecks _aiChecks;
    private readonly ILogger<ModerationService> _logger;

    // Счетчики хороших сообщений для новой системы
    private readonly ConcurrentDictionary<long, int> _goodUserMessages = new();
    private readonly ConcurrentDictionary<string, int> _groupGoodUserMessages = new();
    private readonly ConcurrentDictionary<long, DateTime> _warnedUsers = new();

    public ModerationService(
        SpamHamClassifier classifier,
        BadMessageManager badMessageManager,
        IUserManager userManager,
        AiChecks aiChecks,
        ILogger<ModerationService> logger)
    {
        _classifier = classifier;
        _badMessageManager = badMessageManager;
        _userManager = userManager;
        _aiChecks = aiChecks;
        _logger = logger;
    }

    public async Task<ModerationResult> CheckMessageAsync(Message message)
    {
        var user = message.From!;
        var text = message.Text ?? message.Caption;
        var chat = message.Chat;

        // Кэшируем текст сообщения
        if (text != null)
        {
            MemoryCache.Default.Set(
                new CacheItem($"{chat.Id}_{user.Id}", text),
                new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) }
            );
        }

        // 1. Проверка блэклиста
        if (await _userManager.InBanlist(user.Id))
        {
            return new ModerationResult(ModerationAction.Ban, "Пользователь в блэклисте спамеров");
        }

        // 2. Проверка кнопок
        if (message.ReplyMarkup != null)
        {
            return new ModerationResult(ModerationAction.Ban, "Сообщение с кнопками");
        }

        // 3. Проверка Story
        if (message.Story != null)
        {
            return new ModerationResult(ModerationAction.Delete, "Сторис");
        }

        // 4. Проверка пустого текста/медиа
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Empty text/caption");
            
            // Проверяем медиа
            var mediaResult = CheckMediaContent(message, chat.Id);
            if (mediaResult != null)
                return mediaResult;

            return new ModerationResult(ModerationAction.Report, "Медиа без подписи");
        }

        // 5. Проверка известных плохих сообщений
        var isKnownBad = _badMessageManager.KnownBadMessage(text);
        _logger.LogDebug("Проверка известных плохих сообщений: текст='{Text}', известное={IsKnownBad}", 
            text.Length > 50 ? text.Substring(0, 50) + "..." : text, isKnownBad);
            
        if (isKnownBad)
        {
            _logger.LogInformation("Найдено известное спам-сообщение: '{Text}'", text);
            return new ModerationResult(ModerationAction.Ban, "Известное спам-сообщение");
        }

        // 6-10. Проверка текста
        return await CheckTextContentAsync(text, message);
    }

    public async Task<ModerationResult> CheckUserNameAsync(User user)
    {
        var fullName = Utils.FullName(user);
        
        // Проверяем длину имени
        if (fullName.Length > 75)
        {
            return new ModerationResult(ModerationAction.Ban, $"Экстремально длинное имя ({fullName.Length} символов)");
        }
        
        if (fullName.Length > 40)
        {
            return new ModerationResult(ModerationAction.Report, $"Подозрительно длинное имя ({fullName.Length} символов)");
        }

        return new ModerationResult(ModerationAction.Allow, "Имя пользователя корректно");
    }

    public Task ExecuteModerationActionAsync(Message message, ModerationResult result)
    {
        // Эта логика пока остается в Worker.cs, будет вынесена позже
        throw new NotImplementedException("Логика выполнения действий будет вынесена в следующих итерациях");
    }

    public bool IsUserApproved(long userId, long? chatId = null)
    {
        if (Config.UseNewApprovalSystem)
        {
            return _userManager.Approved(userId, chatId);
        }
        else
        {
            return _userManager.Approved(userId);
        }
    }

    public async Task IncrementGoodMessageCountAsync(User user, Chat chat)
    {
        if (Config.UseNewApprovalSystem && !Config.GlobalApprovalMode)
        {
            // Новая система, групповой режим
            var groupUserKey = $"{chat.Id}_{user.Id}";
            var goodInteractions = _groupGoodUserMessages.AddOrUpdate(groupUserKey, 1, (_, oldValue) => oldValue + 1);
            
            if (goodInteractions >= 3)
            {
                _logger.LogInformation(
                    "User {FullName} behaved well for the last {Count} messages in group {GroupTitle}, approving in this group",
                    Utils.FullName(user),
                    goodInteractions,
                    chat.Title ?? chat.Id.ToString()
                );
                
                await _userManager.Approve(user.Id, chat.Id);
                _groupGoodUserMessages.TryRemove(groupUserKey, out _);
                _warnedUsers.TryRemove(user.Id, out _);
            }
        }
        else
        {
            // Старая система или новая система в глобальном режиме
            var goodInteractions = _goodUserMessages.AddOrUpdate(user.Id, 1, (_, oldValue) => oldValue + 1);
            
            if (goodInteractions >= 3)
            {
                _logger.LogInformation(
                    "User {FullName} behaved well for the last {Count} messages, approving {Mode}",
                    Utils.FullName(user),
                    goodInteractions,
                    Config.GlobalApprovalMode ? "globally" : "in old system"
                );
                
                await _userManager.Approve(user.Id, Config.GlobalApprovalMode ? null : chat.Id);
                _goodUserMessages.TryRemove(user.Id, out _);
                _warnedUsers.TryRemove(user.Id, out _);
            }
        }
    }

    private ModerationResult? CheckMediaContent(Message message, long chatId)
    {
        var hasPhotoOrVideo = message.Photo != null || message.Video != null;
        var hasStickerOrDocument = message.Sticker != null || message.Document != null;
        var chatType = ChatSettingsManager.GetChatType(chatId);
        var isAnnouncement = chatType == "announcement";

        // Не репортим картинки и видео если фильтрация отключена
        if (Config.IsMediaFilteringDisabledForChat(chatId) && hasPhotoOrVideo && !hasStickerOrDocument)
            return null;

        // Стикеры и документы всегда блокируем в неутвержденных сообщениях
        if (!isAnnouncement && hasStickerOrDocument)
        {
            return new ModerationResult(ModerationAction.Delete, "В первых трёх сообщениях нельзя отправлять стикеры или документы");
        }

        // Картинки и видео блокируем только если фильтрация не отключена
        if (!Config.IsMediaFilteringDisabledForChat(chatId) && !isAnnouncement && hasPhotoOrVideo)
        {
            return new ModerationResult(ModerationAction.Delete, "В первых трёх сообщениях нельзя отправлять картинки или видео");
        }

        return null;
    }

    private async Task<ModerationResult> CheckTextContentAsync(string text, Message message)
    {
        var chatType = ChatSettingsManager.GetChatType(message.Chat.Id);
        var isAnnouncement = chatType == "announcement";

        // 6. Проверка эмодзи
        var tooManyEmojis = SimpleFilters.TooManyEmojis(text);
        _logger.LogDebug("Проверка эмодзи: текст='{Text}', многовато={TooMany}, объявление={IsAnnouncement}", 
            text.Length > 50 ? text.Substring(0, 50) + "..." : text, tooManyEmojis, isAnnouncement);
            
        if (!isAnnouncement && tooManyEmojis)
        {
            _logger.LogInformation("Слишком много эмодзи в тексте: '{Text}'", text);
            return new ModerationResult(ModerationAction.Delete, "В этом сообщении многовато эмоджи");
        }

        var normalized = TextProcessor.NormalizeText(text);

        // 7. Проверка lookalike символов
        var lookalike = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(normalized);
        if (lookalike.Count > 2)
        {
            var tailMessage = lookalike.Count > 5 ? ", и другие" : "";
            var reason = $"Были найдены слова маскирующиеся под русские: {string.Join(", ", lookalike.Take(5))}{tailMessage}";
            
            if (Config.LookAlikeAutoBan)
            {
                return new ModerationResult(ModerationAction.Ban, reason);
            }
            
            return new ModerationResult(ModerationAction.Delete, reason);
        }

        // 8. Проверка стоп-слов
        var hasStopWords = SimpleFilters.HasStopWords(normalized);
        _logger.LogDebug("Проверка стоп-слов: текст='{Text}', найдены={HasStopWords}", normalized, hasStopWords);
        
        if (hasStopWords)
        {
            _logger.LogInformation("Найдены стоп-слова в тексте: '{Text}'", normalized);
            return new ModerationResult(ModerationAction.Delete, "В этом сообщении есть стоп-слова");
        }

        // 9. ML классификация спама
        var (spam, score) = await _classifier.IsSpam(normalized);
        _logger.LogDebug("ML анализ: текст='{Text}', спам={Spam}, скор={Score}", normalized, spam, score);
        
        if (spam)
        {
            _logger.LogInformation("ML классификатор определил спам: '{Text}', скор={Score}", normalized, score);
            return new ModerationResult(ModerationAction.Delete, $"ML решил что это спам, скор {score}", score);
        }

        // 10. Проверка низкой уверенности в ham
        if (score > -0.6 && Config.LowConfidenceHamForward)
        {
            return new ModerationResult(ModerationAction.RequireManualReview, 
                $"Классификатор думает что это НЕ спам, но конфиденс низкий: скор {score}", score);
        }

        // Все проверки пройдены - сообщение можно разрешить
        return new ModerationResult(ModerationAction.Allow, "Сообщение прошло все проверки", score);
    }
} 
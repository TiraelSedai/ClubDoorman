using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text.Json;

namespace ClubDoorman;

internal sealed class Worker(
    ILogger<Worker> logger,
    SpamHamClassifier classifier,
    IUserManager userManager,
    BadMessageManager badMessageManager
) : BackgroundService
{
    private sealed record CaptchaInfo(
        long ChatId,
        string? ChatTitle,
        DateTime Timestamp,
        User User,
        int CorrectAnswer,
        CancellationTokenSource Cts,
        Message? UserJoinedMessage
    );

    private sealed class Stats(string? Title)
    {
        public string? ChatTitle = Title;
        public int StoppedCaptcha;
        public int BlacklistBanned;
        public int KnownBadMessage;
        public int LongNameBanned;
    }

    private readonly ConcurrentDictionary<string, CaptchaInfo> _captchaNeededUsers = new();
    // Для старой системы - глобальный счетчик хороших сообщений
    private readonly ConcurrentDictionary<long, int> _goodUserMessages = new();
    // Для новой системы - счетчик хороших сообщений по группам
    private readonly ConcurrentDictionary<string, int> _groupGoodUserMessages = new();
    private readonly ConcurrentDictionary<long, DateTime> _warnedUsers = new();
    private readonly TelegramBotClient _bot = new(Config.BotApi);
    private readonly ConcurrentDictionary<long, Stats> _stats = new();
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));
    private readonly PeriodicTimer _banlistRefreshTimer = new(TimeSpan.FromHours(12));
    private readonly PeriodicTimer _membersCountUpdateTimer = new(TimeSpan.FromHours(8));
    private readonly ILogger<Worker> _logger = logger;
    private readonly SpamHamClassifier _classifier = classifier;
    private readonly IUserManager _userManager = userManager;
    private readonly BadMessageManager _badMessageManager = badMessageManager;
    private readonly GlobalStatsManager _globalStatsManager = new();
    private AiChecks _aiChecks = null!;
    private User _me = default!;
    private static readonly ConcurrentDictionary<string, byte> _joinedUserFlags = new();
    
    // Группы, где не показывать рекламу VPN (из .env NO_VPN_AD_GROUPS)
    private static readonly HashSet<long> NoVpnAdGroups = 
        (Environment.GetEnvironmentVariable("NO_VPN_AD_GROUPS") ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(id => long.TryParse(id.Trim(), out var val) ? val : (long?)null)
        .Where(id => id.HasValue)
        .Select(id => id.Value)
        .ToHashSet();

    static Worker()
    {
        var envVar = Environment.GetEnvironmentVariable("NO_VPN_AD_GROUPS");
        Console.WriteLine($"[DEBUG] NO_VPN_AD_GROUPS env var: '{envVar}'");
        Console.WriteLine($"[DEBUG] Loaded {NoVpnAdGroups.Count} groups without VPN ads: [{string.Join(", ", NoVpnAdGroups)}]");
    }

    private async Task CaptchaLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), token);
            _ = BanNoCaptchaUsers();
        }
    }

    private async Task RefreshBanlistLoop(CancellationToken token)
    {
        // Обновляем банлист сразу после запуска бота
        try 
        {
            _logger.LogInformation("Начальное обновление банлиста из lols.bot при старте бота");
            await _userManager.RefreshBanlist();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при начальном обновлении банлиста");
        }

        while (await _banlistRefreshTimer.WaitForNextTickAsync(token))
        {
            try
            {
                _logger.LogInformation("Обновляем банлист из lols.bot");
                await _userManager.RefreshBanlist();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении банлиста");
            }
        }
    }

    private async Task UpdateMembersCountLoop(CancellationToken stoppingToken)
    {
        while (await _membersCountUpdateTimer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await _globalStatsManager.UpdateAllMembersAsync(_bot);
                _logger.LogInformation("Обновлено количество участников во всех чатах для статистики");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при обновлении количества участников чатов");
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ChatSettingsManager.InitConfigFileIfMissing();
        _ = CaptchaLoop(stoppingToken);
        _ = ReportStatistics(stoppingToken);
        _ = RefreshBanlistLoop(stoppingToken);
        _ = UpdateMembersCountLoop(stoppingToken);
        _classifier.Touch();
        const string offsetPath = "data/offset.txt";
        var offset = 0;
        if (System.IO.File.Exists(offsetPath))
        {
            var lines = await System.IO.File.ReadAllLinesAsync(offsetPath, stoppingToken);
            if (lines.Length > 0 && int.TryParse(lines[0], out offset))
                _logger.LogDebug("offset read ok");
        }

        _me = await _bot.GetMe(cancellationToken: stoppingToken);
        
        // Инициализируем AI проверки после создания бота
        _aiChecks = new AiChecks(_bot, _logger);
        
        _ = Task.Run(async () => {
            try {
                await _globalStatsManager.UpdateAllMembersAsync(_bot);
                await _globalStatsManager.UpdateZeroMemberChatsAsync(_bot);
                _logger.LogInformation("Первичное обновление количества участников во всех чатах для статистики");
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Ошибка при первичном обновлении количества участников");
            }
        });

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                offset = await UpdateLoop(offset, stoppingToken);
                if (offset % 100 == 0)
                    await System.IO.File.WriteAllTextAsync(offsetPath, offset.ToString(), stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "ExecuteAsync");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }

    private async Task<int> UpdateLoop(int offset, CancellationToken stoppingToken)
    {
        var updates = await _bot.GetUpdates(
            offset,
            limit: 100,
            timeout: 100,
            allowedUpdates: [UpdateType.Message, UpdateType.EditedMessage, UpdateType.ChatMember, UpdateType.CallbackQuery],
            cancellationToken: stoppingToken
        );
        if (updates.Length == 0)
            return offset;
        offset = updates.Max(x => x.Id) + 1;
        string? mediaGroupId = null;
        foreach (var update in updates)
        {
            try
            {
                var prevMediaGroup = mediaGroupId;
                mediaGroupId = update.Message?.MediaGroupId;
                if (prevMediaGroup != null && prevMediaGroup == mediaGroupId)
                {
                    _logger.LogDebug("2+ message from an album, it could not have any text/caption, skip");
                    continue;
                }
                await HandleUpdate(update, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "UpdateLoop");
            }
        }
        return offset;
    }

    private async Task HandleUpdate(Update update, CancellationToken stoppingToken)
    {
        var chat = update.EditedMessage?.Chat ?? update.Message?.Chat ?? update.ChatMember?.Chat;
        if (chat != null)
            ChatSettingsManager.EnsureChatInConfig(chat.Id, chat.Title);

        if (update.CallbackQuery != null)
        {
            await HandleCallback(update);
            return;
        }
        if (update.ChatMember != null)
        {
            if (update.ChatMember.From.Id == _me.Id)
                return;
            await HandleChatMemberUpdated(update);
            return;
        }

        var message = update.EditedMessage ?? update.Message;
        if (message == null)
            return;
        
        // Обработка команды /start
        if (message.Text?.Trim().ToLower() == "/start")
        {
            if (message.Chat.Type == ChatType.Private)
            {
                var about =
$"""
<b>👋 Привет! Я антиспам-бот для Telegram-групп</b>

━━━━━━━━━━━━━━━

<b>🚀 Как я работаю?</b>

Просто добавьте меня в группу администратором — настройка не требуется.

<b>🔄 Алгоритм работы:</b>

<b>1)</b> Новый участник → проверка спам-списков, тихая блокировка известных спамеров

<b>2)</b> Капча 60 секунд. Не решил — блок с очисткой сообщений

<b>3)</b> Первые 3 сообщения: только текст, без медиа/ссылок/эмодзи

<b>4)</b> Подозрение на спам → предупреждение + удаление сообщения

<b>5)</b> 3 хороших сообщения → полная свобода!

<a href="https://telegra.ph/GateTroitsBot-04-19">📖 Подробнее</a>

━━━━━━━━━━━━━━━

<b>⚡ Подключение за 3 шага:</b>

1️⃣ Добавьте бота в группу
2️⃣ Назначьте администратором
3️⃣ Готово! Бот уже работает

━━━━━━━━━━━━━━━

<b>🌟 ПЛАТНЫЕ ПЛАНЫ:</b>

🔥 <b>БЕЗ РЕКЛАМЫ</b> — всего <b>$5 навсегда</b> 
   Отключение рекламы в одной группе

💎 <b>ПРЕМИУМ</b> — <b>$12/год</b>
   Отдельная копия бота с персональным ML-датасетом под ваши группы (до 100)

🌐 💳 ДЛЯ ЗАКАЗА НАПИСАТЬ @momai 

━━━━━━━━━━━━━━━

<b>💡 Полезные советы:</b>

👤 <b>Новым участникам:</b>
   • Первые 3 сообщения — только текст
   • Без ссылок, эмодзи, картинок

👑 <b>Администраторам:</b>
   • Права админа боту можно дать через 3-4 дня
   • Так, бот соберёт статистику и одобрит активных, не мешая старожилам.

━━━━━━━━━━━━━━━

<b>📎 Дополнительно:</b>

📖 <a href="https://telegra.ph/GateTroitsBot-04-19">Документация</a>
💻 <a href="https://github.com/momai/ClubDoorman">Исходный код</a>

<b>🧼 Пусть в вашем чате будет чисто и спокойно!</b>
""";
                await _bot.SendMessage(message.Chat.Id, about, parseMode: ParseMode.Html);
            }
            return;
        }
        
        // Игнорировать полностью отключённые чаты
        if (Config.DisabledChats.Contains(chat.Id))
            return;
        
        // Проверяем и удаляем сообщения о том, что бот кого-то исключил
        if (message.LeftChatMember != null && message.From?.Id == _me.Id)
        {
            try 
            {
                await _bot.DeleteMessage(chat.Id, message.MessageId, stoppingToken);
                _logger.LogDebug("Удалено сообщение о бане/исключении пользователя");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Не удалось удалить сообщение о бане/исключении");
            }
            return;
        }
        
        if (message.NewChatMembers != null && chat.Id != Config.AdminChatId)
        {
            foreach (var newUser in message.NewChatMembers.Where(x => !x.IsBot))
            {
                var joinKey = $"joined_{chat.Id}_{newUser.Id}";
                if (!_joinedUserFlags.ContainsKey(joinKey))
                {
                    _logger.LogInformation("==================== НОВЫЙ УЧАСТНИК ====================\nПользователь {User} (id={UserId}, username={Username}) зашел в группу '{ChatTitle}' (id={ChatId})\n========================================================", 
                        (newUser.FirstName + (string.IsNullOrEmpty(newUser.LastName) ? "" : " " + newUser.LastName)), newUser.Id, newUser.Username ?? "-", chat.Title ?? "-", chat.Id);
                    _joinedUserFlags.TryAdd(joinKey, 1);
                    _ = Task.Run(async () => { await Task.Delay(15000); _joinedUserFlags.TryRemove(joinKey, out _); });
                }
                await IntroFlow(message, newUser);
            }
            return;
        }

        if (chat.Id == Config.AdminChatId)
        {
            await AdminChatMessage(message);
            return;
        }

        if (message.SenderChat != null)
        {
            if (message.SenderChat.Id == chat.Id)
                return;
            if (ChatSettingsManager.GetChatType(chat.Id) == "announcement")
                return;
            // to get linked_chat_id we need ChatFullInfo
            var chatFull = await _bot.GetChat(chat, stoppingToken);
            var linked = chatFull.LinkedChatId;
            if (linked != null && linked == message.SenderChat.Id)
                return;

            if (Config.ChannelAutoBan)
            {
                try
                {
                    var fwd = await _bot.ForwardMessage(Config.AdminChatId, chat, message.MessageId, cancellationToken: stoppingToken);
                    await _bot.DeleteMessage(chat, message.MessageId, stoppingToken);
                    await _bot.BanChatSenderChat(chat, message.SenderChat.Id, stoppingToken);
                    await _bot.SendMessage(
                        Config.AdminChatId,
                        $"Сообщение удалено, в чате {chat.Title} забанен канал {message.SenderChat.Title}",
                        replyParameters: fwd,
                        cancellationToken: stoppingToken
                    );
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Unable to ban");
                    await _bot.SendMessage(
                        Config.AdminChatId,
                        $"Не могу удалить или забанить в чате {chat.Title} сообщение от имени канала {message.SenderChat.Title}. Не хватает могущества?",
                        cancellationToken: stoppingToken
                    );
                }
                return;
            }

            if (ChatSettingsManager.GetChatType(chat.Id) != "announcement")
                await DontDeleteButReportMessage(message, message.From!, stoppingToken);
            return;
        }

        var user = message.From!;
        var text = message.Text ?? message.Caption;

        if (text != null)
            MemoryCache.Default.Set(
                new CacheItem($"{chat.Id}_{user.Id}", text),
                new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) }
            );

        var key = UserToKey(chat.Id, user);
        if (_captchaNeededUsers.ContainsKey(key))
        {
            await _bot.DeleteMessage(chat.Id, message.MessageId, stoppingToken);
            return;
        }

        if (IsUserApproved(user.Id, message.Chat.Id))
            return;

        // Определяем, является ли это сообщением из обсуждения канала
        var isChannelDiscussion = await IsChannelDiscussion(chat, message);
        var userType = isChannelDiscussion ? "из обсуждения канала" : "новый участник";
        
        _logger.LogInformation("==================== СООБЩЕНИЕ ОТ НЕОДОБРЕННОГО ====================\n{UserType}: {User} (id={UserId}, username={Username}) в '{ChatTitle}' (id={ChatId})\nСообщение: {Text}\n================================================================", 
            userType, Utils.FullName(user), user.Id, user.Username ?? "-", chat.Title ?? "-", chat.Id, text?.Substring(0, Math.Min(text.Length, 100)) ?? "[медиа]");

        // Автоматически добавляем чат в конфиг, если его там нет
        ChatSettingsManager.EnsureChatInConfig(chat.Id, chat.Title);

        // At this point we are believing we see first-timers, and we need to check for spam
        var name = await _userManager.GetClubUsername(user.Id);
        if (!string.IsNullOrEmpty(name))
        {
            _logger.LogDebug("User is {Name} from club", name);
            return;
        }
        if (await _userManager.InBanlist(user.Id))
        {
            if (Config.BlacklistAutoBan)
            {
                var stats = _stats.GetOrAdd(chat.Id, new Stats(chat.Title));
                Interlocked.Increment(ref stats.BlacklistBanned);
                await _bot.BanChatMember(chat.Id, user.Id, revokeMessages: true, cancellationToken: stoppingToken);
                
                // Удаляем текущее сообщение пользователя
                await _bot.DeleteMessage(chat.Id, message.MessageId, stoppingToken);
                
                // Проверяем, является ли сообщение о входе в чат
                if (message.NewChatMembers != null)
                {
                    _logger.LogDebug("Удаляем сообщение о входе в чат пользователя из блэклиста");
                }
            }
            else
            {
                const string reason = "Пользователь в блеклисте спамеров";
                await DeleteAndReportMessage(message, reason, stoppingToken);
            }
            return;
        }

        if (message.ReplyMarkup != null)
        {
            await AutoBan(message, "Сообщение с кнопками", stoppingToken);
            return;
        }
        if (message.Story != null)
        {
            await DeleteAndReportMessage(message, "Сторис", stoppingToken);
            return;
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Empty text/caption");
            // Не репортим медиа (включая пересланные) в announcement-группах
            if (ChatSettingsManager.GetChatType(chat.Id) == "announcement" && (message.Photo != null || message.Sticker != null || message.Document != null || message.Video != null))
                return;
            await DontDeleteButReportMessage(message, user, stoppingToken);
            return;
        }
        if (_badMessageManager.KnownBadMessage(text))
        {
            await HandleBadMessage(message, user, stoppingToken);
            return;
        }
        var chatType = ChatSettingsManager.GetChatType(chat.Id);
        if (chatType == "announcement")
            return;
        var isAnnouncement = chatType == "announcement";
        if (!isAnnouncement && SimpleFilters.TooManyEmojis(text))
        {
            const string reason = "В этом сообщении многовато эмоджи";
            await DeleteAndReportMessage(message, reason, stoppingToken);
            return;
        }
        if (!isAnnouncement && (message.Photo != null || message.Sticker != null || message.Document != null || message.Video != null))
        {
            const string reason = "В первых трёх сообщениях нельзя отправлять картинки, стикеры или документы";
            await DeleteAndReportMessage(message, reason, stoppingToken);
            return;
        }
        if (isAnnouncement && (message.Photo != null || message.Sticker != null || message.Document != null || message.Video != null))
        {
            // В announcement-группах не репортим медиа
            return;
        }

        var normalized = TextProcessor.NormalizeText(text);

        var lookalike = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(normalized);
        if (lookalike.Count > 2)
        {
            var tailMessage = lookalike.Count > 5 ? ", и другие" : "";
            var reason = $"Были найдены слова маскирующиеся под русские: {string.Join(", ", lookalike.Take(5))}{tailMessage}";
            if (!Config.LookAlikeAutoBan)
            {
                await DeleteAndReportMessage(message, reason, stoppingToken);
                return;
            }

            await AutoBan(message, reason, stoppingToken);
            return;
        }

        if (SimpleFilters.HasStopWords(normalized))
        {
            const string reason = "В этом сообщении есть стоп-слова";
            await DeleteAndReportMessage(message, reason, stoppingToken);
            return;
        }
        var (spam, score) = await _classifier.IsSpam(normalized);
        if (spam)
        {
            var reason = $"ML решил что это спам, скор {score}";
            await DeleteAndReportMessage(message, reason, stoppingToken);
            return;
        }
        // else - ham
        if (score > -0.6 && Config.LowConfidenceHamForward)
        {
            var forward = await _bot.ForwardMessage(Config.AdminChatId, chat.Id, message.MessageId, cancellationToken: stoppingToken);
            var postLink = LinkToMessage(chat, message.MessageId);
            var callbackDataBan = $"ban_{message.Chat.Id}_{user.Id}";
            // Сохраняем сообщение для обработки callback'а (удаление и бан)
            MemoryCache.Default.Add(callbackDataBan, message, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });
            await _bot.SendMessage(
                Config.AdminChatId,
                $"Классифаер думает, что это НЕ спам, но конфиденс низкий: скор {score}. Хорошая идея — добавить сообщение в датасет.{Environment.NewLine}Юзер {FullName(user.FirstName, user.LastName)} из чата {chat.Title}{Environment.NewLine}{postLink}",
                replyParameters: forward,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new InlineKeyboardButton("удалить и забанить") { CallbackData = callbackDataBan },
                    new InlineKeyboardButton("🥰 свой") { CallbackData = $"approve_{user.Id}" }
                }),
                cancellationToken: stoppingToken
            );
        }


        _logger.LogDebug("Classifier thinks its ham, score {Score}", score);

        // ========== AI ПРОВЕРКА ПРОФИЛЕЙ ==========
        // Для пользователей из обсуждений каналов и обычных групп где включены AI проверки
        if (Config.OpenRouterApi != null && Config.IsAiEnabledForChat(chat.Id))
        {
            try 
            {
                var (attention, photo, bio) = await _aiChecks.GetAttentionBaitProbability(user);
                _logger.LogDebug("AI проверка профиля пользователя {UserId}: {Probability}", user.Id, attention.Probability);
                
                if (attention.Probability >= Consts.LlmLowProbability)
                {
                    var keyboard = new List<InlineKeyboardButton>
                    {
                        new(Consts.BanButton) { CallbackData = $"ban_{message.Chat.Id}_{user.Id}" },
                        new(Consts.OkButton) { CallbackData = $"aiOk_{user.Id}" }
                    };

                    ReplyParameters? replyParams = null;
                    if (photo.Length != 0)
                    {
                        using var ms = new MemoryStream(photo);
                        var photoMsg = await _bot.SendPhoto(
                            Config.AdminChatId,
                            new InputFileStream(ms),
                            $"{bio}{Environment.NewLine}Сообщение:{Environment.NewLine}{text}",
                            cancellationToken: stoppingToken
                        );
                        replyParams = photoMsg;
                    }

                    var isHighRisk = attention.Probability >= Consts.LlmHighProbability;
                    var action = isHighRisk ? "Даём ридонли на 10 минут" : "Требует ручной проверки";
                    var at = user.Username == null ? "" : $" @{user.Username} ";
                    
                    await _bot.SendMessage(
                        Config.AdminChatId,
                        $"🤖 AI: Вероятность спам-профиля {attention.Probability * 100:F1}%. {action}{Environment.NewLine}{attention.Reason}{Environment.NewLine}Юзер {Utils.FullName(user)}{at} из чата {chat.Title}{Environment.NewLine}{LinkToMessage(chat, message.MessageId)}",
                        replyMarkup: new InlineKeyboardMarkup(keyboard),
                        replyParameters: replyParams,
                        cancellationToken: stoppingToken
                    );

                    // При высокой вероятности спама - автоматическая модерация
                    if (isHighRisk)
                    {
                        await _bot.DeleteMessage(chat, message.MessageId, cancellationToken: stoppingToken);
                        await _bot.RestrictChatMember(
                            chat.Id,
                            user.Id,
                            new ChatPermissions(false),
                            untilDate: DateTime.UtcNow.AddMinutes(10),
                            cancellationToken: stoppingToken
                        );
                        
                        _logger.LogInformation("AI автомодерация: пользователь {UserId} получил ридонли на 10 минут (вероятность спама {Probability})", user.Id, attention.Probability);
                        return; // Прерываем обработку
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Ошибка при AI проверке профиля пользователя {UserId}", user.Id);
            }
        }
        // ========== КОНЕЦ AI ПРОВЕРКИ ==========

        // Now we need a mechanism for users who have been writing non-spam for some time
        if (Config.UseNewApprovalSystem && !Config.GlobalApprovalMode)
        {
            // Новая система, групповой режим
            var groupUserKey = $"{message.Chat.Id}_{user.Id}";
            var goodInteractions = _groupGoodUserMessages.AddOrUpdate(groupUserKey, 1, (_, oldValue) => oldValue + 1);
            if (goodInteractions >= 3)
            {
                _logger.LogInformation(
                    "User {FullName} behaved well for the last {Count} messages in group {GroupTitle}, approving in this group",
                    FullName(user.FirstName, user.LastName),
                    goodInteractions,
                    message.Chat.Title ?? message.Chat.Id.ToString()
                );
                await _userManager.Approve(user.Id, message.Chat.Id);
                _groupGoodUserMessages.TryRemove(groupUserKey, out _);
                // --- Новая логика: сбрасываем предупреждение для юзера ---
                _warnedUsers.TryRemove(user.Id, out _);
                // --- Конец новой логики ---
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
                    FullName(user.FirstName, user.LastName),
                    goodInteractions,
                    Config.GlobalApprovalMode ? "globally" : "in old system"
                );
                await _userManager.Approve(user.Id, Config.GlobalApprovalMode ? null : message.Chat.Id);
                _goodUserMessages.TryRemove(user.Id, out _);
                // --- Новая логика: сбрасываем предупреждение для юзера ---
                _warnedUsers.TryRemove(user.Id, out _);
                // --- Конец новой логики ---
            }
        }
    }

    private async Task AutoBan(Message message, string reason, CancellationToken stoppingToken)
    {
        var user = message.From;
        var forward = await _bot.ForwardMessage(
            new ChatId(Config.AdminChatId),
            message.Chat.Id,
            message.MessageId,
            cancellationToken: stoppingToken
        );
        await _bot.SendMessage(
            Config.AdminChatId,
            $"Авто-бан: {reason}{Environment.NewLine}Юзер {FullName(user.FirstName, user.LastName)} из чата {message.Chat.Title}{Environment.NewLine}{LinkToMessage(message.Chat, message.MessageId)}",
            replyParameters: forward,
            cancellationToken: stoppingToken
        );
        await _bot.DeleteMessage(message.Chat, message.MessageId, cancellationToken: stoppingToken);
        await _bot.BanChatMember(message.Chat, user.Id, revokeMessages: false, cancellationToken: stoppingToken);
        _globalStatsManager.IncBan(message.Chat.Id, message.Chat.Title ?? "");
        if (_userManager.RemoveApproval(user.Id, message.Chat.Id, removeAll: true))
        {
            await _bot.SendMessage(
                Config.AdminChatId,
                $"⚠️ Пользователь {FullName(user.FirstName, user.LastName)} удален из списка одобренных после автобана",
                cancellationToken: stoppingToken
            );
        }
    }

    private async Task HandleBadMessage(Message message, User user, CancellationToken stoppingToken)
    {
        try
        {
            var chat = message.Chat;
            var stats = _stats.GetOrAdd(chat.Id, new Stats(chat.Title));
            Interlocked.Increment(ref stats.KnownBadMessage);
            await _bot.DeleteMessage(chat, message.MessageId, stoppingToken);
            await _bot.BanChatMember(chat.Id, user.Id, cancellationToken: stoppingToken);
            _globalStatsManager.IncBan(chat.Id, chat.Title ?? "");
            if (_userManager.RemoveApproval(user.Id, message.Chat.Id, removeAll: true))
            {
                await _bot.SendMessage(
                    Config.AdminChatId,
                    $"⚠️ Пользователь {FullName(user.FirstName, user.LastName)} удален из списка одобренных после бана за спам",
                    cancellationToken: stoppingToken
                );
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to ban");
        }
    }

    private async Task HandleCallback(Update update)
    {
        var cb = update.CallbackQuery;
        Debug.Assert(cb != null);
        var cbData = cb.Data;
        if (cbData == null)
            return;
        var message = cb.Message;
        if (message == null || message.Chat.Id == Config.AdminChatId)
            await HandleAdminCallback(cbData, cb);
        else
            await HandleCaptchaCallback(update);
    }

    private async Task HandleCaptchaCallback(Update update)
    {
        var cb = update.CallbackQuery;
        Debug.Assert(cb != null);
        var cbData = cb.Data;
        if (cbData == null)
            return;
        var message = cb.Message;
        Debug.Assert(message != null);

        //$"cap_{user.Id}_{x}"
        var split = cbData.Split('_');
        if (split.Length < 3)
            return;
        if (split[0] != "cap")
            return;
        if (!long.TryParse(split[1], out var userId))
            return;
        if (!int.TryParse(split[2], out var chosen))
            return;
        // Prevent other people from ruining the flow
        if (cb.From.Id != userId)
        {
            await _bot.AnswerCallbackQuery(cb.Id);
            return;
        }

        var chat = message.Chat;
        var key = UserToKey(chat.Id, cb.From);
        var ok = _captchaNeededUsers.TryRemove(key, out var info);
        await _bot.DeleteMessage(chat, message.MessageId);
        if (!ok)
        {
            _logger.LogWarning("{Key} was not found in the dictionary _captchaNeededUsers", key);
            return;
        }
        Debug.Assert(info != null);
        await info.Cts.CancelAsync();
        if (info.CorrectAnswer != chosen)
        {
            // Логируем неуспешное прохождение капчи
            _logger.LogInformation("==================== КАПЧА НЕ ПРОЙДЕНА ====================\nПользователь {User} (id={UserId}) не прошёл капчу в группе '{ChatTitle}' (id={ChatId})\n===========================================================", info.User.FirstName + (string.IsNullOrEmpty(info.User.LastName) ? "" : " " + info.User.LastName), info.User.Id, info.ChatTitle ?? "-", info.ChatId);
            var stats = _stats.GetOrAdd(chat.Id, new Stats(chat.Title));
            Interlocked.Increment(ref stats.StoppedCaptcha);
            await _bot.BanChatMember(chat, userId, DateTime.UtcNow + TimeSpan.FromMinutes(20), revokeMessages: false);
            if (info.UserJoinedMessage != null)
                await _bot.DeleteMessage(chat, info.UserJoinedMessage.MessageId);
            UnbanUserLater(chat, userId);
        }
        else
        {
            // Логируем успешное прохождение капчи
            _logger.LogInformation("==================== КАПЧА ПРОЙДЕНА ====================\nПользователь {User} (id={UserId}) успешно прошёл капчу в группе '{ChatTitle}' (id={ChatId}) — показываем приветствие\n========================================================", cb.From.FirstName + (string.IsNullOrEmpty(cb.From.LastName) ? "" : " " + cb.From.LastName), cb.From.Id, chat.Title ?? "-", chat.Id);
            // Приветственное сообщение: разное для обычных и announcement чатов
            var displayName = !string.IsNullOrEmpty(cb.From.FirstName)
                ? System.Net.WebUtility.HtmlEncode(FullName(cb.From.FirstName, cb.From.LastName))
                : (!string.IsNullOrEmpty(cb.From.Username) ? "@" + cb.From.Username : "гость");
            var mention = $"<a href=\"tg://user?id={cb.From.Id}\">{displayName}</a>";
            string greetMsg;
            
            // Реклама VPN для всех приветственных сообщений (если группа не в исключениях)
            var isNoAdGroup = NoVpnAdGroups.Contains(chat.Id);
            Console.WriteLine($"[DEBUG] Chat {chat.Id} ({chat.Title}) - No VPN ad: {isNoAdGroup}");
            var vpnAd = isNoAdGroup ? "" : "\n\n\n🌐 <b>Твой VPN</b> — @vpn_momai_dev_bot \n <i>Два дня бесплатно</i>";
            
            if (ChatSettingsManager.GetChatType(chat.Id) == "announcement")
            {
                greetMsg = $"👋 {mention}\n\n<b>Внимание:</b> первые три сообщения проходят антиспам-проверку, ваше объявление может быть удалено.{vpnAd}";
            }
            else
            {
                greetMsg = $"👋 {mention}\n\n<b>Внимание!</b> первые три сообщения проходят антиспам-проверку, эмодзи, изображения и реклама запрещены — они могут удаляться автоматически.\nПишите только <b>текст</b>.{vpnAd}";
            }
            var sent = await _bot.SendMessage(chat.Id, greetMsg, parseMode: ParseMode.Html);
            DeleteMessageLater(sent, TimeSpan.FromSeconds(20));
        }
    }

    private readonly List<string> _namesBlacklist = ["p0rn", "porn", "порн", "п0рн", "pоrn", "пoрн", "bot"];

    private async Task BanUserForLongName(
        Message? userJoinMessage,
        User user,
        string fullName,
        TimeSpan? banDuration,
        string banType,
        string nameDescription,
        Chat? chat = default)
    {
        try
        {
            chat = userJoinMessage?.Chat ?? chat;
            Debug.Assert(chat != null);
            
            // Баним пользователя (если banDuration null - бан навсегда)
            await _bot.BanChatMember(
                chat.Id, 
                user.Id,
                banDuration.HasValue ? DateTime.UtcNow + banDuration.Value : null,
                revokeMessages: true  // Удаляем все сообщения пользователя
            );
            
            // Удаляем из списка одобренных только при перманентном бане
            if (!banDuration.HasValue)
                _userManager.RemoveApproval(user.Id);
            
            // Удаляем сообщение о входе
            if (userJoinMessage != null)
            {
                await _bot.DeleteMessage(userJoinMessage.Chat.Id, (int)userJoinMessage.MessageId);
            }

            // Логируем для статистики
            var stats = _stats.GetOrAdd(chat.Id, new Stats(chat.Title));
            Interlocked.Increment(ref stats.LongNameBanned);

            // Уведомляем админов
            await _bot.SendMessage(
                Config.AdminChatId,
                $"{banType} в чате *{chat.Title}* за {nameDescription} длинное имя пользователя ({fullName.Length} символов):\n`{Markdown.Escape(fullName)}`",
                parseMode: ParseMode.Markdown
            );
            _globalStatsManager.IncBan(chat.Id, chat.Title ?? "");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to ban user with long username");
        }
    }

    private async ValueTask IntroFlow(Message? userJoinMessage, User user, Chat? chat = default)
    {
        // Проверяем длину имени
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        
        // Проверяем длину имени для обоих случаев
        if (fullName.Length > 40)
        {
            var isPermanent = fullName.Length > 75;
            await BanUserForLongName(
                userJoinMessage,
                user,
                fullName,
                isPermanent ? null : TimeSpan.FromMinutes(10),
                isPermanent ? "🚫 Перманентный бан" : "Автобан на 10 минут",
                isPermanent ? "экстремально" : "подозрительно",
                chat
            );
            return;
        }

        _logger.LogDebug("Intro flow {@User}", user);
        
        // Проверяем, является ли пользователь участником клуба
        var clubUser = await _userManager.GetClubUsername(user.Id);
        if (clubUser != null)
        {
            _logger.LogDebug("User is {Name} from club", clubUser);
            return;
        }

        chat = userJoinMessage?.Chat ?? chat;
        Debug.Assert(chat != null);
        var chatId = chat.Id;

        if (await BanIfBlacklisted(user, chat, userJoinMessage))
            return;

        var key = UserToKey(chatId, user);
        if (_captchaNeededUsers.ContainsKey(key))
        {
            _logger.LogDebug("This user is already awaiting captcha challenge");
            return;
        }

        const int challengeLength = 8;
        var correctAnswerIndex = Random.Shared.Next(challengeLength);
        var challenge = new List<int>(challengeLength);
        while (challenge.Count < challengeLength)
        {
            var rand = Random.Shared.Next(Captcha.CaptchaList.Count);
            if (!challenge.Contains(rand))
                challenge.Add(rand);
        }
        var correctAnswer = challenge[correctAnswerIndex];
        var keyboard = challenge
            .Select(x => new InlineKeyboardButton(Captcha.CaptchaList[x].Emoji) { CallbackData = $"cap_{user.Id}_{x}" })
            .ToList();

        ReplyParameters? replyParams = null;
        if (userJoinMessage != null)
            replyParams = userJoinMessage;

        var fullNameForDisplay = FullName(user.FirstName, user.LastName);
        var fullNameLower = fullNameForDisplay.ToLowerInvariant();
        var username = user.Username?.ToLower();
        if (_namesBlacklist.Any(fullNameLower.Contains) || username?.Contains("porn") == true || username?.Contains("p0rn") == true)
            fullNameForDisplay = "новый участник чата";

        var isApproved = IsUserApproved(user.Id, chatId);
        var welcomeMessage = isApproved
            ? $"С возвращением, [{Markdown.Escape(fullNameForDisplay)}](tg://user?id={user.Id})! Для подтверждения личности: на какой кнопке {Captcha.CaptchaList[correctAnswer].Description}?"
            : $"Привет, [{Markdown.Escape(fullNameForDisplay)}](tg://user?id={user.Id})! Антиспам: на какой кнопке {Captcha.CaptchaList[correctAnswer].Description}?";

        // Добавляем рекламу VPN к welcomeMessage (HTML-совместимо, если группа не в исключениях)
        var isNoAdGroup = NoVpnAdGroups.Contains(chatId);
        Console.WriteLine($"[DEBUG] Chat {chatId} - No VPN ad in captcha: {isNoAdGroup}");
        var vpnAdHtml = isNoAdGroup ? "" : "\n\n Твой VPN — @vpn_momai_dev_bot\n<i>2 дня бесплатно</i>";
        var welcomeMessageHtml = (isApproved
            ? $"С возвращением, <a href=\"tg://user?id={user.Id}\">{System.Net.WebUtility.HtmlEncode(fullNameForDisplay)}</a>! Для подтверждения личности: на какой кнопке {Captcha.CaptchaList[correctAnswer].Description}?"
            : $"Привет, <a href=\"tg://user?id={user.Id}\">{System.Net.WebUtility.HtmlEncode(fullNameForDisplay)}</a>! Антиспам: на какой кнопке {Captcha.CaptchaList[correctAnswer].Description}?")
            + vpnAdHtml;

        var del = await _bot.SendMessage(
            chatId,
            welcomeMessageHtml,
            parseMode: ParseMode.Html,
            replyParameters: replyParams,
            replyMarkup: new InlineKeyboardMarkup(keyboard)
        );

        var cts = new CancellationTokenSource();
        DeleteMessageLater(del, TimeSpan.FromMinutes(1.2), cts.Token);
        if (userJoinMessage != null)
        {
            DeleteMessageLater(userJoinMessage, TimeSpan.FromMinutes(1.2), cts.Token);
            _captchaNeededUsers.TryAdd(
                key,
                new CaptchaInfo(chatId, chat.Title, DateTime.UtcNow, user, correctAnswer, cts, userJoinMessage)
            );
        }
        else
        {
            _captchaNeededUsers.TryAdd(key, new CaptchaInfo(chatId, chat.Title, DateTime.UtcNow, user, correctAnswer, cts, null));
        }
        _globalStatsManager.IncCaptcha(chatId, chat.Title ?? "");
    }

    private static string GetChatLink(Chat chat)
    {
        var escapedTitle = Markdown.Escape(chat.Title ?? "Неизвестный чат");
        if (!string.IsNullOrEmpty(chat.Username))
        {
            // Публичная группа или канал
            return $"[{escapedTitle}](https://t.me/{chat.Username})";
        }
        var formattedId = chat.Id.ToString();
        if (formattedId.StartsWith("-100"))
        {
            // Супергруппа без username
            formattedId = formattedId.Substring(4);
            return $"[{escapedTitle}](https://t.me/c/{formattedId})";
        }
        else if (formattedId.StartsWith("-"))
        {
            // Обычная группа без username
            return $"*{escapedTitle}*";
        }
        else
        {
            // Канал без username
            return $"*{escapedTitle}*";
        }
    }

    private static string GetChatLink(long chatId, string? chatTitle)
    {
        // Для обратной совместимости: используем только если нет объекта Chat
        var escapedTitle = Markdown.Escape(chatTitle ?? "Неизвестный чат");
        var formattedId = chatId.ToString();
        if (formattedId.StartsWith("-100"))
        {
            formattedId = formattedId.Substring(4);
            return $"[{escapedTitle}](https://t.me/c/{formattedId})";
        }
        else if (formattedId.StartsWith("-"))
        {
            return $"*{escapedTitle}*";
        }
        else
        {
            if (chatTitle?.StartsWith("@") == true)
            {
                var username = chatTitle.Substring(1);
                return $"[{escapedTitle}](https://t.me/{username})";
            }
            return $"*{escapedTitle}*";
        }
    }

    private async Task ReportStatistics(CancellationToken ct)
    {
        while (await _timer.WaitForNextTickAsync(ct))
        {
            if (DateTimeOffset.UtcNow.Hour != 12)
                continue;

            var report = _stats.ToArray();
            _stats.Clear();
            var sb = new StringBuilder();
            sb.AppendLine("📊 *Статистика за последние 24 часа:*");
            
            foreach (var (chatId, stats) in report.OrderBy(x => x.Value.ChatTitle))
            {
                var sum = stats.KnownBadMessage + stats.BlacklistBanned + stats.StoppedCaptcha + stats.LongNameBanned;
                if (sum == 0) continue;
                
                // Попробуем получить объект чата, если он есть в кэше
                Chat? chat = null;
                try
                {
                    chat = await _bot.GetChat(chatId);
                }
                catch { /* fallback на старый вариант */ }

                sb.AppendLine();
                if (chat != null)
                    sb.AppendLine($"{GetChatLink(chat)} (`{chat.Id}`) [{ChatSettingsManager.GetChatType(chat.Id)}]:");
                else
                    sb.AppendLine($"{GetChatLink(chatId, stats.ChatTitle)} (`{chatId}`) [{ChatSettingsManager.GetChatType(chatId)}]:");
                sb.AppendLine($"▫️ Всего блокировок: *{sum}*");
                if (stats.BlacklistBanned > 0)
                    sb.AppendLine($"▫️ По блеклистам: *{stats.BlacklistBanned}*");
                if (stats.StoppedCaptcha > 0)
                    sb.AppendLine($"▫️ Не прошли капчу: *{stats.StoppedCaptcha}*");
                if (stats.KnownBadMessage > 0)
                    sb.AppendLine($"▫️ Известные спам-сообщения: *{stats.KnownBadMessage}*");
                if (stats.LongNameBanned > 0)
                    sb.AppendLine($"▫️ За длинные имена: *{stats.LongNameBanned}*");
            }

            if (sb.Length <= 35) // Если нет данных, кроме заголовка
            {
                sb.AppendLine("\nНичего интересного не произошло 🎉");
            }

            try
            {
                await _bot.SendMessage(Config.AdminChatId, sb.ToString(), parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to sent report to admin chat");
            }
        }
    }

    private async Task<bool> BanIfBlacklisted(User user, Chat chat, Message? userJoinMessage = null)
    {
        if (!await _userManager.InBanlist(user.Id))
            return false;

        try
        {
            var stats = _stats.GetOrAdd(chat.Id, new Stats(chat.Title));
            Interlocked.Increment(ref stats.BlacklistBanned);
            
            // Баним пользователя на 4 часа с параметром revokeMessages: true чтобы удалить все сообщения
            var banUntil = DateTime.UtcNow + TimeSpan.FromMinutes(240);
            await _bot.BanChatMember(chat.Id, user.Id, banUntil, revokeMessages: true);
            
            // Явно удаляем сообщение о входе в чат, если оно есть
            if (userJoinMessage != null)
            {
                try
                {
                    await _bot.DeleteMessage(chat.Id, userJoinMessage.MessageId);
                    _logger.LogDebug("Удалено сообщение о входе пользователя из блэклиста");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось удалить сообщение о входе пользователя из блэклиста");
                }
            }
            
            // Удаляем из списка одобренных
            if (_userManager.RemoveApproval(user.Id, chat.Id, removeAll: true))
            {
                await _bot.SendMessage(
                    Config.AdminChatId,
                    $"⚠️ Пользователь [{Markdown.Escape(FullName(user.FirstName, user.LastName))}](tg://user?id={user.Id}) удален из списка одобренных после бана по блеклисту",
                    parseMode: ParseMode.Markdown
                );
            }
            
            // Отключено
            // await _bot.SendMessage(
            //     Config.AdminChatId,
            //     $"🚫 *Автобан на 4 часа* в чате *{chat.Title}*\nПользователь [{Markdown.Escape(FullName(user.FirstName, user.LastName))}](tg://user?id={user.Id}) находится в блэклисте",
            //     parseMode: ParseMode.Markdown
            // );
            _logger.LogInformation("Пользователь {User} (id={UserId}) из блэклиста забанен на 4 часа в чате {ChatTitle} (id={ChatId})", FullName(user.FirstName, user.LastName), user.Id, chat.Title, chat.Id);
            _globalStatsManager.IncBan(chat.Id, chat.Title ?? "");
            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to ban");
            await _bot.SendMessage(
                Config.AdminChatId,
                $"⚠️ Не могу забанить юзера из блеклиста в чате *{chat.Title}*. Не хватает могущества? Сходите забаньте руками.",
                parseMode: ParseMode.Markdown
            );
        }

        return false;
    }

    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";

    private async Task HandleAdminCallback(string cbData, CallbackQuery cb)
    {
        var split = cbData.Split('_').ToList();
        if (split.Count > 1 && split[0] == "approve" && long.TryParse(split[1], out var approveUserId))
        {
            // Админ одобряет пользователя - всегда глобально
            await _userManager.Approve(approveUserId);
            await _bot.SendMessage(
                new ChatId(Config.AdminChatId),
                $"✅ [{Markdown.Escape(FullName(cb.From.FirstName, cb.From.LastName))}](tg://user?id={cb.From.Id}) добавил пользователя в список доверенных",
                parseMode: ParseMode.Markdown,
                replyParameters: cb.Message?.MessageId
            );
        }
        else if (split.Count > 2 && split[0] == "ban" && long.TryParse(split[1], out var chatId) && long.TryParse(split[2], out var userId))
        {
            var userMessage = MemoryCache.Default.Remove(cbData) as Message;
            var text = userMessage?.Caption ?? userMessage?.Text;
            if (!string.IsNullOrWhiteSpace(text))
                await _badMessageManager.MarkAsBad(text);
            try
            {
                await _bot.BanChatMember(new ChatId(chatId), userId);
                if (_userManager.RemoveApproval(userId, chatId, removeAll: true))
                {
                    await _bot.SendMessage(
                        Config.AdminChatId,
                        $"⚠️ Пользователь удален из списка одобренных после ручного бана администратором [{Markdown.Escape(FullName(cb.From.FirstName, cb.From.LastName))}](tg://user?id={cb.From.Id})",
                        parseMode: ParseMode.Markdown,
                        replyParameters: cb.Message?.MessageId
                    );
                }
                await _bot.SendMessage(
                    new ChatId(Config.AdminChatId),
                    $"🚫 {AdminDisplayName(cb.From)} забанил, сообщение добавлено в список авто-бана",
                    parseMode: ParseMode.Markdown,
                    replyParameters: cb.Message?.MessageId
                );
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to ban");
                await _bot.SendMessage(
                    new ChatId(Config.AdminChatId),
                    $"⚠️ Не могу забанить. Не хватает могущества? Сходите забаньте руками",
                    replyParameters: cb.Message?.MessageId
                );
            }
            try
            {
                if (userMessage != null)
                    await _bot.DeleteMessage(userMessage.Chat, userMessage.MessageId);
            }
            catch { }
        }
        else if (split.Count > 1 && split[0] == "aiOk" && long.TryParse(split[1], out var aiOkUserId))
        {
            // Админ отметил профиль как безопасный - добавляем в кэш как проверенный
            _aiChecks.MarkUserOkay(aiOkUserId);
            await _bot.SendMessage(
                new ChatId(Config.AdminChatId),
                $"✅ {AdminDisplayName(cb.From)} отметил профиль как безопасный - AI проверки отключены для этого пользователя",
                replyParameters: cb.Message?.MessageId
            );
        }
        var msg = cb.Message;
        if (msg != null)
            await _bot.EditMessageReplyMarkup(msg.Chat.Id, msg.MessageId);
    }

    private async Task HandleChatMemberUpdated(Update update)
    {
        var chatMember = update.ChatMember;
        Debug.Assert(chatMember != null);
        var newChatMember = chatMember.NewChatMember;
        ChatSettingsManager.EnsureChatInConfig(chatMember.Chat.Id, chatMember.Chat.Title);
        switch (newChatMember.Status)
        {
            case ChatMemberStatus.Member:
            {
                _logger.LogDebug("New chat member new {@New} old {@Old}", newChatMember, chatMember.OldChatMember);
                if (chatMember.OldChatMember.Status == ChatMemberStatus.Left)
                {
                    var u = newChatMember.User;
                    var joinKey = $"joined_{chatMember.Chat.Id}_{u.Id}";
                    await Task.Delay(200); // Дать шанс NewChatMembers выставить флаг
                    if (!_joinedUserFlags.ContainsKey(joinKey))
                    {
                        _logger.LogInformation("==================== НОВЫЙ УЧАСТНИК ====================\nПользователь {User} (id={UserId}, username={Username}) зашел в группу '{ChatTitle}' (id={ChatId})\n========================================================", 
                            (u.FirstName + (string.IsNullOrEmpty(u.LastName) ? "" : " " + u.LastName)), u.Id, u.Username ?? "-", chatMember.Chat.Title ?? "-", chatMember.Chat.Id);
                        _joinedUserFlags.TryAdd(joinKey, 1);
                        _ = Task.Run(async () => { await Task.Delay(15000); _joinedUserFlags.TryRemove(joinKey, out _); });
                    }
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        await IntroFlow(null, newChatMember.User, chatMember.Chat);
                    });
                }

                break;
            }
            case ChatMemberStatus.Kicked
            or ChatMemberStatus.Restricted:
                var user = newChatMember.User;
                var key = $"{chatMember.Chat.Id}_{user.Id}";
                var lastMessage = MemoryCache.Default.Get(key) as string;
                var tailMessage = string.IsNullOrWhiteSpace(lastMessage)
                    ? ""
                    : $" Его/её последним сообщением было:\n```\n{lastMessage}\n```";
                
                // Удаляем из списка доверенных
                if (_userManager.RemoveApproval(user.Id, chatMember.Chat.Id, removeAll: true))
                {
                    await _bot.SendMessage(
                        Config.AdminChatId,
                        $"⚠️ Пользователь [{Markdown.Escape(FullName(user.FirstName, user.LastName))}](tg://user?id={user.Id}) удален из списка одобренных после получения ограничений в чате *{chatMember.Chat.Title}*",
                        parseMode: ParseMode.Markdown
                    );
                }
                
                await _bot.SendMessage(
                    new ChatId(Config.AdminChatId),
                    $"🔔 В чате *{chatMember.Chat.Title}* пользователю [{Markdown.Escape(FullName(user.FirstName, user.LastName))}](tg://user?id={user.Id}) дали ридонли или забанили, посмотрите в Recent actions, возможно ML пропустил спам. Если это так - кидайте его сюда.{tailMessage}",
                    parseMode: ParseMode.Markdown
                );
                break;
        }
    }

    private async Task DontDeleteButReportMessage(Message message, User user, CancellationToken stoppingToken)
    {
        // Проверяем, является ли сообщение сообщением о выходе пользователя
        if (message.LeftChatMember != null)
        {
            _logger.LogDebug("Пропускаем форвард сообщения о выходе пользователя");
            return;
        }
        
        try
        {
            var forward = await _bot.ForwardMessage(
                new ChatId(Config.AdminChatId),
                message.Chat.Id,
                message.MessageId,
                cancellationToken: stoppingToken
            );
            var callbackData = $"ban_{message.Chat.Id}_{user.Id}";
            MemoryCache.Default.Add(callbackData, message, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });
            await _bot.SendMessage(
                new ChatId(Config.AdminChatId),
                $"⚠️ *Подозрительное сообщение* - например, медиа без подписи от 'нового' пользователя или сообщение от канала. Сообщение *НЕ удалено*.\nПользователь [{Markdown.Escape(FullName(user.FirstName, user.LastName))}](tg://user?id={user.Id}) в чате *{message.Chat.Title}*",
                parseMode: ParseMode.Markdown,
                replyParameters: forward.MessageId,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new InlineKeyboardButton("🤖 бан") { CallbackData = callbackData },
                    new InlineKeyboardButton("👍 ok") { CallbackData = "noop" },
                    new InlineKeyboardButton("🥰 свой") { CallbackData = $"approve_{user.Id}" }
                }),
                cancellationToken: stoppingToken
            );
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Ошибка при пересылке сообщения");
            // Если не удалось переслать, просто отправляем уведомление
            await _bot.SendMessage(
                new ChatId(Config.AdminChatId),
                $"⚠️ Не удалось переслать подозрительное сообщение из чата *{message.Chat.Title}* от пользователя [{Markdown.Escape(FullName(user.FirstName, user.LastName))}](tg://user?id={user.Id})",
                parseMode: ParseMode.Markdown
            );
        }
    }

    private async Task DeleteAndReportMessage(Message message, string reason, CancellationToken stoppingToken)
    {
        var user = message.From;
        
        // Проверяем, является ли сообщение сообщением о выходе пользователя
        if (message.LeftChatMember != null)
        {
            _logger.LogDebug("Пропускаем форвард сообщения о выходе пользователя");
            
            try
            {
                await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
                _logger.LogDebug("Сообщение о выходе пользователя удалено");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Не удалось удалить сообщение о выходе пользователя");
            }
            return;
        }
        
        Message? forward = null;
        var deletionMessagePart = $"{reason}";
        
        try 
        {
            // Пытаемся переслать сообщение
            forward = await _bot.ForwardMessage(
                new ChatId(Config.AdminChatId),
                message.Chat.Id,
                message.MessageId,
                cancellationToken: stoppingToken
            );
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось переслать сообщение");
        }
        
        try
        {
            await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
            deletionMessagePart += ", сообщение удалено.";
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to delete");
            deletionMessagePart += ", сообщение НЕ удалено (не хватило могущества?).";
        }
        // --- Новая логика: объясняющее сообщение для новичка ---
        bool isBlacklisted = false;
        if (user != null && !IsUserApproved(user.Id, message.Chat.Id))
        {
            try {
                isBlacklisted = await _userManager.InBanlist(user.Id);
            } catch {}
        }
        if (user != null && !IsUserApproved(user.Id, message.Chat.Id) && !isBlacklisted && !_warnedUsers.ContainsKey(user.Id))
        {
            var displayName = !string.IsNullOrEmpty(user.FirstName)
                ? System.Net.WebUtility.HtmlEncode(FullName(user.FirstName, user.LastName))
                : (!string.IsNullOrEmpty(user.Username) ? "@" + user.Username : "гость");
            var mention = $"<a href=\"tg://user?id={user.Id}\">{displayName}</a>";
            var warnMsg = $"👋 {mention}, вы пока <b>новичок</b> в этом чате.\n\n<b>Первые 3 сообщения</b> проходят антиспам-проверку:\n• нельзя эмодзи, картинки, рекламу  \n• работает ML-анализ\n\nПосле 3 обычных сообщений фильтры <b>отключатся</b>, и вы сможете писать свободно!";
            var sentWarn = await _bot.SendMessage(message.Chat.Id, warnMsg, parseMode: ParseMode.Html);
            _warnedUsers.TryAdd(user.Id, DateTime.UtcNow);
            DeleteMessageLater(sentWarn, TimeSpan.FromSeconds(40));
            _logger.LogInformation("Показано объясняющее сообщение новичку: {User} (id={UserId}) в чате {ChatTitle} (id={ChatId})", displayName, user.Id, message.Chat.Title, message.Chat.Id);
        }
        else if (user != null && !IsUserApproved(user.Id, message.Chat.Id) && isBlacklisted)
        {
            var displayName = !string.IsNullOrEmpty(user.FirstName)
                ? System.Net.WebUtility.HtmlEncode(FullName(user.FirstName, user.LastName))
                : (!string.IsNullOrEmpty(user.Username) ? "@" + user.Username : "гость");
            _logger.LogInformation("Объясняющее сообщение НЕ показано (пользователь в блэклисте): {User} (id={UserId}) в чате {ChatTitle} (id={ChatId})", displayName, user.Id, message.Chat.Title, message.Chat.Id);
        }
        else if (user != null && !IsUserApproved(user.Id, message.Chat.Id))
        {
            var displayName = !string.IsNullOrEmpty(user.FirstName)
                ? System.Net.WebUtility.HtmlEncode(FullName(user.FirstName, user.LastName))
                : (!string.IsNullOrEmpty(user.Username) ? "@" + user.Username : "гость");
            _logger.LogInformation("Объясняющее сообщение НЕ показано (уже было) новичку: {User} (id={UserId}) в чате {ChatTitle} (id={ChatId})", displayName, user.Id, message.Chat.Title, message.Chat.Id);
        }
        // --- Конец новой логики ---
        var callbackDataBan = $"ban_{message.Chat.Id}_{user.Id}";
        MemoryCache.Default.Add(callbackDataBan, message, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });
        var postLink = LinkToMessage(message.Chat, message.MessageId);
        var row = new List<InlineKeyboardButton>
        {
                new InlineKeyboardButton("🤖 бан") { CallbackData = callbackDataBan },
                new InlineKeyboardButton("😶 пропуск") { CallbackData = "noop" },
                new InlineKeyboardButton("🥰 свой") { CallbackData = $"approve_{user.Id}" }
        };

        await _bot.SendMessage(
            new ChatId(Config.AdminChatId),
            $"⚠️ *{deletionMessagePart}*\nПользователь [{Markdown.Escape(FullName(user.FirstName, user.LastName))}](tg://user?id={user.Id}) в чате *{message.Chat.Title}*\n{postLink}",
            parseMode: ParseMode.Markdown,
            replyParameters: forward,
            replyMarkup: new InlineKeyboardMarkup(row),
            cancellationToken: stoppingToken
        );
    }

    private static string LinkToMessage(Chat chat, long messageId) =>
        chat.Type == ChatType.Supergroup ? LinkToSuperGroupMessage(chat, messageId)
        : chat.Username == null ? ""
        : LinkToGroupWithNameMessage(chat, messageId);

    private static string LinkToSuperGroupMessage(Chat chat, long messageId) => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}";

    private static string LinkToGroupWithNameMessage(Chat chat, long messageId) => $"https://t.me/{chat.Username}/{messageId}";

    private async Task AdminChatMessage(Message message)
    {
        if (message is { ReplyToMessage: { } replyToMessage, Text: "/spam" or "/ham" or "/check" })
        {
            if (replyToMessage.From?.Id == _me.Id && replyToMessage.ForwardDate == null)
            {
                await _bot.SendMessage(
                    message.Chat.Id,
                    "⚠️ Похоже что вы промахнулись и реплайнули на сообщение бота, а не форвард",
                    replyParameters: replyToMessage
                );
                return;
            }
            var text = replyToMessage.Text ?? replyToMessage.Caption;
            if (!string.IsNullOrWhiteSpace(text))
            {
                switch (message.Text)
                {
                    case "/check":
                    {
                        var emojis = SimpleFilters.TooManyEmojis(text);
                        var normalized = TextProcessor.NormalizeText(text);
                        var lookalike = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(normalized);
                        var hasStopWords = SimpleFilters.HasStopWords(normalized);
                        var (spam, score) = await _classifier.IsSpam(normalized);
                        var lookAlikeMsg = lookalike.Count == 0 ? "отсутствуют" : string.Join(", ", lookalike);
                        var msg =
                            $"*Результат проверки:*\n"
                            + $"• Много эмодзи: *{emojis}*\n"
                            + $"• Найдены стоп-слова: *{hasStopWords}*\n"
                            + $"• Маскирующиеся слова: *{lookAlikeMsg}*\n"
                            + $"• ML классификатор: спам *{spam}*, скор *{score}*\n\n"
                            + $"_Если простые фильтры отработали, то в датасет добавлять не нужно_";
                        await _bot.SendMessage(message.Chat.Id, msg, parseMode: ParseMode.Markdown);
                        break;
                    }
                    case "/spam":
                        await _classifier.AddSpam(text);
                        await _badMessageManager.MarkAsBad(text);
                        await _bot.SendMessage(
                            message.Chat.Id,
                            "✅ Сообщение добавлено как пример спама в датасет, а также в список авто-бана",
                            replyParameters: replyToMessage
                        );
                        break;
                    case "/ham":
                        await _classifier.AddHam(text);
                        await _bot.SendMessage(
                            message.Chat.Id,
                            "✅ Сообщение добавлено как пример НЕ-спама в датасет",
                            replyParameters: replyToMessage
                        );
                        break;
                }
            }
        }
        // Команда статистики по группам
        else if (message.Text?.Trim().ToLower() == "/stat" || message.Text?.Trim().ToLower() == "/stats")
        {
            var report = _stats.ToArray();
            var sb = new StringBuilder();
            sb.AppendLine("📊 *Статистика по группам:*\n");
            foreach (var (chatId, stats) in report.OrderBy(x => x.Value.ChatTitle))
            {
                var sum = stats.KnownBadMessage + stats.BlacklistBanned + stats.StoppedCaptcha + stats.LongNameBanned;
                if (sum == 0) continue;
                Chat? chat = null;
                try { chat = await _bot.GetChat(chatId); } catch { }
                sb.AppendLine();
                if (chat != null)
                    sb.AppendLine($"{GetChatLink(chat)} (`{chat.Id}`) [{ChatSettingsManager.GetChatType(chat.Id)}]:");
                else
                    sb.AppendLine($"{GetChatLink(chatId, stats.ChatTitle)} (`{chatId}`) [{ChatSettingsManager.GetChatType(chatId)}]:");
                sb.AppendLine($"▫️ Всего блокировок: *{sum}*");
                if (stats.BlacklistBanned > 0)
                    sb.AppendLine($"▫️ По блеклистам: *{stats.BlacklistBanned}*");
                if (stats.StoppedCaptcha > 0)
                    sb.AppendLine($"▫️ Не прошли капчу: *{stats.StoppedCaptcha}*");
                if (stats.KnownBadMessage > 0)
                    sb.AppendLine($"▫️ Известные спам-сообщения: *{stats.KnownBadMessage}*");
                if (stats.LongNameBanned > 0)
                    sb.AppendLine($"▫️ За длинные имена: *{stats.LongNameBanned}*");
            }
            if (sb.Length <= 35)
                sb.AppendLine("\nНичего интересного не произошло 🎉");
            await _bot.SendMessage(message.Chat.Id, sb.ToString(), parseMode: ParseMode.Markdown);
            return;
        }
        // Добавляю обработку команды /say
        if (message.Text != null && message.Text.StartsWith("/say "))
        {
            var parts = message.Text.Split(' ', 3);
            if (parts.Length < 3)
            {
                await _bot.SendMessage(message.Chat.Id, "Формат: /say @username сообщение или /say user_id сообщение");
                return;
            }
            var target = parts[1];
            var textToSend = parts[2];
            long? userId = null;
            if (target.StartsWith("@"))
            {
                // Пробуем найти userId по username среди недавних пользователей (по кэшу)
                // Если нет — не отправляем
                userId = TryFindUserIdByUsername(target.Substring(1));
            }
            else if (long.TryParse(target, out var id))
            {
                userId = id;
            }
            if (userId == null)
            {
                await _bot.SendMessage(message.Chat.Id, $"Не удалось найти пользователя {target}. Сообщение не отправлено.");
                return;
            }
            try
            {
                await _bot.SendMessage(userId.Value, textToSend, parseMode: ParseMode.Markdown, disableNotification: true);
                await _bot.SendMessage(message.Chat.Id, $"Сообщение отправлено пользователю {target}");
            }
            catch (Exception ex)
            {
                await _bot.SendMessage(message.Chat.Id, $"Не удалось доставить сообщение пользователю {target}: {ex.Message}");
            }
            return;
        }
    }

    // Вспомогательная функция для поиска userId по username среди недавних пользователей (по кэшу)
    private long? TryFindUserIdByUsername(string username)
    {
        // Можно использовать MemoryCache или другой кэш, если он есть
        // Здесь пример с MemoryCache: ищем по ключам, где username встречался
        foreach (var item in MemoryCache.Default)
        {
            if (item.Value is string text && text.Contains(username, StringComparison.OrdinalIgnoreCase))
            {
                // Ключи вида chatId_userId
                var parts = item.Key.ToString().Split('_');
                if (parts.Length == 2 && long.TryParse(parts[1], out var uid))
                    return uid;
            }
        }
        return null;
    }

    private static string UserToKey(long chatId, User user) => $"{chatId}_{user.Id}";
    
    /// <summary>
    /// Проверяет, одобрен ли пользователь с учетом текущей системы одобрения
    /// </summary>
    private bool IsUserApproved(long userId, long? chatId = null)
    {
        if (Config.UseNewApprovalSystem)
        {
            // Новая система: проверяем с учетом режима (глобального или группового)
            return _userManager.Approved(userId, chatId);
        }
        else
        {
            // Старая система: проверяем только глобально
            return _userManager.Approved(userId);
        }
    }

    /// <summary>
    /// Определяет, является ли сообщение из обсуждения канала
    /// </summary>
    private async Task<bool> IsChannelDiscussion(Chat chat, Message message)
    {
        try
        {
            // Если это не супергруппа, то это точно не обсуждение
            if (chat.Type != ChatType.Supergroup)
                return false;

            // Проверяем, есть ли у группы связанный канал
            var chatFull = await _bot.GetChat(chat.Id);
            var hasLinkedChannel = chatFull.LinkedChatId != null;
            
            // Обсуждение канала если:
            // 1. Есть связанный канал И сообщение автоматически переслано
            // 2. ИЛИ просто есть связанный канал (пользователи пишут в обсуждении)
            var isDiscussion = hasLinkedChannel && (message.IsAutomaticForward || true);
            
            if (isDiscussion)
            {
                _logger.LogDebug("Обнаружено обсуждение канала: chat={ChatId}, linkedChannel={LinkedId}, autoForward={AutoForward}", 
                    chat.Id, chatFull.LinkedChatId, message.IsAutomaticForward);
            }
            
            return isDiscussion;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось определить тип чата {ChatId}", chat.Id);
            return false;
        }
    }

    private async Task BanNoCaptchaUsers()
    {
        if (_captchaNeededUsers.IsEmpty)
            return;
        var now = DateTime.UtcNow;
        var users = _captchaNeededUsers.ToArray();
        foreach (var (key, (chatId, title, timestamp, user, _, _, _)) in users)
        {
            var minutes = (now - timestamp).TotalMinutes;
            if (minutes > 1)
            {
                // Логируем неуспешное прохождение капчи по таймауту
                _logger.LogInformation("==================== КАПЧА НЕ ПРОЙДЕНА (таймаут) ====================\nПользователь {User} (id={UserId}) не прошёл капчу (таймаут) в группе '{ChatTitle}' (id={ChatId})\n====================================================================", user.FirstName + (string.IsNullOrEmpty(user.LastName) ? "" : " " + user.LastName), user.Id, title ?? "-", chatId);
                var stats = _stats.GetOrAdd(chatId, new Stats(title));
                Interlocked.Increment(ref stats.StoppedCaptcha);
                _captchaNeededUsers.TryRemove(key, out _);
                await _bot.BanChatMember(chatId, user.Id, now + TimeSpan.FromMinutes(20), revokeMessages: false);
                UnbanUserLater(chatId, user.Id);
            }
        }
    }

    private class CaptchaAttempts
    {
        public int Attempts { get; set; }
    }

    private void UnbanUserLater(ChatId chatId, long userId)
    {
        var key = $"captcha_{userId}";
        var cache = MemoryCache.Default.AddOrGetExisting(
            new CacheItem(key, new CaptchaAttempts()),
            new CacheItemPolicy { SlidingExpiration = TimeSpan.FromHours(4) }
        );
        var attempts = (CaptchaAttempts)cache.Value;
        attempts.Attempts++;
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Exp(attempts.Attempts)));
                await _bot.UnbanChatMember(chatId, userId);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, nameof(UnbanUserLater));
            }
        });
    }

    private void DeleteMessageLater(Message message, TimeSpan after = default, CancellationToken cancellationToken = default)
    {
        if (after == default)
            after = TimeSpan.FromMinutes(5);
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await Task.Delay(after, cancellationToken);
                    await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "DeleteMessage");
                }
            },
            cancellationToken
        );
    }

    private static string AdminDisplayName(User user)
    {
        return !string.IsNullOrEmpty(user.Username)
            ? user.Username
            : FullName(user.FirstName, user.LastName);
    }
}

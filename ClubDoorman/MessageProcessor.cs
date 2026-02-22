using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman;

internal enum CheckResult
{
    Pass,
    Suspicious,
    NoMoreAction,
}

internal sealed class ChatIgnoreState
{
    public int FailureCount { get; set; }
    public DateTime IgnoreUntil { get; set; }
}

internal class MessageProcessor
{
    private static readonly TimeSpan[] NewcomerBanlistCheckAfterJoin =
    [
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(45),
        TimeSpan.FromHours(2),
        TimeSpan.FromHours(6),
        TimeSpan.FromHours(12),
        TimeSpan.FromHours(24),
    ];

    private readonly ITelegramBotClient _bot;
    private readonly ILogger<MessageProcessor> _logger;
    private readonly SpamHamClassifier _classifier;
    private readonly UserManager _userManager;
    private readonly BadMessageManager _badMessageManager;
    private readonly AiChecks _aiChecks;
    private readonly CaptchaManager _captchaManager;
    private readonly ConcurrentDictionary<long, int> _goodUserMessages = new();
    private readonly ConcurrentDictionary<long, ChatIgnoreState> _ignoredChats = new();
    private readonly StatisticsReporter _statistics;
    private readonly Config _config;
    private readonly ReactionHandler _reactionHandler;
    private readonly AdminCommandHandler _adminCommandHandler;
    private readonly RecentMessagesStorage _recentMessagesStorage;
    private readonly ConcurrentDictionary<(long ChatId, long UserId), CancellationTokenSource> _newcomersOnWatch = new();
    private readonly HybridCache _hybridCache;
    private readonly SpamDeduplicationCache _spamDeduplicationCache;
    private User? _me;

    public MessageProcessor(
        ITelegramBotClient bot,
        ILogger<MessageProcessor> logger,
        SpamHamClassifier classifier,
        UserManager userManager,
        BadMessageManager badMessageManager,
        AiChecks aiChecks,
        CaptchaManager captchaManager,
        StatisticsReporter statistics,
        Config config,
        ReactionHandler reactionHandler,
        AdminCommandHandler adminCommandHandler,
        RecentMessagesStorage recentMessagesStorage,
        HybridCache hybridCache,
        SpamDeduplicationCache spamDeduplicationCache
    )
    {
        _bot = bot;
        _logger = logger;
        _classifier = classifier;
        _userManager = userManager;
        _badMessageManager = badMessageManager;
        _aiChecks = aiChecks;
        _captchaManager = captchaManager;
        _statistics = statistics;
        _config = config;
        _reactionHandler = reactionHandler;
        _adminCommandHandler = adminCommandHandler;
        _recentMessagesStorage = recentMessagesStorage;
        _hybridCache = hybridCache;
        _spamDeduplicationCache = spamDeduplicationCache;
    }

    public async Task HandleUpdate(Update update, CancellationToken stoppingToken)
    {
        using var logScope = _logger.BeginScope("Update Id = {Id}", update.Id);
        _me ??= await _bot.GetMe(cancellationToken: stoppingToken);
        if (update.MessageReaction != null)
        {
            await _reactionHandler.HandleReaction(update.MessageReaction);
            return;
        }
        if (update.CallbackQuery != null)
        {
            var cb = update.CallbackQuery;
            if (cb.Data == null)
                return;
            var msg = cb.Message;

            if (msg == null || msg.Chat.Id == _config.AdminChatId || _config.MultiAdminChatMap.Values.Contains(msg.Chat.Id))
                await _adminCommandHandler.HandleAdminCallback(cb.Data, cb);
            else
                await _captchaManager.HandleCaptchaCallback(update);
            return;
        }
        if (update.ChatMember != null)
        {
            if (update.ChatMember.From.Id == _me.Id)
                return;
            _logger.LogDebug("Chat member updated {@ChatMember}", update.ChatMember);
            await HandleChatMemberUpdated(update, stoppingToken);
            return;
        }

        var message = update.EditedMessage ?? update.Message;
        if (message == null)
            return;

        var chat = message.Chat;
        using var _chatScope = _logger.BeginScope("Chat {ChatName}", chat.Title);

        if (chat.Id == _config.AdminChatId || _config.MultiAdminChatMap.Values.Contains(chat.Id))
        {
            await _adminCommandHandler.AdminChatMessage(message);
            return;
        }

        var admChat = _config.GetAdminChat(chat.Id);
        if (message.SenderChat != null)
        {
            if (message.IsAutomaticForward)
                return;
            if (message.SenderChat.Id == chat.Id)
                return;
            var chatFull = await _bot.GetChat(chat, stoppingToken);
            var linked = chatFull.LinkedChatId;
            if (linked != null && linked == message.SenderChat.Id)
                return;

            _recentMessagesStorage.Add(message.SenderChat.Id, chat.Id, message);
            if (!_config.ChannelsCheckExclusionChats.Contains(chat.Id))
            {
                if (_config.ChannelAutoBan)
                {
                    try
                    {
                        var subs = await _bot.GetChatMemberCount(message.SenderChat.Id, cancellationToken: stoppingToken);
                        if (subs > Consts.BigChannelSubsCount)
                        {
                            _logger.LogDebug("Popular channel {Ch}, not banning", message.SenderChat.Title);
                            return;
                        }
                        await _bot.DeleteMessage(chat, message.MessageId, stoppingToken);
                        await _bot.BanChatSenderChat(chat, message.SenderChat.Id, stoppingToken);
                        var stats = _statistics.Stats.GetOrAdd(chat.Id, new Stats(chat.Title) { Id = chat.Id });
                        stats.Channels++;
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Unable to ban");
                        if (_config.NonFreeChat(chat.Id))
                            await _bot.SendMessage(
                                admChat,
                                $"Не могу удалить или забанить в чате {chat.Title} сообщение от имени канала {message.SenderChat.Title}. Не хватает могущества?",
                                cancellationToken: stoppingToken
                            );
                    }
                    return;
                }

                await DontDeleteButReportMessage(message, "сообщение от канала", stoppingToken);
            }
        }

        var user = message.From!;
        if (_userManager.Approved(user.Id))
        {
            var approvedText = message.Text ?? message.Caption;
            if (
                _config.ApprovedUsersMlSpamCheck
                && !string.IsNullOrWhiteSpace(approvedText)
                && _config.NonFreeChat(message.Chat.Id)
                && !approvedText.Contains("http")
            )
            {
                var normalized = TextProcessor.NormalizeText(approvedText);
                if (normalized.Length >= 10)
                {
                    var (spam, score) = await _classifier.IsSpam(normalized);
                    if (spam)
                    {
                        var fwd = await _bot.ForwardMessage(
                            _config.AdminChatId,
                            message.Chat,
                            message.MessageId,
                            cancellationToken: stoppingToken
                        );
                        await _bot.SendMessage(
                            _config.AdminChatId,
                            $"ML решил что это спам, скор {score}, но пользователь в доверенных. Возможно стоит добавить в ham, чат {chat.Title} {Utils.LinkToMessage(chat, message.MessageId)}",
                            replyParameters: fwd,
                            cancellationToken: stoppingToken
                        );
                    }
                }
            }
            return;
        }
        var clubUser = await _userManager.GetClubUsername(user.Id);
        if (!string.IsNullOrEmpty(clubUser))
        {
            _logger.LogDebug("User is {Name} from club", clubUser);
            return;
        }

        if (_ignoredChats.TryGetValue(chat.Id, out var ignoreState) && DateTime.UtcNow < ignoreState.IgnoreUntil)
        {
            _logger.LogDebug("Ignoring chat {ChatId} until {IgnoreUntil} due to repeated failures", chat.Id, ignoreState.IgnoreUntil);
            return;
        }
        if (chat.Type == ChatType.Private)
        {
            await _bot.SendMessage(
                chat,
                "Привет, я бот-антиспам. Для базовой защиты - просто добавь меня в группу и сделай админом. Для продвинутой - пиши @TiraelSedai.\nhttps://antispam.burm.dev",
                replyParameters: message,
                cancellationToken: stoppingToken
            );
            return;
        }
        if (message.LeftChatMember != null)
        {
            await _bot.DeleteMessage(message.Chat, message.Id, cancellationToken: stoppingToken);
            return;
        }
        if (message.NewChatMembers != null)
        {
            try
            {
                await _bot.DeleteMessage(message.Chat, message.Id, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Cannot delete join message");
            }
            return;
        }

        if (_me != null && user.Id == _me.Id)
            return;

        if (_captchaManager.IsCaptchaNeeded(chat.Id, user))
        {
            await _bot.DeleteMessage(chat.Id, message.MessageId, stoppingToken);
            return;
        }

        if (await _userManager.InBanlist(user.Id))
        {
            _logger.LogDebug("InBanlist");
            if (_config.BlacklistAutoBan)
            {
                var stats = _statistics.Stats.GetOrAdd(chat.Id, new Stats(chat.Title) { Id = chat.Id });
                stats.BlacklistBanned++;
                var deleteFailed = false;
                var banFailed = false;
                try
                {
                    await _bot.DeleteMessage(chat.Id, message.MessageId, stoppingToken);
                }
                catch (ApiRequestException e)
                {
                    _logger.LogInformation(e, "Cannot delete message");
                    deleteFailed = true;
                }
                try
                {
                    await _bot.BanChatMember(chat.Id, user.Id, revokeMessages: false, cancellationToken: stoppingToken);
                }
                catch (ApiRequestException e)
                {
                    _logger.LogInformation(e, "Cannot ban user");
                    banFailed = true;
                }

                if (deleteFailed && banFailed && !_config.NonFreeChat(chat.Id))
                {
                    var state = _ignoredChats.AddOrUpdate(
                        chat.Id,
                        _ => new ChatIgnoreState { FailureCount = 1, IgnoreUntil = DateTime.UtcNow.AddHours(6) },
                        (_, existing) =>
                        {
                            existing.FailureCount++;
                            existing.IgnoreUntil = DateTime.UtcNow.AddHours(existing.FailureCount);
                            return existing;
                        }
                    );
                    _logger.LogWarning(
                        "Both delete and ban failed for free chat {ChatId} {ChatTitle}. Ignoring for {Hours} hour(s) until {IgnoreUntil}",
                        chat.Id,
                        chat.Title,
                        state.FailureCount,
                        state.IgnoreUntil
                    );
                }
            }
            else
            {
                await DeleteAndReportMessage(message, "Пользователь в блеклисте спамеров", stoppingToken);
            }
            return;
        }

        if (message.ReplyMarkup != null)
        {
            _logger.LogDebug("Buttons");
            await (
                _config.ButtonAutoBan
                    ? AutoBan(message, "Сообщение с кнопками", stoppingToken)
                    : DeleteAndReportMessage(message, "Сообщение с кнопками", stoppingToken)
            );
            return;
        }
        if (message.Story != null)
        {
            _logger.LogDebug("Stories");
            await DeleteAndReportMessage(message, "Сторис", stoppingToken);
            return;
        }

        var text = message.Text ?? message.Caption;
        if (message.Quote?.Text != null)
            text = $"> {message.Quote.Text}{Environment.NewLine}{text}";

        _logger.LogDebug("First-time message, chat {Chat} user {User}, message {Message}", chat.Title, Utils.FullName(user), text);
        using var logScopeName = _logger.BeginScope("User {Usr}", Utils.FullName(user));
        _recentMessagesStorage.Add(user.Id, chat.Id, message);

        var contentResult = await CheckMessageContent(message, user, text ?? "", chat, admChat, stoppingToken);
        if (contentResult == CheckResult.NoMoreAction)
            return;

        var profileResult = await CheckUserProfile(message, user, text ?? "", chat, admChat, stoppingToken);
        if (profileResult == CheckResult.NoMoreAction)
            return;

        if (
            update.Message != null
            && update.EditedMessage == null
            && contentResult == CheckResult.Pass
            && profileResult == CheckResult.Pass
        )
        {
            await HandleGoodUserCounter(message, user, chat, stoppingToken);
        }
    }

    private async Task<CheckResult> CheckMessageContent(
        Message message,
        User user,
        string text,
        Chat chat,
        long admChat,
        CancellationToken stoppingToken
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Empty text/caption");
            if (message.Photo != null && _config.OpenRouterApi != null)
            {
                var spamCheck = await _aiChecks.GetSpamProbability(message, free: !_config.NonFreeChat(chat.Id));
                if (spamCheck.Probability >= Consts.LlmLowProbability)
                {
                    var reason = $"LLM думает что это спам {spamCheck.Probability * 100}%{Environment.NewLine}{spamCheck.Reason}";
                    if (spamCheck.Probability >= Consts.LlmHighProbability && !_config.MarketologsChats.Contains(chat.Id))
                    {
                        await DeleteAndReportMessage(message, reason, stoppingToken);
                        return CheckResult.NoMoreAction;
                    }
                    await DontDeleteButReportMessage(message, reason, stoppingToken);
                    return CheckResult.Suspicious;
                }
            }
            else if (!_config.NonFreeChat(chat.Id))
            {
                return CheckResult.Pass;
            }
            else
            {
                await DontDeleteButReportMessage(
                    message,
                    "пустое сообщение/подпись (вероятно картинка/видео/кружок/голосовуха)",
                    stoppingToken
                );
                return CheckResult.Suspicious;
            }
            return CheckResult.Pass;
        }
        if (_badMessageManager.KnownBadMessage(text))
        {
            _logger.LogDebug("KnownBadMessage");
            await HandleBadMessage(message, user, stoppingToken);
            return CheckResult.NoMoreAction;
        }
        var normalized = TextProcessor.NormalizeText(text);
        var lookalike = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(normalized);
        if (lookalike.Count > 2)
        {
            _logger.LogDebug("lookalike");
            var tailMessage = lookalike.Count > 5 ? ", и другие" : "";
            var reason = $"Были найдены слова маскирующиеся под русские: {string.Join(", ", lookalike.Take(5))}{tailMessage}";
            if (!_config.LookAlikeAutoBan || _config.MarketologsChats.Contains(chat.Id))
            {
                await DeleteAndReportMessage(message, reason, stoppingToken);
                return CheckResult.NoMoreAction;
            }

            await AutoBan(message, reason, stoppingToken);
            return CheckResult.NoMoreAction;
        }

        if (SimpleFilters.HasUnwantedChars(normalized))
        {
            const string reason = "В этом сообщении необычные буквы";

            if (_config.MarketologsChats.Contains(chat.Id))
            {
                await DontDeleteButReportMessage(message, reason, stoppingToken);
                return CheckResult.Suspicious;
            }
            if (_config.OpenRouterApi != null)
            {
                var spamCheck = await _aiChecks.GetSpamProbability(message, free: !_config.NonFreeChat(chat.Id));
                if (spamCheck.Probability >= Consts.LlmHighProbability)
                {
                    await AutoBan(message, $"{reason}{Environment.NewLine}{spamCheck.Reason}", stoppingToken);
                    return CheckResult.NoMoreAction;
                }
                await DeleteAndReportMessage(message, $"{reason}{Environment.NewLine}{spamCheck.Reason}", stoppingToken);
                return CheckResult.NoMoreAction;
            }
            await DeleteAndReportMessage(message, reason, stoppingToken);
            return CheckResult.NoMoreAction;
        }

        if (!_config.EmojiCheckDisabledChats.Contains(chat.Id) && SimpleFilters.TooManyEmojis(text))
        {
            _logger.LogDebug("TooManyEmojis");
            const string reason = "В этом сообщении многовато эмоджи";

            var firstLast = Utils.FullName(user);
            if (
                SimpleFilters.JustOneEmoji(text)
                && SimpleFilters.InUsernameSuspiciousList(firstLast)
                && !_config.MarketologsChats.Contains(chat.Id)
            )
            {
                await AutoBan(message, "один эмодзи и имя из блеклиста", stoppingToken);
                return CheckResult.NoMoreAction;
            }

            if (_config.MarketologsChats.Contains(chat.Id))
            {
                await DontDeleteButReportMessage(message, reason, stoppingToken);
                return CheckResult.Suspicious;
            }
            if (text.Length > 10 && _config.OpenRouterApi != null)
            {
                var spamCheck = await _aiChecks.GetSpamProbability(message, free: !_config.NonFreeChat(chat.Id));
                if (spamCheck.Probability >= Consts.LlmHighProbability)
                {
                    await AutoBan(message, $"{reason}{Environment.NewLine}{spamCheck.Reason}", stoppingToken);
                    return CheckResult.NoMoreAction;
                }
                await DeleteAndReportMessage(message, $"{reason}{Environment.NewLine}{spamCheck.Reason}", stoppingToken);
                return CheckResult.NoMoreAction;
            }
            await DeleteAndReportMessage(message, reason, stoppingToken);
            return CheckResult.NoMoreAction;
        }

        if (SimpleFilters.HasStopWords(normalized))
        {
            await DeleteAndReportMessage(message, "В этом сообщении есть стоп-слова", stoppingToken);
            return CheckResult.NoMoreAction;
        }
        _logger.LogDebug("Normalized:\n {Norm}", normalized);
        var (spam, score) = await _classifier.IsSpam(normalized);
        if (score > 0.3)
        {
            var reason = $"ML решил что это спам, скор {score}";
            if (score > 3 && _config.HighConfidenceAutoBan && !_config.MarketologsChats.Contains(chat.Id))
            {
                await AutoBan(message, reason, stoppingToken);
                return CheckResult.NoMoreAction;
            }
            if (_config.OpenRouterApi != null)
            {
                var spamCheck = await _aiChecks.GetSpamProbability(message, free: !_config.NonFreeChat(chat.Id));

                if (_config.MarketologsChats.Contains(chat.Id))
                {
                    await DontDeleteButReportMessage(message, $"{reason}{Environment.NewLine}{spamCheck.Reason}", stoppingToken);
                    return CheckResult.Suspicious;
                }
                if (spamCheck.Probability >= Consts.LlmHighProbability)
                {
                    await AutoBan(message, $"{reason}{Environment.NewLine}{spamCheck.Reason}", stoppingToken);
                    return CheckResult.NoMoreAction;
                }
                var llmScore = $"{Environment.NewLine}LLM оценивает вероятность спама в {spamCheck.Probability * 100}%:";
                await DeleteAndReportMessage(message, $"{reason}{llmScore}{Environment.NewLine}{spamCheck.Reason}", stoppingToken);
                return CheckResult.NoMoreAction;
            }
            await DeleteAndReportMessage(message, reason, stoppingToken);
            return CheckResult.NoMoreAction;
        }

        if (_config.OpenRouterApi != null && message.From != null)
        {
            var spamCheck = await _aiChecks.GetSpamProbability(message, free: !_config.NonFreeChat(message.Chat.Id));
            if (spamCheck.Probability >= Consts.LlmLowProbability)
            {
                var reason = $"LLM думает что это спам {spamCheck.Probability * 100}%{Environment.NewLine}{spamCheck.Reason}";
                if (spamCheck.Probability >= Consts.LlmHighProbability && !_config.MarketologsChats.Contains(chat.Id))
                {
                    await DeleteAndReportMessage(message, reason, stoppingToken);
                    return CheckResult.NoMoreAction;
                }
                await DontDeleteButReportMessage(message, reason, stoppingToken);
                return CheckResult.Suspicious;
            }
        }

        if (score > -0.5 && _config.LowConfidenceHamForward && _config.NonFreeChat(chat.Id))
        {
            var forward = await _bot.ForwardMessage(_config.AdminChatId, chat.Id, message.MessageId, cancellationToken: stoppingToken);
            var postLink = Utils.LinkToMessage(chat, message.MessageId);
            await _bot.SendMessage(
                _config.AdminChatId,
                $"Классифаер думает что это НЕ спам, но конфиденс низкий: скор {score}. Хорошая идея - добавить сообщение в датасет.{Environment.NewLine}Юзер {Utils.FullName(user)} из чата {chat.Title}{Environment.NewLine}{postLink}",
                replyParameters: forward,
                cancellationToken: stoppingToken
            );
        }
        if (!_config.NonFreeChat(chat.Id) && SimpleFilters.HasOnlyHelloWord(text))
        {
            await DontDeleteButReportMessage(message, "в этом сообщении написано привет и больше ничего, обычно это спамер", stoppingToken);
            return CheckResult.Suspicious;
        }

        _logger.LogDebug("Classifier thinks its ham, score {Score}", score);

        if (text.Length > 10)
        {
            var duplicates = _spamDeduplicationCache.AddAndCheck(
                text,
                chat.Id,
                chat.Title,
                message.Id,
                user.Id,
                user.FirstName,
                user.LastName
            );

            if (duplicates.Count > 0)
            {
                const string reason = "точно такое же сообщение было недавно в других чатах, в которых есть Швейцар, это подозрительно";

                await DontDeleteButReportMessage(message, reason, stoppingToken);

                foreach (var dup in duplicates)
                {
                    await DontDeleteButReportMessage(
                        new Message
                        {
                            Id = (int)dup.MessageId,
                            Chat = new Chat { Id = dup.ChatId, Title = dup.ChatTitle },
                            From = new User
                            {
                                Id = dup.UserId,
                                FirstName = dup.UserFirstName,
                                LastName = dup.UserLastName,
                            },
                        },
                        reason,
                        stoppingToken
                    );
                }
                return CheckResult.Suspicious;
            }
        }

        return CheckResult.Pass;
    }

    private async Task<CheckResult> CheckUserProfile(
        Message message,
        User user,
        string text,
        Chat chat,
        long admChat,
        CancellationToken stoppingToken
    )
    {
        if (_config.OpenRouterApi == null || message.From == null || !_config.NonFreeChat(message.Chat.Id))
            return CheckResult.Pass;

        var replyToRecentPost =
            message.ReplyToMessage?.IsAutomaticForward == true && DateTime.UtcNow - message.ReplyToMessage.Date < TimeSpan.FromMinutes(10);
        var (attention, photo, bio) = await _aiChecks.GetAttentionBaitProbability(
            message.From,
            async x =>
            {
                var alreadyBanned = await _userManager.InBanlist(message.From.Id);
                if (alreadyBanned)
                {
                    await AutoBan(message, $"{x}{Environment.NewLine}Теперь в банлисте", stoppingToken);
                    return;
                }
                await _aiChecks.ClearCache(message.From.Id);
                var (ascore, p, b) = await _aiChecks.GetAttentionBaitProbability(message.From, null, true);
                if (ascore.EroticProbability > Consts.LlmLowProbability)
                    await AutoBan(message, $"{x}{Environment.NewLine}эротика или полиция нравов", stoppingToken);
                else
                    await DontDeleteButReportMessage(message, x, stoppingToken);
            }
        );
        _logger.LogDebug("GetAttentionBaitProbability, result = {@Prob}", attention);
        var erotic = attention.EroticProbability >= Consts.LlmLowProbability;
        var money = attention.GamblingProbability >= Consts.LlmLowProbability;
        var nonHuman = attention.NonPersonProbability >= Consts.LlmLowProbability;
        var selfPromo = attention.SelfPromotionProbability >= Consts.LlmLowProbability;
        if (!erotic && !money && !nonHuman && !selfPromo)
            return CheckResult.Pass;

        ReplyParameters? replyParams = null;
        var textForReport = text;
        if (message.ReplyToMessage != null)
            textForReport = $"{text}{Environment.NewLine}реплай на {Utils.LinkToMessage(message.Chat, message.ReplyToMessage.MessageId)}";

        var reportCacheKey = $"user_reported_{chat.Id}_{user.Id}";
        var shouldReport = await _hybridCache.GetOrCreateAsync(
            reportCacheKey,
            _ => ValueTask.FromResult(true),
            new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromHours(8) },
            cancellationToken: stoppingToken
        );
        if (shouldReport)
            if (photo.Length != 0)
            {
                using var ms = new MemoryStream(photo);
                var photoMsg = await _bot.SendPhoto(
                    admChat,
                    new InputFileStream(ms),
                    $"{bio}{Environment.NewLine}Сообщение:{Environment.NewLine}{textForReport}{Environment.NewLine}{Utils.LinkToMessage(message.Chat, message.MessageId)}",
                    cancellationToken: stoppingToken
                );
                replyParams = photoMsg;
            }
            else
            {
                var textMsg = await _bot.SendMessage(
                    admChat,
                    $"{bio}{Environment.NewLine}Сообщение:{Environment.NewLine}{textForReport}{Environment.NewLine}{Utils.LinkToMessage(message.Chat, message.MessageId)}",
                    cancellationToken: stoppingToken
                );
                replyParams = textMsg;
            }

        if (replyToRecentPost)
            _logger.LogDebug("It's a reply to recent post, high alert");
        var bioInvite = bio.Contains("t.me/+");
        var bioObscured = SimpleFilters.FindAllRussianWordsWithLookalikeSymbols(bio).Count > 0;

        bool highErotic =
            attention.EroticProbability >= Consts.LlmHighProbability
            || (replyToRecentPost && attention.EroticProbability >= Consts.LlmLowProbability);
        bool highGambling = attention.GamblingProbability >= Consts.LlmHighProbability;
        bool highNonHuman = attention.NonPersonProbability >= Consts.LlmHighProbability;
        bool highSelfPromo = attention.SelfPromotionProbability >= Consts.LlmHighProbability && (bioInvite || bioObscured);
        var delete = highErotic || highGambling || highNonHuman || highSelfPromo;
        if (_config.MarketologsChats.Contains(chat.Id))
            delete = highErotic || highGambling;

        var msg = "Сообщение НЕ удалено";
        if (delete)
        {
            msg = "Сообщение УДАЛЕНО. Даём ридонли на 10 минут. Причина: ";
            if (highErotic)
                msg += "подозрение на эротику\n";
            if (highGambling)
                msg += "подозрение на быстрый заработок\n";
            if (highNonHuman)
                msg += "подозрение на бизнес-аккаунт\n";
            if (highSelfPromo)
                msg += "подозрение на селф-промо и ссылка на вступление в группу или маскировочные буквы в био\n";
        }

        if (attention.EroticProbability >= Consts.LlmLowProbability)
            msg += $"{Environment.NewLine}Вероятность что этот профиль связан с эротикой/порно: {attention.EroticProbability * 100}%";
        if (attention.GamblingProbability >= Consts.LlmLowProbability)
            msg +=
                $"{Environment.NewLine}Вероятность что этот профиль предлагает быстрый заработок: {attention.GamblingProbability * 100}%";
        if (attention.NonPersonProbability >= Consts.LlmLowProbability)
            msg +=
                $"{Environment.NewLine}Вероятность что этот профиль не человека, а бизнес-аккаунта:  {attention.NonPersonProbability * 100} %";
        if (attention.SelfPromotionProbability >= Consts.LlmLowProbability)
            msg +=
                $"{Environment.NewLine}Вероятность что этот профиль имеет элементы само-продвижения (включая невинные, типа личного блога): {attention.SelfPromotionProbability * 100} %";
        msg = $"{msg}{Environment.NewLine}{attention.Reason}";

        string? restoreKey = null;
        if (delete)
            restoreKey = $"restore_{IdGenerator.NextBase62()}";
        var keyboard = new List<InlineKeyboardButton> { new(Consts.BanButton) { CallbackData = $"banNoMark_{message.Chat.Id}_{user.Id}" } };
        if (delete)
        {
            keyboard.Add(new InlineKeyboardButton(Consts.OkButton) { CallbackData = $"attOk_{user.Id}_{restoreKey}" });
            keyboard.Add(new InlineKeyboardButton(Consts.ApproveButton) { CallbackData = $"approve_{user.Id}_{restoreKey}" });
        }
        else
        {
            keyboard.Add(new(Consts.OkButton) { CallbackData = $"attOk_{user.Id}" });
            keyboard.Add(new InlineKeyboardButton(Consts.ApproveButton) { CallbackData = $"approve_{user.Id}" });
        }

        if (shouldReport)
        {
            await _bot.SendMessage(
                admChat,
                msg,
                replyMarkup: new InlineKeyboardMarkup(keyboard),
                replyParameters: replyParams,
                cancellationToken: stoppingToken
            );
            await _hybridCache.SetAsync(
                reportCacheKey,
                false,
                new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromHours(8) },
                cancellationToken: stoppingToken
            );
        }

        if (delete)
        {
            var deletedInfo = new DeletedMessageInfo(
                restoreKey!,
                chat.Id,
                user.Id,
                user.FirstName,
                user.LastName,
                user.Username,
                message.Text,
                message.Caption,
                message.Photo?.LastOrDefault()?.FileId,
                message.Video?.FileId,
                message.ReplyToMessage?.MessageId,
                DeletionReason.UserProfile
            );
            _logger.LogDebug(
                "Storing deletedInfo in cache: key={RestoreKey}, chatId={ChatId}, userId={UserId}",
                restoreKey,
                chat.Id,
                user.Id
            );
            await _hybridCache.SetAsync(
                restoreKey!,
                deletedInfo,
                new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromHours(24) },
                cancellationToken: stoppingToken
            );
            await _bot.DeleteMessage(chat, message.Id, cancellationToken: stoppingToken);
            await _bot.RestrictChatMember(
                chat,
                user.Id,
                new ChatPermissions(false),
                untilDate: DateTime.UtcNow.AddMinutes(10),
                cancellationToken: stoppingToken
            );
            return CheckResult.NoMoreAction;
        }

        return CheckResult.Suspicious;
    }

    private async Task HandleGoodUserCounter(Message message, User user, Chat chat, CancellationToken stoppingToken)
    {
        var fullName = Utils.FullName(user);
        var lookalikeInName = SimpleFilters.FindAllRussianWordsWithLookalikeSymbols(fullName);
        if (lookalikeInName.Count > 0 && _config.NonFreeChat(chat.Id) && !await _userManager.IsHalfApproved(user.Id))
        {
            var reason = $"В имени пользователя найдены слова с маскирующимися символами: {string.Join(", ", lookalikeInName)}";
            await DontDeleteButReportMessageWithApprove(message, user.Id, reason, stoppingToken);
            return;
        }

        var goodInteractions = _goodUserMessages.AddOrUpdate(user.Id, 1, (_, oldValue) => oldValue + 1);
        if (goodInteractions >= 3)
        {
            _logger.LogInformation(
                "User {FullName} behaved well for the last {Count} messages, approving",
                Utils.FullName(user),
                goodInteractions
            );
            await _userManager.Approve(user.Id);
            _goodUserMessages.TryRemove(user.Id, out _);
        }
    }

    private async Task AutoBan(Message message, string reason, CancellationToken stoppingToken)
    {
        var user = message.From!;
        var fullName = Utils.FullName(user);
        var chat = message.Chat;
        _logger.LogDebug("Autoban. Chat: {Chat} {Id} User: {User}", chat.Title, chat.Id, fullName);
        var admChat = _config.AdminChatId;
        if (_config.NonFreeChat(chat.Id))
        {
            var forward = await _bot.ForwardMessage(admChat, chat.Id, message.MessageId, cancellationToken: stoppingToken);
            await _bot.SendMessage(
                admChat,
                $"Авто-бан: {reason}{Environment.NewLine}Юзер {fullName} из чата {chat.Title}{Environment.NewLine}{Utils.LinkToMessage(chat, message.MessageId)}",
                replyParameters: forward,
                cancellationToken: stoppingToken
            );
        }
        await _bot.DeleteMessage(chat, message.MessageId, cancellationToken: stoppingToken);
        var stats = _statistics.Stats.GetOrAdd(chat.Id, new Stats(chat.Title) { Id = chat.Id });
        stats.Autoban++;
        await _bot.BanChatMember(chat, user.Id, revokeMessages: false, cancellationToken: stoppingToken);
    }

    private void WatchNewcomer(Chat chat, User user, CancellationToken stoppingToken)
    {
        if (!_config.BlacklistAutoBan)
            return;

        var key = (chat.Id, user.Id);
        StopWatchingNewcomer(key);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        if (!_newcomersOnWatch.TryAdd(key, cts))
        {
            cts.Dispose();
            return;
        }

        CheckNewcomerBanlistLater(chat, user, key, cts, cts.Token).FireAndForget(_logger, nameof(CheckNewcomerBanlistLater));
    }

    private void StopWatchingNewcomer((long ChatId, long UserId) key)
    {
        if (!_newcomersOnWatch.TryRemove(key, out var cts))
            return;

        cts.Cancel();
        cts.Dispose();
    }

    private async Task CheckNewcomerBanlistLater(
        Chat chat,
        User user,
        (long ChatId, long UserId) key,
        CancellationTokenSource cts,
        CancellationToken stoppingToken
    )
    {
        try
        {
            var previousCheck = TimeSpan.Zero;
            foreach (var checkAfter in NewcomerBanlistCheckAfterJoin)
            {
                await Task.Delay(checkAfter - previousCheck, stoppingToken);
                previousCheck = checkAfter;

                if (!await _userManager.InBanlist(user.Id))
                    continue;

                _logger.LogInformation(
                    "Newcomer in banlist. Chat {ChatId} {ChatTitle}, User {UserId} {User}, Joined check {Hours}h",
                    chat.Id,
                    chat.Title,
                    user.Id,
                    Utils.FullName(user),
                    checkAfter.TotalHours
                );

                try
                {
                    var stats = _statistics.Stats.GetOrAdd(chat.Id, new Stats(chat.Title) { Id = chat.Id });
                    stats.BlacklistBanned++;
                    await _bot.BanChatMember(chat.Id, user.Id, revokeMessages: false, cancellationToken: stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Unable to ban newcomer from delayed banlist check");
                    if (_config.NonFreeChat(chat.Id))
                    {
                        await _bot.SendMessage(
                            _config.GetAdminChat(chat.Id),
                            $"Не могу забанить нового участника из блеклиста. Не хватает могущества? Сходите забаньте руками, чат {chat.Title}",
                            cancellationToken: stoppingToken
                        );
                    }
                }

                return;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            _logger.LogWarning(e, nameof(CheckNewcomerBanlistLater));
        }
        finally
        {
            if (_newcomersOnWatch.TryGetValue(key, out var currentCts) && ReferenceEquals(currentCts, cts))
                _newcomersOnWatch.TryRemove(key, out _);
            cts.Dispose();
        }
    }

    private async Task HandleBadMessage(Message message, User user, CancellationToken stoppingToken)
    {
        try
        {
            var chat = message.Chat;
            var stats = _statistics.Stats.GetOrAdd(chat.Id, new Stats(chat.Title) { Id = chat.Id });
            stats.KnownBadMessage++;
            await _bot.DeleteMessage(chat, message.MessageId, stoppingToken);
            await _bot.BanChatMember(chat.Id, user.Id, cancellationToken: stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to ban");
        }
    }

    private async Task HandleChatMemberUpdated(Update update, CancellationToken stoppingToken)
    {
        var chatMember = update.ChatMember;
        Debug.Assert(chatMember != null);
        var newChatMember = chatMember.NewChatMember;
        var key = (chatMember.Chat.Id, newChatMember.User.Id);
        switch (newChatMember.Status)
        {
            case ChatMemberStatus.Member:
            {
                if (chatMember.OldChatMember.Status == ChatMemberStatus.Left)
                {
                    _logger.LogDebug(
                        "New chat member in chat {Chat}: {First} {Last} @{Username}; Id = {Id}",
                        chatMember.Chat.Title,
                        newChatMember.User.FirstName,
                        newChatMember.User.LastName,
                        newChatMember.User.Username,
                        newChatMember.User.Id
                    );
                    await _captchaManager.IntroFlow(newChatMember.User, chatMember.Chat);
                    WatchNewcomer(chatMember.Chat, newChatMember.User, stoppingToken);
                }
                break;
            }
            case ChatMemberStatus.Kicked or ChatMemberStatus.Restricted:
                StopWatchingNewcomer(key);
                if (!_config.NonFreeChat(chatMember.Chat.Id))
                    break;

                var action = newChatMember.Status == ChatMemberStatus.Kicked ? "забанил(а)" : "дал(а) ридонли";
                var user = newChatMember.User;
                var messages = _recentMessagesStorage.Get(user.Id, chatMember.Chat.Id);
                var lastMessage = messages.LastOrDefault();
                var lastMessageText = lastMessage?.Text ?? lastMessage?.Caption;
                var tailMessage = string.IsNullOrWhiteSpace(lastMessageText)
                    ? "Если его забанили за спам, а ML не распознал спам - киньте его сообщение сюда."
                    : $"Его/её последним сообщением было:{Environment.NewLine}{lastMessageText}";
                var mentionAt = user.Username != null ? $" @{user.Username}" : "";
                await _bot.SendMessage(
                    _config.GetAdminChat(chatMember.Chat.Id),
                    $"В чате {chatMember.Chat.Title} юзеру {Utils.FullName(user)}{mentionAt} {action} {Utils.FullName(chatMember.From)}. {tailMessage}"
                );
                break;
            case ChatMemberStatus.Left:
                StopWatchingNewcomer(key);
                break;
        }
    }

    private async Task DontDeleteButReportMessage(Message message, string? reason = null, CancellationToken stoppingToken = default)
    {
        _logger.LogDebug("DontDeleteButReportMessage");
        if (!_config.NonFreeChat(message.Chat.Id))
            return;
        var fromChat = message.SenderChat;
        var user = message.From!;
        var admChat = _config.GetAdminChat(message.Chat.Id);
        Message? forward = null;
        try
        {
            forward = await _bot.ForwardMessage(admChat, message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
        }
        catch (ApiRequestException are)
        {
            _logger.LogInformation(are, "Cannot forward");
        }
        var callbackData = fromChat == null ? $"ban_{message.Chat.Id}_{user.Id}" : $"banchan_{message.Chat.Id}_{fromChat.Id}";

        var postLink = Utils.LinkToMessage(message.Chat, message.MessageId);
        var reply = "";
        if (message.ReplyToMessage != null)
            reply = $"{Environment.NewLine}реплай на {Utils.LinkToMessage(message.Chat, message.ReplyToMessage.MessageId)}";

        var msg =
            reason
            ?? "Это подозрительное сообщение - например, картинка/видео/кружок/голосовуха без подписи от 'нового' юзера, или сообщение от канала";
        var editedMessageNote = message.EditDate != null ? $"{Environment.NewLine}Проверка редактированного сообщения (не оригинала)" : "";
        await _bot.SendMessage(
            admChat,
            $"Сообщение НЕ удалено{editedMessageNote}{Environment.NewLine}{msg}{Environment.NewLine}Юзер {Utils.FullName(user)} из чата {message.Chat.Title}{Environment.NewLine}{postLink}{reply}",
            replyParameters: forward,
            replyMarkup: new InlineKeyboardMarkup(
                new InlineKeyboardButton(Consts.BanButton) { CallbackData = callbackData },
                new InlineKeyboardButton(Consts.OkButton) { CallbackData = "noop" }
            ),
            cancellationToken: stoppingToken
        );
    }

    private async Task DontDeleteButReportMessageWithApprove(
        Message message,
        long userId,
        string? reason = null,
        CancellationToken stoppingToken = default
    )
    {
        _logger.LogDebug("DontDeleteButReportMessageWithApprove");
        var user = message.From!;
        var admChat = _config.GetAdminChat(message.Chat.Id);
        Message? forward = null;
        try
        {
            forward = await _bot.ForwardMessage(admChat, message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
        }
        catch (ApiRequestException are)
        {
            _logger.LogInformation(are, "Cannot forward");
        }

        var postLink = Utils.LinkToMessage(message.Chat, message.MessageId);
        var reply = "";
        if (message.ReplyToMessage != null)
            reply = $"{Environment.NewLine}реплай на {Utils.LinkToMessage(message.Chat, message.ReplyToMessage.MessageId)}";

        var msg =
            reason
            ?? "Это подозрительное сообщение - например, картинка/видео/кружок/голосовуха без подписи от 'нового' юзера, или сообщение от канала";
        await _bot.SendMessage(
            admChat,
            $"Сообщение НЕ удалено{Environment.NewLine}{msg}{Environment.NewLine}Юзер {Utils.FullName(user)} из чата {message.Chat.Title}{Environment.NewLine}{postLink}{reply}",
            replyParameters: forward,
            replyMarkup: new InlineKeyboardMarkup(
                new InlineKeyboardButton(Consts.BanButton) { CallbackData = $"banNoMark_{message.Chat.Id}_{user.Id}" },
                new InlineKeyboardButton(Consts.OkButton) { CallbackData = $"attOk_{userId}" },
                new InlineKeyboardButton(Consts.ApproveButton) { CallbackData = $"approve_{userId}" }
            ),
            cancellationToken: stoppingToken
        );
    }

    private async Task DeleteAndReportMessage(Message message, string reason, CancellationToken stoppingToken)
    {
        _logger.LogDebug("DeleteAndReportMessage");
        var admChat = _config.GetAdminChat(message.Chat.Id);

        var user = message.From!;
        var fromChat = message.SenderChat;
        Message? forward = null;
        if (_config.NonFreeChat(message.Chat.Id))
            try
            {
                forward = await _bot.ForwardMessage(admChat, message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
            }
            catch (ApiRequestException are)
            {
                _logger.LogInformation(are, "Cannot forward");
            }

        var restoreKey = $"restore_{IdGenerator.NextBase62()}";
        _logger.LogDebug("DeleteAndReportMessage: generated restoreKey={RestoreKey}", restoreKey);
        var deletedInfo = new DeletedMessageInfo(
            restoreKey,
            message.Chat.Id,
            user.Id,
            user.FirstName,
            user.LastName,
            user.Username,
            message.Text,
            message.Caption,
            message.Photo?.LastOrDefault()?.FileId,
            message.Video?.FileId,
            message.ReplyToMessage?.MessageId
        );
        await _hybridCache.SetAsync(
            restoreKey,
            deletedInfo,
            new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromHours(24) },
            cancellationToken: stoppingToken
        );
        _logger.LogDebug(
            "Stored deletedInfo in cache: key={RestoreKey}, chatId={ChatId}, userId={UserId}",
            restoreKey,
            message.Chat.Id,
            user.Id
        );

        var deletionMessagePart = $"Причина: {reason}";
        try
        {
            await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
            await _bot.RestrictChatMember(
                message.Chat.Id,
                user!.Id,
                new ChatPermissions(false),
                untilDate: DateTime.UtcNow.AddMinutes(10),
                cancellationToken: stoppingToken
            );
            deletionMessagePart = $"Сообщение УДАЛЕНО. Даём ридонли на 10 минут. {deletionMessagePart}";
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to delete");
            deletionMessagePart =
                $"Сообщение НЕ удалено или юзеру не дали ридонли (не хватило могущества?). {deletionMessagePart}";
        }

        if (!_config.NonFreeChat(message.Chat.Id))
            return;

        var callbackDataBan = fromChat == null ? $"ban_{message.Chat.Id}_{user.Id}" : $"banchan_{message.Chat.Id}_{fromChat.Id}";
        var postLink = Utils.LinkToMessage(message.Chat, message.MessageId);
        var reply = "";
        if (message.ReplyToMessage != null)
            reply = $"{Environment.NewLine}реплай на {Utils.LinkToMessage(message.Chat, message.ReplyToMessage.MessageId)}";

        var row = new List<InlineKeyboardButton>([
            new InlineKeyboardButton(Consts.BanButton) { CallbackData = callbackDataBan },
            new InlineKeyboardButton(Consts.RestoreButton) { CallbackData = restoreKey },
            new InlineKeyboardButton(Consts.ApproveButton) { CallbackData = $"approve_{user.Id}_{restoreKey}" },
        ]);

        var username = user.Username == null ? "" : $" @{user.Username}";
        await _bot.SendMessage(
            admChat,
            $"{deletionMessagePart}{Environment.NewLine}Юзер {Utils.FullName(user)}{username} из чата {message.Chat.Title}{Environment.NewLine}{postLink}{reply}",
            replyParameters: forward,
            replyMarkup: new InlineKeyboardMarkup(row),
            cancellationToken: stoppingToken
        );
    }
}

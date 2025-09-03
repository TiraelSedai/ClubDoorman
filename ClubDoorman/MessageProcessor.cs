using System.Collections.Concurrent;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman;

internal class MessageProcessor
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<MessageProcessor> _logger;
    private readonly SpamHamClassifier _classifier;
    private readonly UserManager _userManager;
    private readonly BadMessageManager _badMessageManager;
    private readonly AiChecks _aiChecks;
    private readonly CaptchaManager _captchaManager;
    private readonly ConcurrentDictionary<long, int> _goodUserMessages = new();
    private readonly StatisticsReporter _statistics;
    private readonly Config _config;
    private readonly ReactionHandler _reactionHandler;
    private readonly AdminCommandHandler _adminCommandHandler;
    private readonly RecentMessagesStorage _recentMessagesStorage;
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
        RecentMessagesStorage recentMessagesStorage
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
    }

    public async Task HandleUpdate(Update update, CancellationToken stoppingToken)
    {
        using var logScope = _logger.BeginScope("Update Id = {Id}", update.Id);
        // TODO: this is not ideal, share getter with AdminCommandHandler
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
            await HandleChatMemberUpdated(update);
            return;
        }

        var message = update.EditedMessage ?? update.Message;
        if (message == null)
            return;

        var chat = message.Chat;
        if (chat.Type == ChatType.Private)
        {
            await _bot.SendMessage(
                chat,
                "Сорян, я не отвечаю в личке, если вы хотели написать создателю - в описании",
                replyParameters: message,
                cancellationToken: stoppingToken
            );
            return;
        }
        if (message.NewChatMembers != null && chat.Id != _config.AdminChatId && !_config.MultiAdminChatMap.Values.Contains(chat.Id))
        {
            foreach (var newUser in message.NewChatMembers.Where(x => !x.IsBot))
                await _captchaManager.IntroFlow(message, newUser);
            return;
        }
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
            // to get linked_chat_id we need ChatFullInfo
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
                        Message? fwd = null;
                        if (_config.NonFreeChat(chat.Id))
                            fwd = await _bot.ForwardMessage(_config.AdminChatId, chat, message.MessageId, cancellationToken: stoppingToken);
                        await _bot.DeleteMessage(chat, message.MessageId, stoppingToken);
                        await _bot.BanChatSenderChat(chat, message.SenderChat.Id, stoppingToken);
                        var stats = _statistics.Stats.GetOrAdd(chat.Id, new Stats(chat.Title) { Id = chat.Id });
                        stats.Channels++;
                        if (_config.NonFreeChat(chat.Id))
                            await _bot.SendMessage(
                                _config.AdminChatId,
                                $"Сообщение удалено, в чате {chat.Title} забанен канал {message.SenderChat.Title}",
                                replyParameters: fwd!,
                                cancellationToken: stoppingToken
                            );
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
                return;
            }
        }

        var user = message.From!;
        var text = message.Text ?? message.Caption;

        if (message.Quote?.Text != null)
            text = $"> {message.Quote.Text}{Environment.NewLine}{text}";

        if (_captchaManager.IsCaptchaNeeded(chat.Id, user))
        {
            await _bot.DeleteMessage(chat.Id, message.MessageId, stoppingToken);
            return;
        }

        if (_userManager.Approved(user.Id))
        {
            if (!_config.ApprovedUsersMlSpamCheck || string.IsNullOrWhiteSpace(text))
                return;

            if (!_config.NonFreeChat(message.Chat.Id))
                return;

            var normalized_ = TextProcessor.NormalizeText(text);
            if (normalized_.Length < 10)
                return;
            var (spam_, score_) = await _classifier.IsSpam(normalized_);
            if (!spam_)
                return;

            var fwd = await _bot.ForwardMessage(_config.AdminChatId, message.Chat, message.MessageId, cancellationToken: stoppingToken);
            await _bot.SendMessage(
                _config.AdminChatId,
                $"ML решил что это спам, скор {score_}, но пользователь в доверенных. Возможно стоит добавить в ham, чат {chat.Title} {Utils.LinkToMessage(chat, message.MessageId)}",
                replyParameters: fwd,
                cancellationToken: stoppingToken
            );
            return;
        }

        _logger.LogDebug("First-time message, chat {Chat} user {User}, message {Message}", chat.Title, Utils.FullName(user), text);
        _recentMessagesStorage.Add(user.Id, chat.Id, message);

        // At this point we are believing we see first-timers, and we need to check for spam
        var name = await _userManager.GetClubUsername(user.Id);
        if (!string.IsNullOrEmpty(name))
        {
            _logger.LogDebug("User is {Name} from club", name);
            return;
        }
        if (await _userManager.InBanlist(user.Id))
        {
            _logger.LogDebug("InBanlist");
            if (_config.BlacklistAutoBan)
            {
                var stats = _statistics.Stats.GetOrAdd(chat.Id, new Stats(chat.Title) { Id = chat.Id });
                stats.BlacklistBanned++;
                await _bot.BanChatMember(chat.Id, user.Id, revokeMessages: false, cancellationToken: stoppingToken);
                await _bot.DeleteMessage(chat.Id, message.MessageId, stoppingToken);
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
        if (message.Sticker != null)
        {
            _logger.LogDebug("Sticker");
            return;
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Empty text/caption");
            if (!_config.NonFreeChat(chat.Id))
                return;
            if (message.Photo != null && _config.OpenRouterApi != null)
            {
                var spamCheck = await _aiChecks.GetSpamProbability(message);
                if (spamCheck.Probability >= Consts.LlmLowProbability)
                {
                    var reason = $"LLM думает что это спам {spamCheck.Probability * 100}%{Environment.NewLine}{spamCheck.Reason}";
                    if (spamCheck.Probability >= Consts.LlmHighProbability)
                        await DeleteAndReportMessage(message, reason, stoppingToken);
                    else
                        await DontDeleteButReportMessage(message, reason, stoppingToken);
                }
            }
            else
            {
                await DontDeleteButReportMessage(message, "картинка/видео/кружок/голосовуха без подписи", stoppingToken);
            }
            return;
        }
        if (_badMessageManager.KnownBadMessage(text))
        {
            _logger.LogDebug("KnownBadMessage");
            await HandleBadMessage(message, user, stoppingToken);
            return;
        }
        var normalized = TextProcessor.NormalizeText(text);
        var lookalike = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(normalized);
        if (lookalike.Count > 2)
        {
            _logger.LogDebug("lookalike");
            var tailMessage = lookalike.Count > 5 ? ", и другие" : "";
            var reason = $"Были найдены слова маскирующиеся под русские: {string.Join(", ", lookalike.Take(5))}{tailMessage}";
            if (!_config.LookAlikeAutoBan)
            {
                await DeleteAndReportMessage(message, reason, stoppingToken);
                return;
            }

            await AutoBan(message, reason, stoppingToken);
            return;
        }

        if (SimpleFilters.HasUnwantedChars(normalized))
        {
            const string reason = "В этом сообщении необычные буквы";
            if (_config.OpenRouterApi != null && _config.NonFreeChat(chat.Id))
            {
                var spamCheck = await _aiChecks.GetSpamProbability(message);
                if (spamCheck.Probability >= Consts.LlmHighProbability)
                    await AutoBan(message, $"{reason}{Environment.NewLine}{spamCheck.Reason}", stoppingToken);
                else
                    await DeleteAndReportMessage(message, $"{reason}{Environment.NewLine}{spamCheck.Reason}", stoppingToken);
            }
            else
            {
                await DeleteAndReportMessage(message, reason, stoppingToken);
            }
            return;
        }

        if (SimpleFilters.TooManyEmojis(text))
        {
            _logger.LogDebug("TooManyEmojis");
            const string reason = "В этом сообщении многовато эмоджи";
            if (text.Length > 10 && _config.OpenRouterApi != null && _config.NonFreeChat(chat.Id))
            {
                var spamCheck = await _aiChecks.GetSpamProbability(message);
                if (spamCheck.Probability >= Consts.LlmHighProbability)
                    await AutoBan(message, $"{reason}{Environment.NewLine}{spamCheck.Reason}", stoppingToken);
                else
                    await DeleteAndReportMessage(message, $"{reason}{Environment.NewLine}{spamCheck.Reason}", stoppingToken);
            }
            else
            {
                await DeleteAndReportMessage(message, reason, stoppingToken);
            }
            return;
        }

        if (SimpleFilters.HasStopWords(normalized))
        {
            const string reason = "В этом сообщении есть стоп-слова";
            await DeleteAndReportMessage(message, reason, stoppingToken);
            return;
        }
        _logger.LogDebug("Normalized:\n {Norm}", normalized);
        var (spam, score) = await _classifier.IsSpam(normalized);
        if (spam)
        {
            var reason = $"ML решил что это спам, скор {score}";
            if (score > 3 && _config.HighConfidenceAutoBan)
            {
                await AutoBan(message, reason, stoppingToken);
            }
            else
            {
                if (_config.OpenRouterApi != null && _config.NonFreeChat(chat.Id))
                {
                    var spamCheck = await _aiChecks.GetSpamProbability(message);
                    if (spamCheck.Probability >= Consts.LlmHighProbability)
                        await AutoBan(message, $"{reason}{Environment.NewLine}{spamCheck.Reason}", stoppingToken);
                    else
                        await DeleteAndReportMessage(message, $"{reason}{Environment.NewLine}{spamCheck.Reason}", stoppingToken);
                }
                else
                {
                    await DeleteAndReportMessage(message, reason, stoppingToken);
                }
            }
            return;
        }

        var userAttentionSpammer = false;
        if (_config.OpenRouterApi != null && message.From != null && _config.NonFreeChat(message.Chat.Id))
        {
            var replyToRecentPost =
                message.ReplyToMessage?.IsAutomaticForward == true
                && DateTime.UtcNow - message.ReplyToMessage.Date < TimeSpan.FromMinutes(10);
            var (attention, photo, bio) = await _aiChecks.GetAttentionBaitProbability(
                message.From,
                async x =>
                {
                    var alreadyBanned = await _userManager.InBanlist(message.From.Id);
                    if (alreadyBanned)
                        await AutoBan(message, $"{x}{Environment.NewLine}Теперь в банлисте", stoppingToken);
                    await _aiChecks.ClearCache(message.From.Id);
                    var (ascore, p, b) = await _aiChecks.GetAttentionBaitProbability(message.From);
                    if (ascore.Probability > Consts.LlmLowProbability)
                        await AutoBan(message, $"{x}{Environment.NewLine}{ascore.Reason}", stoppingToken);
                    else
                        await DontDeleteButReportMessage(message, x, stoppingToken);
                }
            );
            _logger.LogDebug("GetAttentionBaitProbability, result = {@Prob}", attention);
            if (attention.Probability >= Consts.LlmLowProbability)
            {
                userAttentionSpammer = true;
                var keyboard = new List<InlineKeyboardButton>
                {
                    new(Consts.BanButton) { CallbackData = $"ban_{message.Chat.Id}_{user.Id}" },
                    new(Consts.OkButton) { CallbackData = $"attOk_{user.Id}" },
                };

                ReplyParameters? replyParams = null;
                if (message.ReplyToMessage != null)
                    text = $"{text}{Environment.NewLine}реплай на {Utils.LinkToMessage(message.Chat, message.ReplyToMessage.MessageId)}";
                
                if (photo.Length != 0)
                {
                    using var ms = new MemoryStream(photo);
                    var photoMsg = await _bot.SendPhoto(
                        admChat,
                        new InputFileStream(ms),
                        $"{bio}{Environment.NewLine}Сообщение:{Environment.NewLine}{text}",
                        cancellationToken: stoppingToken
                    );
                    replyParams = photoMsg;
                }
                else
                {
                    var textMsg = await _bot.SendMessage(
                        admChat,
                        $"{bio}{Environment.NewLine}Сообщение:{Environment.NewLine}{text}",
                        cancellationToken: stoppingToken
                    );
                    replyParams = textMsg;
                }

                if (replyToRecentPost)
                    _logger.LogDebug("It's a reply to recent post, high alert");
                var delete = attention.Probability >= Consts.LlmHighProbability || replyToRecentPost;

                var action = delete ? "Даём ридонли на 10 минут; " : "";
                var at = user.Username == null ? "" : $" @{user.Username} ";
                await _bot.SendMessage(
                    admChat,
                    $"{action}Вероятность что это профиль бейт спаммер {attention.Probability * 100}%.{Environment.NewLine}{attention.Reason}{Environment.NewLine}Юзер {Utils.FullName(user)}{at} из чата {chat.Title}",
                    replyMarkup: new InlineKeyboardMarkup(keyboard),
                    replyParameters: null,
                    cancellationToken: stoppingToken
                );

                if (delete)
                {
                    await _bot.DeleteMessage(chat, message.Id, cancellationToken: stoppingToken);
                    await _bot.RestrictChatMember(
                        chat,
                        user.Id,
                        new ChatPermissions(false),
                        untilDate: DateTime.UtcNow.AddMinutes(10),
                        cancellationToken: stoppingToken
                    );
                }
            }
        }
        if (_config.OpenRouterApi != null && message.From != null && _config.NonFreeChat(message.Chat.Id))
        {
            var spamCheck = await _aiChecks.GetSpamProbability(message);
            if (spamCheck.Probability >= Consts.LlmLowProbability)
            {
                var reason = $"LLM думает что это спам {spamCheck.Probability * 100}%{Environment.NewLine}{spamCheck.Reason}";
                if (spamCheck.Probability >= Consts.LlmHighProbability)
                    await DeleteAndReportMessage(message, reason, stoppingToken);
                else
                    await DontDeleteButReportMessage(message, reason, stoppingToken);
            }
        }

        // else - ham
        if (score > -0.5 && _config.LowConfidenceHamForward && _config.NonFreeChat(message.Chat.Id))
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
        _logger.LogDebug("Classifier thinks its ham, score {Score}", score);

        // Now we need a mechanism for users who have been writing non-spam for some time
        if (update.Message != null && !userAttentionSpammer)
        {
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

    private async Task HandleChatMemberUpdated(Update update)
    {
        var chatMember = update.ChatMember;
        Debug.Assert(chatMember != null);
        var newChatMember = chatMember.NewChatMember;
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
                    await _captchaManager.IntroFlow(null, newChatMember.User, chatMember.Chat);
                }
                break;
            }
            case ChatMemberStatus.Kicked or ChatMemberStatus.Restricted:
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
        }
    }

    private async Task DontDeleteButReportMessage(Message message, string? reason = null, CancellationToken stoppingToken = default)
    {
        _logger.LogDebug("DontDeleteButReportMessage");
        var fromChat = message.SenderChat;
        var user = message.From!;
        var admChat = _config.GetAdminChat(message.Chat.Id);
        var forward = await _bot.ForwardMessage(admChat, message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
        var callbackData = fromChat == null ? $"ban_{message.Chat.Id}_{user.Id}" : $"banchan_{message.Chat.Id}_{fromChat.Id}";

        var postLink = Utils.LinkToMessage(message.Chat, message.MessageId);
        var reply = "";
        if (message.ReplyToMessage != null)
            reply = $"{Environment.NewLine}реплай на {Utils.LinkToMessage(message.Chat, message.ReplyToMessage.MessageId)}";

        var msg =
            reason
            ?? "Это подозрительное сообщение - например, картинка/видео/кружок/голосовуха без подписи от 'нового' юзера, или сообщение от канала";
        await _bot.SendMessage(
            admChat,
            $"{msg}. Сообщение НЕ удалено.{Environment.NewLine}Юзер {Utils.FullName(user)} из чата {message.Chat.Title}{Environment.NewLine}{postLink}{reply}",
            replyParameters: forward.MessageId,
            replyMarkup: new InlineKeyboardMarkup(
                new InlineKeyboardButton(Consts.BanButton) { CallbackData = callbackData },
                new InlineKeyboardButton(Consts.OkButton) { CallbackData = "noop" }
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
            forward = await _bot.ForwardMessage(admChat, message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
        var deletionMessagePart = reason;
        try
        {
            await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
            deletionMessagePart += ", сообщение удалено. Юзеру дали ридонли на 10 мин";
            await _bot.RestrictChatMember(
                message.Chat.Id,
                user!.Id,
                new ChatPermissions(false),
                untilDate: DateTime.UtcNow.AddMinutes(10),
                cancellationToken: stoppingToken
            );
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to delete");
            deletionMessagePart += ", сообщение НЕ удалено (не хватило могущества?).";
        }

        if (!_config.NonFreeChat(message.Chat.Id))
            return;

        var callbackDataBan = fromChat == null ? $"ban_{message.Chat.Id}_{user.Id}" : $"banchan_{message.Chat.Id}_{fromChat.Id}";
        var postLink = Utils.LinkToMessage(message.Chat, message.MessageId);
        var reply = "";
        if (message.ReplyToMessage != null)
            reply = $"{Environment.NewLine}реплай на {Utils.LinkToMessage(message.Chat, message.ReplyToMessage.MessageId)}";

        var row = new List<InlineKeyboardButton>(
            [
                new InlineKeyboardButton(Consts.BanButton) { CallbackData = callbackDataBan },
                new InlineKeyboardButton(Consts.OkButton) { CallbackData = "noop" },
            ]
        );
        if (_config.ApproveButtonEnabled)
            row.Add(new InlineKeyboardButton("🥰🥰🥰 approve") { CallbackData = $"approve_{user.Id}" });

        var username = user.Username == null ? "" : $" @{user.Username}";
        await _bot.SendMessage(
            admChat,
            $"{deletionMessagePart}{Environment.NewLine}Юзер {Utils.FullName(user)}{username} из чата {message.Chat.Title}{Environment.NewLine}{postLink}{reply}",
            replyParameters: forward!,
            replyMarkup: new InlineKeyboardMarkup(row),
            cancellationToken: stoppingToken
        );
    }
}

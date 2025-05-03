using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Caching;
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
    private readonly ReactionHandler _reactionHandler;
    private readonly AdminCommandHandler _adminCommandHandler;
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
        ReactionHandler reactionHandler,
        AdminCommandHandler adminCommandHandler
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
        _reactionHandler = reactionHandler;
        _adminCommandHandler = adminCommandHandler;
    }

    public async Task HandleUpdate(Update update, CancellationToken stoppingToken)
    {
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

            if (msg == null || msg.Chat.Id == Config.AdminChatId || Config.MultiAdminChatMap.Values.Contains(msg.Chat.Id))
                await _adminCommandHandler.HandleAdminCallback(cb.Data, cb);
            else
                await _captchaManager.HandleCaptchaCallback(update);
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

        var chat = message.Chat;
        if (chat.Type == ChatType.Private)
        {
            await _bot.SendMessage(chat, "Сорян, я не отвечаю в личке", replyParameters: message, cancellationToken: stoppingToken);
            return;
        }
        if (message.NewChatMembers != null && chat.Id != Config.AdminChatId && !Config.MultiAdminChatMap.Values.Contains(chat.Id))
        {
            foreach (var newUser in message.NewChatMembers.Where(x => !x.IsBot))
                await _captchaManager.IntroFlow(message, newUser); // Handled by CaptchaManager
            return;
        }
        if (chat.Id == Config.AdminChatId || Config.MultiAdminChatMap.Values.Contains(chat.Id))
        {
            await _adminCommandHandler.AdminChatMessage(message);
            return;
        }
        var admChat = Config.GetAdminChat(chat.Id);

        if (message.SenderChat != null)
        {
            if (message.SenderChat.Id == chat.Id)
                return;
            if (message.IsAutomaticForward)
                return;
            // to get linked_chat_id we need ChatFullInfo
            var chatFull = await _bot.GetChat(chat, stoppingToken);
            var linked = chatFull.LinkedChatId;
            if (linked != null && linked == message.SenderChat.Id)
                return;

            if (Config.ChannelAutoBan && !Config.ChannelsCheckExclusionChats.Contains(chat.Id))
            {
                try
                {
                    var subs = await _bot.GetChatMemberCount(message.SenderChat.Id, cancellationToken: stoppingToken);
                    if (subs > Consts.BigChannelSubsCount)
                    {
                        _logger.LogDebug("Popular channel {Ch}, not banning", message.SenderChat.Title);
                        return;
                    }

                    var fwd = await _bot.ForwardMessage(admChat, chat, message.MessageId, cancellationToken: stoppingToken);
                    await _bot.DeleteMessage(chat, message.MessageId, stoppingToken);
                    await _bot.BanChatSenderChat(chat, message.SenderChat.Id, stoppingToken);
                    await _bot.SendMessage(
                        admChat,
                        $"Сообщение удалено, в чате {chat.Title} забанен канал {message.SenderChat.Title}",
                        replyParameters: fwd,
                        cancellationToken: stoppingToken
                    );
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Unable to ban");
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

        var user = message.From!;
        var text = message.Text ?? message.Caption;

        if (text != null)
            MemoryCache.Default.Set(
                new CacheItem($"{chat.Id}_{user.Id}", text),
                new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) }
            );

        if (_captchaManager.IsCaptchaNeeded(chat.Id, user))
        {
            await _bot.DeleteMessage(chat.Id, message.MessageId, stoppingToken);
            return;
        }

        if (_userManager.Approved(user.Id))
            return;
        _logger.LogDebug("First-time message, chat {Chat} user {User} message {Id}, message {Message}", chat.Title, Utils.FullName(user), message.Id, text);

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
            if (Config.BlacklistAutoBan)
            {
                var stats = _statistics.Stats.GetOrAdd(chat.Id, new Stats(chat.Title));
                Interlocked.Increment(ref stats.BlacklistBanned);
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
                Config.ButtonAutoBan
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
        if (message.Sticker!=null)
        {
            _logger.LogDebug("Sticker");
            return;
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Empty text/caption");
            await DontDeleteButReportMessage(message, "картинка/видео/кружок/голосовуха без подписи", stoppingToken);
            return;
        }
        if (_badMessageManager.KnownBadMessage(text))
        {
            _logger.LogDebug("KnownBadMessage");
            await HandleBadMessage(message, user, stoppingToken);
            return;
        }
        if (SimpleFilters.TooManyEmojis(text))
        {
            _logger.LogDebug("TooManyEmojis");
            const string reason = "В этом сообщении многовато эмоджи";
            await DeleteAndReportMessage(message, reason, stoppingToken);
            return;
        }

        var normalized = TextProcessor.NormalizeText(text);

        var lookalike = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(normalized);
        if (lookalike.Count > 2)
        {
            _logger.LogDebug("lookalike");
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
            if (score > 3 && Config.HighConfidenceAutoBan)
                await AutoBan(message, reason, stoppingToken);
            else
                await DeleteAndReportMessage(message, reason, stoppingToken);
            return;
        }
        if (
            Config.OpenRouterApi != null
            && message.From != null
            && (Config.MultiAdminChatMap.Count == 0 || Config.MultiAdminChatMap.ContainsKey(message.Chat.Id))
        )
        {
            _logger.LogDebug("Message {Id} GetAttentionBaitProbability start", message.Id);
            var (attentionProb, photo, bio) = await _aiChecks.GetAttentionBaitProbability(message.From);
            _logger.LogDebug("Message {Id} GetAttentionBaitProbability end, result = {Prob}", message.Id, attentionProb);
            if (attentionProb >= Consts.LlmLowProbability)
            {
                var keyboard = new List<InlineKeyboardButton>
                {
                    new("👍 ok") { CallbackData = $"attOk_{user.Id}" },
                    new("🤖 ban") { CallbackData = $"ban_{message.Chat.Id}_{user.Id}" },
                };

                ReplyParameters? replyParams = null;
                if (photo.Length != 0)
                {
                    using var ms = new MemoryStream(photo);
                    var photoMsg = await _bot.SendPhoto(
                        admChat,
                        new InputFileStream(ms),
                        $"{bio}{Environment.NewLine}Сообщение специально не привожу, потому что должно быть понятно без контекста, если это аттеншн-бейт",
                        cancellationToken: stoppingToken
                    );
                    replyParams = photoMsg;
                }
                var action = attentionProb >= Consts.LlmHighProbability ? "Даём ридонли на 15 минут" : "";
                await _bot.SendMessage(
                    admChat,
                    $"Вероятность что это профиль бейт спаммер {attentionProb * 100}%. {action}{Environment.NewLine}Юзер {Utils.FullName(user)} из чата {chat.Title}",
                    replyMarkup: new InlineKeyboardMarkup(keyboard),
                    replyParameters: replyParams,
                    cancellationToken: stoppingToken
                );
                if (attentionProb >= Consts.LlmHighProbability)
                {
                    await _bot.DeleteMessage(chat, message.Id, cancellationToken: stoppingToken);
                    await _bot.RestrictChatMember(
                        chat,
                        user.Id,
                        new ChatPermissions(false),
                        untilDate: DateTime.UtcNow.AddMinutes(15),
                        cancellationToken: stoppingToken
                    );
                }
            }
        }
        if (
            Config.OpenRouterApi != null
            && message.From != null
            && (Config.MultiAdminChatMap.Count == 0 || Config.MultiAdminChatMap.ContainsKey(message.Chat.Id))
        )
        {
            var prob = await _aiChecks.GetSpamProbability(message);
            if (prob >= Consts.LlmLowProbability)
            {
                if (prob >= Consts.LlmHighProbability)
                    await DeleteAndReportMessage(message, $"LLM сказал что вероятность что это спам {prob}", stoppingToken);
                else
                    await DontDeleteButReportMessage(message, $"LLM сказал что вероятность что это спам {prob}", stoppingToken);
            }
        }

        // else - ham
        if (
            score > -0.5
            && Config.LowConfidenceHamForward
            && (Config.MultiAdminChatMap.Count == 0 || Config.MultiAdminChatMap.ContainsKey(message.Chat.Id))
        )
        {
            var forward = await _bot.ForwardMessage(Config.AdminChatId, chat.Id, message.MessageId, cancellationToken: stoppingToken);
            var postLink = Utils.LinkToMessage(chat, message.MessageId);
            await _bot.SendMessage(
                Config.AdminChatId,
                $"Классифаер думает что это НЕ спам, но конфиденс низкий: скор {score}. Хорошая идея - добавить сообщение в датасет.{Environment.NewLine}Юзер {Utils.FullName(user)} из чата {chat.Title}{Environment.NewLine}{postLink}",
                replyParameters: forward,
                cancellationToken: stoppingToken
            );
        }
        _logger.LogDebug("Classifier thinks its ham, score {Score}", score);

        // Now we need a mechanism for users who have been writing non-spam for some time
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
        _logger.LogDebug("Autoban. Chat: {Chat} {Id} User: {User}", message.Chat.Title, message.Chat.Id, fullName);
        if (Config.MultiAdminChatMap.Count == 0 || Config.MultiAdminChatMap.ContainsKey(message.Chat.Id))
        {
            var admChat = Config.GetAdminChat(message.Chat.Id);
            var forward = await _bot.ForwardMessage(admChat, message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
            await _bot.SendMessage(
                admChat,
                $"Авто-бан: {reason}{Environment.NewLine}Юзер {fullName} из чата {message.Chat.Title}{Environment.NewLine}{Utils.LinkToMessage(message.Chat, message.MessageId)}",
                replyParameters: forward,
                cancellationToken: stoppingToken
            );
        }
        await _bot.DeleteMessage(message.Chat, message.MessageId, cancellationToken: stoppingToken);
        await _bot.BanChatMember(message.Chat, user.Id, revokeMessages: false, cancellationToken: stoppingToken);
    }

    private async Task HandleBadMessage(Message message, User user, CancellationToken stoppingToken)
    {
        try
        {
            var chat = message.Chat;
            var stats = _statistics.Stats.GetOrAdd(chat.Id, new Stats(chat.Title));
            Interlocked.Increment(ref stats.KnownBadMessage);
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
                        "New chat member in chat {Chat}: {First} {Last}; Id = {Id}",
                        chatMember.Chat.Title,
                        newChatMember.User.FirstName,
                        newChatMember.User.LastName,
                        newChatMember.User.Id
                    );
                    await _captchaManager.IntroFlow(null, newChatMember.User, chatMember.Chat);
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
                    : $" Его/её последним сообщением было:{Environment.NewLine}{lastMessage}";
                await _bot.SendMessage(
                    Config.GetAdminChat(chatMember.Chat.Id),
                    $"В чате {chatMember.Chat.Title} юзеру {Utils.FullName(user)} tg://user?id={user.Id} дали ридонли или забанили, посмотрите в Recent actions, возможно ML пропустил спам. Если это так - кидайте его сюда.{tailMessage}"
                );
                break;
        }
    }

    private async Task DontDeleteButReportMessage(Message message, string? reason = null, CancellationToken stoppingToken = default)
    {
        _logger.LogDebug("DontDeleteButReportMessage");
        var fromChat = message.SenderChat;
        var user = message.From!;
        var admChat = Config.GetAdminChat(message.Chat.Id);
        var forward = await _bot.ForwardMessage(admChat, message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
        var callbackData = fromChat == null ? $"ban_{message.Chat.Id}_{user.Id}" : $"banchan_{message.Chat.Id}_{fromChat.Id}";
        MemoryCache.Default.Add(callbackData, message, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });
        var msg =
            reason
            ?? "Это подозрительное сообщение - например, картинка/видео/кружок/голосовуха без подписи от 'нового' юзера, или сообщение от канала";
        await _bot.SendMessage(
            admChat,
            $"{msg}. Сообщение НЕ удалено.{Environment.NewLine}Юзер {Utils.FullName(user)} из чата {message.Chat.Title}",
            replyParameters: forward.MessageId,
            replyMarkup: new InlineKeyboardMarkup(
                new InlineKeyboardButton("🤖 ban") { CallbackData = callbackData },
                new InlineKeyboardButton("👍 ok") { CallbackData = "noop" }
            ),
            cancellationToken: stoppingToken
        );
    }

    private async Task DeleteAndReportMessage(Message message, string reason, CancellationToken stoppingToken)
    {
        _logger.LogDebug("DeleteAndReportMessage");
        var admChat = Config.GetAdminChat(message.Chat.Id);

        var user = message.From;
        var fromChat = message.SenderChat;
        var forward = await _bot.ForwardMessage(admChat, message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
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

        var callbackDataBan = fromChat == null ? $"ban_{message.Chat.Id}_{user.Id}" : $"banchan_{message.Chat.Id}_{fromChat.Id}";
        MemoryCache.Default.Add(callbackDataBan, message, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1) });
        var postLink = Utils.LinkToMessage(message.Chat, message.MessageId);
        var row = new List<InlineKeyboardButton>(
            [
                new InlineKeyboardButton("🤖 бан") { CallbackData = callbackDataBan },
                new InlineKeyboardButton("😶 пропуск") { CallbackData = "noop" },
            ]
        );
        if (Config.ApproveButtonEnabled)
            row.Add(new InlineKeyboardButton("🥰 свой") { CallbackData = $"approve_{user.Id}" });

        await _bot.SendMessage(
            admChat,
            $"{deletionMessagePart}{Environment.NewLine}Юзер {Utils.FullName(user)} из чата {message.Chat.Title}{Environment.NewLine}{postLink}",
            replyParameters: forward,
            replyMarkup: new InlineKeyboardMarkup(row),
            cancellationToken: stoppingToken
        );
    }
}

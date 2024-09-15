using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman;

public class Worker(ILogger<Worker> logger, SpamHamClassifier classifier, UserManager userManager) : BackgroundService
{
    private record CaptchaInfo(
        long ChatId,
        DateTime Timestamp,
        User User,
        int CorrectAnswer,
        CancellationTokenSource Cts,
        Message? UserJoinedMessage
    );

    private readonly ConcurrentDictionary<string, CaptchaInfo> _captchaNeededUsers = new();
    private readonly ConcurrentDictionary<long, int> _goodUserMessages = new();
    private readonly TelegramBotClient _bot = new(Config.BotApi);
    private Dictionary<long, (string Title, List<string> Users)> _banned = new();
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));
    private User _me = default!;

    private async Task CaptchaLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), token);
            _ = BanNoCaptchaUsers();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = CaptchaLoop(stoppingToken);
        _ = ReportBanned(stoppingToken);
        classifier.Touch();
        const string offsetPath = "data/offset.txt";
        var offset = 0;
        if (System.IO.File.Exists(offsetPath))
        {
            var lines = await System.IO.File.ReadAllLinesAsync(offsetPath, stoppingToken);
            if (lines.Length > 0 && int.TryParse(lines[0], out offset))
                logger.LogDebug("offset read ok");
        }

        _me = await _bot.GetMeAsync(cancellationToken: stoppingToken);

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
                logger.LogError(e, "ExecuteAsync");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }

    private async Task<int> UpdateLoop(int offset, CancellationToken stoppingToken)
    {
        var updates = await _bot.GetUpdatesAsync(
            offset,
            limit: 100,
            timeout: 100,
            allowedUpdates: [UpdateType.Message, UpdateType.ChatMember, UpdateType.CallbackQuery],
            cancellationToken: stoppingToken
        );
        if (updates.Length == 0)
            return offset;
        offset = updates.Max(x => x.Id) + 1;
        foreach (var update in updates)
        {
            try
            {
                await HandleUpdate(update, stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "UpdateLoop");
            }
        }
        return offset;
    }

    private async Task HandleUpdate(Update update, CancellationToken stoppingToken)
    {
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

        var message = update.Message;
        if (message == null)
            return;
        if (message.NewChatMembers != null && message.Chat.Id != Config.AdminChatId)
        {
            foreach (var newUser in message.NewChatMembers.Where(x => !x.IsBot))
                await IntroFlow(message, newUser);
            return;
        }

        if (message.Chat.Id == Config.AdminChatId)
        {
            await AdminChatMessage(message);
            return;
        }

        if (message.SenderChat != null)
            return;

        var user = message.From!;
        var text = message.Text ?? message.Caption;

        MemoryCache.Default.Set(
            new CacheItem($"{message.Chat.Id}_{user.Id}", text),
            new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) }
        );

        var key = UserToKey(message.Chat.Id, user);
        if (_captchaNeededUsers.ContainsKey(key))
        {
            await _bot.DeleteMessageAsync(message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
            return;
        }

        if (userManager.Approved(user.Id))
            return;

        logger.LogDebug("First-time message, chat {Chat}, message {Message}", message.Chat.Title, text);

        // At this point we are believing we see first-timers, and we need to check for spam
        var name = await userManager.GetClubUsername(user.Id);
        if (!string.IsNullOrEmpty(name))
        {
            logger.LogDebug("User is {Name} from club", name);
            return;
        }
        if (await userManager.InBanlist(user.Id))
        {
            if (Config.BlacklistAutoBan)
            {
                await _bot.BanChatMemberAsync(message.Chat.Id, user.Id, revokeMessages: false, cancellationToken: stoppingToken);
                await _bot.DeleteMessageAsync(message.Chat.Id, message.MessageId, stoppingToken);
                _banned.TryAdd(message.Chat.Id, (message.Chat.Title ?? "", []));
                var list = _banned[message.Chat.Id].Users;
                list.Add(FullName(user.FirstName, user.LastName));
            }
            else
            {
                const string reason = "Пользователь в блеклисте спамеров";
                await DeleteAndReportMessage(message, user, reason, stoppingToken);
            }
            return;
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogDebug("Empty text/caption");
            await DontDeleteButReportMessage(message, user, stoppingToken);
            return;
        }
        if (SimpleFilters.TooManyEmojis(text))
        {
            const string reason = "В этом сообщении многовато эмоджи";
            await DeleteAndReportMessage(message, user, reason, stoppingToken);
            return;
        }

        var normalized = TextProcessor.NormalizeText(text);

        var lookalike = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(normalized);
        if (lookalike.Count > 1)
        {
            var tailMessage = lookalike.Count > 5 ? ", и другие" : "";
            var reason = $"Были найдены слова маскирующиеся под русские: {string.Join(", ", lookalike.Take(5))}{tailMessage}";
            await DeleteAndReportMessage(message, user, reason, stoppingToken);
            return;
        }

        if (SimpleFilters.HasStopWords(normalized))
        {
            const string reason = "В этом сообщении есть стоп-слова";
            await DeleteAndReportMessage(message, user, reason, stoppingToken);
            return;
        }
        var (spam, score) = await classifier.IsSpam(normalized);
        if (spam)
        {
            var reason = $"ML решил что это спам, скор {score}";
            await DeleteAndReportMessage(message, user, reason, stoppingToken);
            return;
        }
        else
        {
            logger.LogDebug("Classifier thinks its ham, score {Score}", score);
        }

        // Now we need a mechanism for users who have been writing non-spam for some time
        var goodInteractions = _goodUserMessages.AddOrUpdate(user.Id, 1, (_, oldValue) => oldValue + 1);
        if (goodInteractions >= 5)
        {
            logger.LogInformation(
                "User {FullName} behaved well for the last {Count} messages, approving",
                FullName(user.FirstName, user.LastName),
                goodInteractions
            );
            await userManager.Approve(user.Id);
            _goodUserMessages.TryRemove(user.Id, out _);
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
            await _bot.AnswerCallbackQueryAsync(cb.Id);
            return;
        }

        var key = UserToKey(message.Chat.Id, cb.From);
        var ok = _captchaNeededUsers.TryRemove(key, out var info);
        await _bot.DeleteMessageAsync(message.Chat, message.MessageId);
        if (!ok)
        {
            logger.LogWarning("{Key} was not found in the dictionary _captchaNeededUsers", key);
            return;
        }
        Debug.Assert(info != null);
        await info.Cts.CancelAsync();
        if (info.CorrectAnswer != chosen)
        {
            await _bot.BanChatMemberAsync(message.Chat, userId, DateTime.UtcNow + TimeSpan.FromMinutes(20), revokeMessages: false);
            if (info.UserJoinedMessage != null)
                await _bot.DeleteMessageAsync(message.Chat, info.UserJoinedMessage.MessageId);
            UnbanUserLater(message.Chat, userId);
        }
        // Else: Theoretically we could approve user here, but I've seen spammers who can pass captcha and then post spam
    }

    private async ValueTask IntroFlow(Message? userJoinMessage, User user, Chat? chat = default)
    {
        logger.LogDebug("Intro flow {@User}", user);
        if (userManager.Approved(user.Id))
            return;
        var clubUser = await userManager.GetClubUsername(user.Id);
        if (clubUser != null)
            return;

        chat = userJoinMessage?.Chat ?? chat;
        Debug.Assert(chat != null);
        var chatId = chat.Id;

        if (await BanIfBlacklisted(user, userJoinMessage?.Chat ?? chat))
            return;

        var key = UserToKey(chatId, user);
        if (_captchaNeededUsers.ContainsKey(key))
        {
            logger.LogDebug("This user is already awaiting captcha challenge");
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

        var del =
            userJoinMessage != null
                ? await _bot.SendTextMessageAsync(
                    chatId,
                    $"Привет! Антиспам: на какой кнопке {Captcha.CaptchaList[correctAnswer].Description}?",
                    replyToMessageId: userJoinMessage.MessageId,
                    replyMarkup: new InlineKeyboardMarkup(keyboard)
                )
                : await _bot.SendTextMessageAsync(
                    chatId,
                    $"Привет {AtUserNameOrFirstLast()}! Антиспам: на какой кнопке {Captcha.CaptchaList[correctAnswer].Description}?",
                    replyMarkup: new InlineKeyboardMarkup(keyboard)
                );

        var cts = new CancellationTokenSource();
        DeleteMessageLater(del, TimeSpan.FromMinutes(1.2), cts.Token);
        if (userJoinMessage != null)
        {
            DeleteMessageLater(userJoinMessage, TimeSpan.FromMinutes(1.2), cts.Token);
            _captchaNeededUsers.TryAdd(key, new CaptchaInfo(chatId, DateTime.UtcNow, user, correctAnswer, cts, userJoinMessage));
        }
        else
        {
            _captchaNeededUsers.TryAdd(key, new CaptchaInfo(chatId, DateTime.UtcNow, user, correctAnswer, cts, null));
        }

        return;

        string AtUserNameOrFirstLast()
        {
            if (user.Username != null)
                return "@" + user.Username;
            return FullName(user.FirstName, user.LastName);
        }
    }

    private async Task ReportBanned(CancellationToken ct)
    {
        while (await _timer.WaitForNextTickAsync(ct))
        {
            if (DateTimeOffset.UtcNow.Hour != 12)
                continue;

            var report = _banned;
            _banned = [];
            if (report.Sum(x => x.Value.Users.Count) == 0)
                continue;
            var sb = new StringBuilder();

            sb.Append("За последние 24 часа были забанены по блеклистам:");
            foreach (var (_, (title, users)) in report.OrderBy(x => x.Value.Title))
            {
                sb.Append(Environment.NewLine);
                sb.Append("В ");
                sb.Append(title);
                sb.Append($": {users.Count} пользователь(ей){Environment.NewLine}");
                sb.Append(string.Join(", ", users.Take(5)));
                if (users.Count > 5)
                    sb.Append(", и другие");
            }

            try
            {
                await _bot.SendTextMessageAsync(Config.AdminChatId, sb.ToString(), cancellationToken: ct);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Unable to sent report to admin chat");
            }
        }
    }

    private async Task<bool> BanIfBlacklisted(User user, Chat chat)
    {
        if (!Config.BlacklistAutoBan)
            return false;
        if (!await userManager.InBanlist(user.Id))
            return false;

        try
        {
            await _bot.BanChatMemberAsync(chat.Id, user.Id);
            _banned.TryAdd(chat.Id, (chat.Title ?? "", []));
            var list = _banned[chat.Id].Users;
            list.Add(FullName(user.FirstName, user.LastName));
            return true;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Unable to ban");
            await _bot.SendTextMessageAsync(
                Config.AdminChatId,
                $"Не могу забанить юзера из блеклиста. Не хватает могущества? Сходите забаньте руками, чат {chat.Title}"
            );
        }

        return false;
    }

    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";

    private async Task HandleAdminCallback(string cbData, CallbackQuery cb)
    {
        var split = cbData.Split('_').ToList();
        if (split.Count > 2 && split[0] == "ban" && long.TryParse(split[1], out var chatId) && long.TryParse(split[2], out var userId))
        {
            try
            {
                await _bot.BanChatMemberAsync(new ChatId(chatId), userId);
                await _bot.SendTextMessageAsync(
                    new ChatId(Config.AdminChatId),
                    $"{FullName(cb.From.FirstName, cb.From.LastName)} забанил",
                    replyToMessageId: cb.Message?.MessageId
                );
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Unable to ban");
                await _bot.SendTextMessageAsync(
                    new ChatId(Config.AdminChatId),
                    $"Не могу забанить. Не хватает могущества? Сходите забаньте руками",
                    replyToMessageId: cb.Message?.MessageId
                );
            }
        }
        var msg = cb.Message;
        if (msg != null)
            await _bot.EditMessageReplyMarkupAsync(msg.Chat.Id, msg.MessageId);
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
                logger.LogDebug("New chat member new {@New} old {@Old}", newChatMember, chatMember.OldChatMember);
                if (chatMember.OldChatMember.Status == ChatMemberStatus.Left)
                {
                    // The reason we need to wait here is that we need to get message that user joined to have a chance to be processed first,
                    // this is not mandatory but looks nicer, however sometimes Telegram doesn't send it at all so consider this a fallback.
                    // There is no way real human would be able to solve this captcha in under 2 seconds so it's fine.
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
                    : $" Его/её последним сообщенимем было:{Environment.NewLine}{lastMessage}";
                await _bot.SendTextMessageAsync(
                    new ChatId(Config.AdminChatId),
                    $"В чате {chatMember.Chat.Title} юзеру {FullName(user.FirstName, user.LastName)} tg://user?id={user.Id} дали ридонли или забанили, посмотрите в Recent actions, возможно ML пропустил спам. Если это так - кидайте его сюда.{tailMessage}"
                );
                break;
        }
    }

    private async Task DontDeleteButReportMessage(Message message, User user, CancellationToken stoppingToken)
    {
        var forward = await _bot.ForwardMessageAsync(
            new ChatId(Config.AdminChatId),
            message.Chat.Id,
            message.MessageId,
            cancellationToken: stoppingToken
        );
        var callbackData = $"ban_{message.Chat.Id}_{user.Id}";
        await _bot.SendTextMessageAsync(
            new ChatId(Config.AdminChatId),
            $"Картинка/видео/кружок/голосовуха без подписи от 'нового' юзера, иногда это спам. Сообщение НЕ удалено.{Environment.NewLine}Юзер {FullName(user.FirstName, user.LastName)} из чата {message.Chat.Title}",
            replyToMessageId: forward.MessageId,
            replyMarkup: new InlineKeyboardMarkup(
                [
                    new InlineKeyboardButton("🤖 ban") { CallbackData = callbackData },
                    new InlineKeyboardButton("👍 ok") { CallbackData = "noop" }
                ]
            ),
            cancellationToken: stoppingToken
        );
    }

    private async Task DeleteAndReportMessage(Message message, User user, string reason, CancellationToken stoppingToken)
    {
        var forward = await _bot.ForwardMessageAsync(
            new ChatId(Config.AdminChatId),
            message.Chat.Id,
            message.MessageId,
            cancellationToken: stoppingToken
        );
        var deletionMessagePart = $"{reason}";
        try
        {
            await _bot.DeleteMessageAsync(message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);
            deletionMessagePart += ", сообщение удалено.";
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Unable to delete");
            deletionMessagePart += ", сообщение НЕ удалено (не хватило могущества?).";
        }

        var callbackData = $"ban_{message.Chat.Id}_{user.Id}";
        var postLink = LinkToMessage(message.Chat, message.MessageId);

        await _bot.SendTextMessageAsync(
            new ChatId(Config.AdminChatId),
            $"{deletionMessagePart}{Environment.NewLine}Юзер {FullName(user.FirstName, user.LastName)} из чата {message.Chat.Title}{Environment.NewLine}{postLink}",
            replyToMessageId: forward.MessageId,
            replyMarkup: new InlineKeyboardMarkup(
                [
                    new InlineKeyboardButton("🤖 ban") { CallbackData = callbackData },
                    new InlineKeyboardButton("👍 ok") { CallbackData = "noop" }
                ]
            ),
            cancellationToken: stoppingToken
        );
    }

    private static string LinkToMessage(Chat chat, long messageId) =>
        chat.Type == ChatType.Supergroup
            ? LinkToSuperGroupMessage(chat, messageId)
            : chat.Username == null
                ? ""
                : LinkToGroupWithNameMessage(chat, messageId);

    private static string LinkToSuperGroupMessage(Chat chat, long messageId) => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}";

    private static string LinkToGroupWithNameMessage(Chat chat, long messageId) => $"https://t.me/{chat.Username}/{messageId}";

    private async Task AdminChatMessage(Message message)
    {
        if (message is { ReplyToMessage: { } replyToMessage, Text: "/spam" or "/ham" or "/check" })
        {
            if (replyToMessage.From?.Id == _me.Id && replyToMessage.ForwardFrom == null && replyToMessage.ForwardSenderName == null)
            {
                await _bot.SendTextMessageAsync(
                    message.Chat.Id,
                    "Похоже что вы промахнулись и реплайнули на сообщение бота, а не форвард",
                    replyToMessageId: replyToMessage.MessageId
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
                        var (spam, score) = await classifier.IsSpam(normalized);
                        var lookAlikeMsg = lookalike.Count == 0 ? "отсутствуют" : string.Join(", ", lookalike);
                        var msg =
                            $"Результат:{Environment.NewLine}"
                            + $"Много эмодзи: {emojis}{Environment.NewLine}"
                            + $"Найдены стоп-слова: {hasStopWords}{Environment.NewLine}"
                            + $"Маскирующиеся слова: {lookAlikeMsg}{Environment.NewLine}"
                            + $"ML классификатор: спам {spam}, скор {score}{Environment.NewLine}{Environment.NewLine}"
                            + $"Если простые фильтры отработали, то в датасет добавлять не нужно";
                        await _bot.SendTextMessageAsync(message.Chat.Id, msg);
                        break;
                    }
                    case "/spam":
                        await classifier.AddSpam(replyToMessage.Text ?? replyToMessage.Caption ?? string.Empty);
                        await _bot.SendTextMessageAsync(
                            message.Chat.Id,
                            "Сообщение добавлено как пример спама в датасет",
                            replyToMessageId: replyToMessage.MessageId
                        );
                        break;
                    case "/ham":
                        await classifier.AddHam(replyToMessage.Text ?? replyToMessage.Caption ?? string.Empty);
                        await _bot.SendTextMessageAsync(
                            message.Chat.Id,
                            "Сообщение добавлено как пример НЕ-спама в датасет",
                            replyToMessageId: replyToMessage.MessageId
                        );
                        break;
                }
            }
        }
    }

    private static string UserToKey(long chatId, User user) => $"{chatId}_{user.Id}";

    private async Task BanNoCaptchaUsers()
    {
        if (_captchaNeededUsers.IsEmpty)
            return;
        var now = DateTime.UtcNow;
        var users = _captchaNeededUsers.ToArray();
        foreach (var (key, (chatId, timestamp, user, _, _, _)) in users)
        {
            var minutes = (now - timestamp).TotalMinutes;
            if (minutes > 1)
            {
                _captchaNeededUsers.TryRemove(key, out _);
                await _bot.BanChatMemberAsync(chatId, user.Id, now + TimeSpan.FromMinutes(20), revokeMessages: false);
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
                await _bot.UnbanChatMemberAsync(chatId, userId);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, nameof(UnbanUserLater));
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
                    await _bot.DeleteMessageAsync(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "DeleteMessageAsync");
                }
            },
            cancellationToken
        );
    }
}

using System.Collections.Concurrent;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman;

public class Worker(ILogger<Worker> logger, SpamHamClassifier classifier, UserManager userManager, SimpleFilters simpleFilters)
    : BackgroundService
{
    private record CaptchaInfo(Message Message, DateTime Timestamp, User User, int CorrectAnswer);

    private readonly ConcurrentDictionary<string, CaptchaInfo> _captchaNeededUsers = new();
    private readonly ConcurrentDictionary<long, int> _goodUserMessages = new();
    private readonly TelegramBotClient _bot = new(Config.BotApi);

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
        classifier.Touch();
        const string offsetPath = "data/offset.txt";
        var offset = 0;
        if (System.IO.File.Exists(offsetPath))
        {
            var lines = await System.IO.File.ReadAllLinesAsync(offsetPath, stoppingToken);
            if (lines.Length > 0 && int.TryParse(lines[0], out offset))
                logger.LogDebug("offset read ok");
        }

        var me = await _bot.GetMeAsync(cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                offset = await UpdateLoop(offset, me, stoppingToken);
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

    private async Task<int> UpdateLoop(int offset, User me, CancellationToken stoppingToken)
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
                await HandleUpdate(update, me, stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "UpdateLoop");
            }
        }
        return offset;
    }

    private async Task HandleUpdate(Update update, User me, CancellationToken stoppingToken)
    {
        if (update.CallbackQuery != null)
        {
            await HandleCallback(update);
            return;
        }
        if (update.ChatMember != null)
        {
            if (update.ChatMember.From.Id == me.Id)
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

        var user = message.From!;
        var text = message.Text ?? message.Caption;

        var key = MessageToKey(message, user);
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
            await userManager.Approve(user.Id);
            logger.LogDebug("User is {Name} from club", name);
            return;
        }
        if (userManager.InBanlist(user.Id))
        {
            const string reason = "Пользователь в блеклисте спамеров";
            await DeleteAndReportMessage(message, user, reason, stoppingToken);
            return;
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogDebug("Empty text/caption");
            return;
        }
        if (SimpleFilters.TooManyEmojis(text))
        {
            const string reason = "В этом сообщени многовато эмоджи";
            await DeleteAndReportMessage(message, user, reason, stoppingToken);
            return;
        }

        var normalized = TextProcessor.NormalizeText(text);

        var lookalike = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(normalized);
        if (lookalike.Count > 0)
        {
            var reason = $"Были найдены слова маскирующиеся под русские: {string.Join(", ", lookalike)}";
            await DeleteAndReportMessage(message, user, reason, stoppingToken);
            return;
        }

        if (simpleFilters.HasStopWords(normalized))
        {
            const string reason = "В этом сообщени есть стоп-слова";
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
                "User {First} {Last} behaved well for the last {Count} messages, approving",
                user.FirstName,
                user.LastName,
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

        var key = MessageToKey(message, cb.From);
        var ok = _captchaNeededUsers.TryRemove(key, out var info);
        await _bot.DeleteMessageAsync(message.Chat, message.MessageId);
        if (!ok)
        {
            logger.LogWarning("{Key} was not found in the dictionary _captchaNeededUsers", key);
            return;
        }
        Debug.Assert(info != null);
        if (info.CorrectAnswer != chosen)
        {
            await _bot.BanChatMemberAsync(message.Chat, userId, DateTime.UtcNow + TimeSpan.FromMinutes(20), revokeMessages: false);
            await _bot.DeleteMessageAsync(message.Chat, info.Message.MessageId);
        }
        // Else: Theoretically we could approve user here, but I've seen spammers who can pass captcha and then post spam
    }

    private async ValueTask IntroFlow(Message message, User user)
    {
        if (userManager.Approved(user.Id))
            return;
        var clubUser = await userManager.GetClubUsername(user.Id);
        if (clubUser != null)
        {
            await userManager.Approve(user.Id);
            return;
        }

        const int challengeLength = 8;
        Debug.Assert(challengeLength % 2 == 0);

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

        var del = await _bot.SendTextMessageAsync(
            message.Chat.Id,
            $"Привет! Антиспам: на какой кнопке {Captcha.CaptchaList[correctAnswer].Description}?",
            replyToMessageId: message.MessageId,
            replyMarkup: new InlineKeyboardMarkup(keyboard)
        );
        DeleteMessageLater(del, TimeSpan.FromMinutes(1.15));
        var key = MessageToKey(message, user);
        _captchaNeededUsers.TryAdd(key, new CaptchaInfo(message, DateTime.UtcNow, user, correctAnswer));
    }

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
                    $"{cb.From.FirstName} {cb.From.LastName} забанил",
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
        var user = update.ChatMember;
        Debug.Assert(user != null);

        if (user.NewChatMember.Status is ChatMemberStatus.Kicked or ChatMemberStatus.Restricted)
            await _bot.SendTextMessageAsync(
                new ChatId(Config.AdminChatId),
                $"В чате {user.Chat.Title} кому-то дали ридонли или забанили, посмотрите в Recent actions, возможно ML пропустил спам. Если это так - кидайте его сюда"
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
        await _bot.DeleteMessageAsync(message.Chat.Id, message.MessageId, cancellationToken: stoppingToken);

        var cbd = $"ban_{message.Chat.Id}_{user.Id}";
        await _bot.SendTextMessageAsync(
            new ChatId(Config.AdminChatId),
            $"{reason}, сообщение удалено.{Environment.NewLine}Юзер {user.FirstName} {user.LastName}; Чат {message.Chat.Title}",
            replyToMessageId: forward.MessageId,
            replyMarkup: new InlineKeyboardMarkup(
                [new InlineKeyboardButton("🤖 ban") { CallbackData = cbd }, new InlineKeyboardButton("👍 ok") { CallbackData = "noop" }]
            ),
            cancellationToken: stoppingToken
        );
    }

    private async Task AdminChatMessage(Message message)
    {
        if (message is { ReplyToMessage: not null, Text: "/spam" or "/ham" or "/classify" or "/lookalike" })
        {
            var text = message.ReplyToMessage.Text ?? message.ReplyToMessage.Caption;
            if (!string.IsNullOrWhiteSpace(text))
            {
                switch (message.Text)
                {
                    case "/lookalike":
                    {
                        var words = SimpleFilters.FindAllRussianWordsWithLookalikeSymbols(text);
                        if (words.Count != 0)
                            await _bot.SendTextMessageAsync(message.Chat.Id, $"Найдены слова: {string.Join(", ", words)}");
                        else
                            await _bot.SendTextMessageAsync(message.Chat.Id, $"Слова не найдены");
                        break;
                    }
                    case "/classify":
                    {
                        var normalized = TextProcessor.NormalizeText(text);
                        var (spam, score) = await classifier.IsSpam(normalized);
                        var messageClass = spam ? "Спам" : "Не спам";
                        await _bot.SendTextMessageAsync(message.Chat.Id, $"{messageClass}, скор {score}");
                        break;
                    }
                    case "/spam":
                        await classifier.AddSpam(message.ReplyToMessage.Text ?? message.ReplyToMessage.Caption ?? string.Empty);
                        await _bot.SendTextMessageAsync(
                            message.Chat.Id,
                            "Сообщение добавлено как пример спама в датасет",
                            replyToMessageId: message.ReplyToMessage.MessageId
                        );
                        break;
                    case "/ham":
                        await classifier.AddHam(message.ReplyToMessage.Text ?? message.ReplyToMessage.Caption ?? string.Empty);
                        await _bot.SendTextMessageAsync(
                            message.Chat.Id,
                            "Сообщение добавлено как пример НЕ-спама в датасет",
                            replyToMessageId: message.ReplyToMessage.MessageId
                        );
                        break;
                }
            }
        }
    }

    private static string MessageToKey(Message message, User user) => $"{message.Chat.Id}_{user.Id}";

    private async Task BanNoCaptchaUsers()
    {
        if (_captchaNeededUsers.IsEmpty)
            return;
        var now = DateTime.UtcNow;
        var users = _captchaNeededUsers.ToArray();
        foreach (var (key, (message, timestamp, user, _)) in users)
        {
            var minutes = (now - timestamp).TotalMinutes;
            if (minutes > 1)
            {
                _captchaNeededUsers.TryRemove(key, out _);
                await _bot.BanChatMemberAsync(message.Chat.Id, user.Id, now + TimeSpan.FromMinutes(20), revokeMessages: false);
            }
        }
    }

    private void DeleteMessageLater(Message message, TimeSpan after = default)
    {
        if (after == default)
            after = TimeSpan.FromMinutes(5);
        _ = Task.Run(async () =>
        {
            await Task.Delay(after);
            try
            {
                await _bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "DeleteMessageAsync");
            }
        });
    }
}

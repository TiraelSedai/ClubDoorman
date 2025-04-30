using System.Runtime.Caching;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ClubDoorman;

internal class AdminCommandHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly UserManager _userManager;
    private readonly BadMessageManager _badMessageManager;
    private readonly SpamHamClassifier _classifier;
    private readonly ILogger<AdminCommandHandler> _logger;
    private User? _me;

    public AdminCommandHandler(
        ITelegramBotClient bot,
        UserManager userManager,
        BadMessageManager badMessageManager,
        SpamHamClassifier classifier,
        ILogger<AdminCommandHandler> logger
    )
    {
        _bot = bot;
        _userManager = userManager;
        _badMessageManager = badMessageManager;
        _classifier = classifier;
        _logger = logger;
    }

    public async Task HandleAdminCallback(string cbData, CallbackQuery cb)
    {
        var chat = cb.Message?.Chat.Id;
        var admChat = chat ?? Config.AdminChatId;
        var split = cbData.Split('_').ToList();
        if (split.Count > 1 && split[0] == "approve" && long.TryParse(split[1], out var approveUserId))
        {
            await _userManager.Approve(approveUserId);
            await _bot.SendMessage(
                admChat,
                $"{Utils.FullName(cb.From)} добавил пользователя в список доверенных",
                replyParameters: cb.Message?.MessageId
            );
        }
        else if (split.Count > 1 && split[0] == "attOk" && long.TryParse(split[1], out var attOkUserId))
        {
            AiChecks.MarkUserOkay(attOkUserId);
            await _bot.SendMessage(
                admChat,
                $"{Utils.FullName(cb.From)} добавил пользователя в список тех чей профиль не в блеклисте (но ТЕКСТЫ его сообщений всё ещё проверяются)",
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
                await _bot.BanChatMember(chatId, userId);
                await _bot.SendMessage(
                    admChat,
                    $"{Utils.FullName(cb.From)} забанил, сообщение добавлено в список авто-бана",
                    replyParameters: cb.Message?.MessageId
                );
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to ban");
                await _bot.SendMessage(
                    admChat,
                    $"Не могу забанить. Не хватает могущества? Сходите забаньте руками",
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
        var msg = cb.Message;
        if (msg != null)
            await _bot.EditMessageReplyMarkup(msg.Chat.Id, msg.MessageId);
    }

    public async Task AdminChatMessage(Message message)
    {
        // TODO: this is not ideal, share getter with MessageProcessor
        _me ??= await _bot.GetMe();
        if (message is { ReplyToMessage: { } replyToMessage, Text: "/spam" or "/ham" or "/check" })
        {
            if (replyToMessage.From?.Id == _me.Id && replyToMessage.ForwardDate == null)
            {
                await _bot.SendMessage(
                    message.Chat.Id,
                    "Похоже что вы промахнулись и реплайнули на сообщение бота, а не форвард",
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
                            $"Результат:{Environment.NewLine}"
                            + $"Много эмодзи: {emojis}{Environment.NewLine}"
                            + $"Найдены стоп-слова: {hasStopWords}{Environment.NewLine}"
                            + $"Маскирующиеся слова: {lookAlikeMsg}{Environment.NewLine}"
                            + $"ML классификатор: спам {spam}, скор {score}{Environment.NewLine}{Environment.NewLine}"
                            + $"Если простые фильтры отработали, то в датасет добавлять не нужно";
                        await _bot.SendMessage(message.Chat.Id, msg);
                        break;
                    }
                    case "/spam":
                        await _classifier.AddSpam(text);
                        await _badMessageManager.MarkAsBad(text);
                        await _bot.SendMessage(
                            message.Chat.Id,
                            "Сообщение добавлено как пример спама в датасет, а так же в список авто-бана",
                            replyParameters: replyToMessage
                        );
                        break;
                    case "/ham":
                        await _classifier.AddHam(text);
                        await _bot.SendMessage(
                            message.Chat.Id,
                            "Сообщение добавлено как пример НЕ-спама в датасет",
                            replyParameters: replyToMessage
                        );
                        break;
                }
            }
        }
    }
}

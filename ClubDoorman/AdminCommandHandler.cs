using Microsoft.Extensions.Caching.Hybrid;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ClubDoorman;

internal class AdminCommandHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly UserManager _userManager;
    private readonly BadMessageManager _badMessageManager;
    private readonly SpamHamClassifier _classifier;
    private readonly Config _config;
    private readonly AiChecks _aiChecks;
    private readonly RecentMessagesStorage _recentMessagesStorage;
    private readonly HybridCache _hybridCache;
    private readonly ILogger<AdminCommandHandler> _logger;
    private User? _me;

    public AdminCommandHandler(
        ITelegramBotClient bot,
        UserManager userManager,
        BadMessageManager badMessageManager,
        SpamHamClassifier classifier,
        Config config,
        AiChecks aiChecks,
        RecentMessagesStorage recentMessagesStorage,
        HybridCache hybridCache,
        ILogger<AdminCommandHandler> logger
    )
    {
        _bot = bot;
        _userManager = userManager;
        _badMessageManager = badMessageManager;
        _classifier = classifier;
        _config = config;
        _aiChecks = aiChecks;
        _recentMessagesStorage = recentMessagesStorage;
        _hybridCache = hybridCache;
        _logger = logger;
    }

    public async Task HandleAdminCallback(string cbData, CallbackQuery cb)
    {
        var chat = cb.Message?.Chat.Id;
        var admChat = chat ?? _config.AdminChatId;
        var split = cbData.Split('_').ToList();
        if (split.Count > 1)
            switch (split[0])
            {
                case "approve":
                    if (!long.TryParse(split[1], out var approveUserId))
                        return;
                    await _userManager.Approve(approveUserId);
                    var msgText = $"{Utils.FullName(cb.From)} добавил пользователя в список доверенных";

                    if (split.Count > 3 && split[2] == "restore")
                    {
                        var restoreKey = $"{split[2]}_{split[3]}";
                        var deletedInfo = await _hybridCache.GetOrCreateAsync<DeletedMessageInfo?>(
                            restoreKey,
                            _ => ValueTask.FromResult<DeletedMessageInfo?>(null),
                            cancellationToken: default
                        );
                        if (deletedInfo != null)
                        {
                            if (await RestoreMessage(deletedInfo, cb.Message, admChat))
                                msgText += " и восстановил сообщение";
                        }
                    }

                    await _bot.SendMessage(admChat, msgText, replyParameters: cb.Message?.MessageId);
                    break;
                case "attOk":
                    if (!long.TryParse(split[1], out var attOkUserId))
                        return;
                    await _aiChecks.MarkUserOkay(attOkUserId);
                    await _bot.SendMessage(
                        admChat,
                        $"{Utils.FullName(cb.From)} добавил пользователя в список тех чей профиль не в блеклисте (но ТЕКСТЫ его сообщений всё ещё проверяются)",
                        replyParameters: cb.Message?.MessageId
                    );
                    break;
                case "ban":
                    {
                        if (split.Count < 3 || !long.TryParse(split[1], out var chatId) || !long.TryParse(split[2], out var userId))
                            return;
                        try
                        {
                            _logger.LogDebug("Someone pressed button to ban chat {ChatId} user {UserId}", chatId, userId);
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
                        await DeleteAllRecentFrom(chatId, userId);
                    }
                    break;
                case "banchan":
                    {
                        if (split.Count < 3 || !long.TryParse(split[1], out var chatId) || !long.TryParse(split[2], out var fromChannelId))
                            return;
                        try
                        {
                            _logger.LogDebug("Someone pressed button to ban chat {ChatId} chan {ChannelId}", chatId, fromChannelId);
                            await _bot.BanChatSenderChat(chatId, fromChannelId);
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
                        await DeleteAllRecentFrom(chatId, fromChannelId);
                    }
                    break;
                case "restore":
                    {
                        if (split.Count < 2)
                            return;
                        var restoreKey = cbData;
                        var deletedInfo = await _hybridCache.GetOrCreateAsync<DeletedMessageInfo?>(
                            restoreKey,
                            _ => ValueTask.FromResult<DeletedMessageInfo?>(null),
                            cancellationToken: default
                        );
                        if (deletedInfo == null)
                        {
                            await _bot.SendMessage(
                                admChat,
                                "Не могу восстановить: информация о сообщении не найдена (возможно, истёк срок хранения)",
                                replyParameters: cb.Message?.MessageId
                            );
                            return;
                        }

                        await _aiChecks.MarkUserOkay(deletedInfo.UserId);

                        if (await RestoreMessage(deletedInfo, cb.Message, admChat))
                        {
                            await _bot.SendMessage(
                                admChat,
                                $"{Utils.FullName(cb.From)} восстановил сообщение",
                                replyParameters: cb.Message?.MessageId
                            );
                        }
                    }
                    break;
            }

        var msg = cb.Message;
        if (msg != null)
            await _bot.EditMessageReplyMarkup(msg.Chat.Id, msg.MessageId);
    }

    private async Task DeleteAllRecentFrom(long chatId, long fromChannelId)
    {
        var recent = _recentMessagesStorage.Get(fromChannelId, chatId);
        foreach (var message in recent)
        {
            var text = message?.Caption ?? message?.Text;
            if (!string.IsNullOrWhiteSpace(text))
                await _badMessageManager.MarkAsBad(text);
        }
        try
        {
            await _bot.DeleteMessages(chatId, recent.Select(x => x.MessageId));
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Cannot delete messages in ban admin callback");
        }
    }

    public async Task AdminChatMessage(Message message)
    {
        // TODO: this is not ideal, share getter with MessageProcessor
        _me ??= await _bot.GetMe();
        if (message.Text != null && message.Text.StartsWith("/unban"))
        {
            var split = message.Text.Split(' ');
            if (split.Length > 1 && long.TryParse(split[1], out var userId))
            {
                await _userManager.Unban(userId);
                await _bot.SendMessage(message.Chat.Id, $"Unbanned user {userId}", replyParameters: message);
                return;
            }
        }

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

            if (replyToMessage.Quote?.Text != null)
                text = $"{replyToMessage.Quote.Text} {text}";

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
                            + $"Если простые фильтры отработали, то в датасет добавлять не нужно.{Environment.NewLine}"
                            + $"Нормализованный текст: {normalized}";
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

    private async Task<bool> RestoreMessage(DeletedMessageInfo deletedInfo, Message? adminMessageForReply, long admChat)
    {
        try
        {
            var username = string.IsNullOrWhiteSpace(deletedInfo.Username) ? "" : $"@{deletedInfo.Username}";
            var restoredText =
                $"Антиспам ошибочно удалил сообщение от {username}:{Environment.NewLine}{deletedInfo.Text ?? deletedInfo.Caption ?? "[медиа без текста]"}";

            if (!string.IsNullOrWhiteSpace(deletedInfo.PhotoFileId))
            {
                await _bot.SendPhoto(
                    deletedInfo.ChatId,
                    new InputFileId(deletedInfo.PhotoFileId),
                    caption: restoredText,
                    replyParameters: deletedInfo.ReplyToMessageId.HasValue
                        ? new ReplyParameters { MessageId = deletedInfo.ReplyToMessageId.Value }
                        : null
                );
            }
            else if (!string.IsNullOrWhiteSpace(deletedInfo.VideoFileId))
            {
                await _bot.SendVideo(
                    deletedInfo.ChatId,
                    new InputFileId(deletedInfo.VideoFileId),
                    caption: restoredText,
                    replyParameters: deletedInfo.ReplyToMessageId.HasValue
                        ? new ReplyParameters { MessageId = deletedInfo.ReplyToMessageId.Value }
                        : null
                );
            }
            else
            {
                await _bot.SendMessage(
                    deletedInfo.ChatId,
                    restoredText,
                    replyParameters: deletedInfo.ReplyToMessageId.HasValue
                        ? new ReplyParameters { MessageId = deletedInfo.ReplyToMessageId.Value }
                        : null
                );
            }
            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to restore message");
            await _bot.SendMessage(
                admChat,
                $"Не могу восстановить сообщение: {e.Message}",
                replyParameters: adminMessageForReply?.MessageId
            );
            return false;
        }
    }
}

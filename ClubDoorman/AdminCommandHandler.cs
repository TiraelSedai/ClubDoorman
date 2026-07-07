using System.Globalization;
using Microsoft.Extensions.Caching.Hybrid;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ClubDoorman;

internal class AdminCommandHandler
{
    private const int DefaultLatestSpamHamRecordCount = 10;
    private const int DefaultUndoSpamHamPreviewLength = 500;
    private const int TelegramMessageLimit = 4096;
    private static readonly TimeSpan LatestSpamHamRecordDelay = TimeSpan.FromSeconds(5);

    private readonly ITelegramBotClient _bot;
    private readonly UserManager _userManager;
    private readonly BadMessageManager _badMessageManager;
    private readonly SpamHamClassifier _classifier;
    private readonly Config _config;
    private readonly AiChecks _aiChecks;
    private readonly RecentMessagesStorage _recentMessagesStorage;
    private readonly HybridCache _hybridCache;
    private readonly SpamDeduplicationCache _spamDeduplicationCache;
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
        SpamDeduplicationCache spamDeduplicationCache,
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
        _spamDeduplicationCache = spamDeduplicationCache;
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
                    _logger.LogDebug(
                        "Approve callback: userId={UserId}, splitCount={SplitCount}, parts=[{Parts}]",
                        approveUserId,
                        split.Count,
                        string.Join(", ", split)
                    );
                    await _userManager.Approve(approveUserId);
                    var msgText = $"{Utils.FullName(cb.From)} добавил пользователя в список доверенных";

                    if (split.Count > 3 && split[2] == "restore")
                    {
                        var restoreKey = $"{split[2]}_{split[3]}";
                        _logger.LogDebug("Attempting to restore message with key: {RestoreKey}", restoreKey);
                        var deletedInfo = await _hybridCache.GetOrCreateAsync<DeletedMessageInfo?>(
                            restoreKey,
                            _ => ValueTask.FromResult<DeletedMessageInfo?>(null),
                            cancellationToken: default
                        );
                        if (deletedInfo != null)
                        {
                            _logger.LogDebug(
                                "Found deletedInfo for key {RestoreKey}, chatId={ChatId}, text length={TextLength}",
                                restoreKey,
                                deletedInfo.ChatId,
                                (deletedInfo.Text ?? deletedInfo.Caption)?.Length ?? 0
                            );
                            if (deletedInfo.Reason == DeletionReason.UserProfile)
                                await _aiChecks.MarkUserOkay(approveUserId);
                            if (await RestoreMessage(deletedInfo, cb.Message, admChat))
                                msgText += " и восстановил сообщение";
                        }
                        else
                        {
                            _logger.LogWarning("deletedInfo not found in cache for key {RestoreKey}", restoreKey);
                        }
                    }
                    else if (split.Count > 2)
                    {
                        _logger.LogDebug(
                            "Not restoring: splitCount={SplitCount}, split[2]='{Split2}'",
                            split.Count,
                            split.Count > 2 ? split[2] : "N/A"
                        );
                    }

                    await _bot.SendMessage(admChat, msgText, replyParameters: cb.Message?.MessageId);
                    break;
                case "attOk":
                    if (!long.TryParse(split[1], out var attOkUserId))
                        return;
                    await _aiChecks.MarkUserOkay(attOkUserId);

                    var attOkMsgText =
                        $"{Utils.FullName(cb.From)} добавил пользователя в список тех чей профиль не в блеклисте (но ТЕКСТЫ его сообщений всё ещё проверяются)";
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
                                attOkMsgText += " и восстановил сообщение";
                        }
                    }

                    await _bot.SendMessage(admChat, attOkMsgText, replyParameters: cb.Message?.MessageId);
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
                case "banNoMark":
                    {
                        if (
                            split.Count < 3
                            || !long.TryParse(split[1], out var chatIdNoMark)
                            || !long.TryParse(split[2], out var userIdNoMark)
                        )
                            return;
                        try
                        {
                            _logger.LogDebug(
                                "Someone pressed button to ban (no mark) chat {ChatId} user {UserId}",
                                chatIdNoMark,
                                userIdNoMark
                            );
                            await _bot.BanChatMember(chatIdNoMark, userIdNoMark);
                            await _bot.SendMessage(admChat, $"{Utils.FullName(cb.From)} забанил", replyParameters: cb.Message?.MessageId);
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
                        await DeleteAllRecentFrom(chatIdNoMark, userIdNoMark);
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
            {
                await _badMessageManager.MarkAsBad(text);
                _spamDeduplicationCache.Remove(text);
            }
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

    public async Task AdminChatMessage(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text != null && message.Text.StartsWith("/unban", StringComparison.Ordinal))
        {
            var split = message.Text.Split(' ');
            if (split.Length > 1 && long.TryParse(split[1], out var userId))
            {
                await _userManager.Unban(userId, cancellationToken);
                await _bot.SendMessage(
                    message.Chat.Id,
                    $"Unbanned user {userId}",
                    replyParameters: message,
                    cancellationToken: cancellationToken
                );
                return;
            }
        }

        if (message.Text != null)
        {
            var command = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            switch (command)
            {
                case "/latest":
                    await SendLatestSpamHamRecords(message, cancellationToken);
                    return;
                case "/undo":
                    await UndoSpamHamRecord(message, cancellationToken);
                    return;
            }
        }

        // TODO: this is not ideal, share getter with MessageProcessor
        _me ??= await _bot.GetMe(cancellationToken);
        if (message is { ReplyToMessage: { } replyToMessage, Text: "/spam" or "/ham" or "/check" })
        {
            if (replyToMessage.From?.Id == _me.Id && replyToMessage.ForwardDate == null)
            {
                await _bot.SendMessage(
                    message.Chat.Id,
                    "Похоже что вы промахнулись и реплайнули на сообщение бота, а не форвард",
                    replyParameters: replyToMessage,
                    cancellationToken: cancellationToken
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
                        await _bot.SendMessage(message.Chat.Id, msg, cancellationToken: cancellationToken);
                        break;
                    }
                    case "/spam":
                        await _classifier.AddSpam(text);
                        await _badMessageManager.MarkAsBad(text);
                        await _bot.SendMessage(
                            message.Chat.Id,
                            "Сообщение добавлено как пример спама в датасет, а так же в список авто-бана",
                            replyParameters: replyToMessage,
                            cancellationToken: cancellationToken
                        );
                        break;
                    case "/ham":
                        await _classifier.AddHam(text);
                        await _bot.SendMessage(
                            message.Chat.Id,
                            "Сообщение добавлено как пример НЕ-спама в датасет",
                            replyParameters: replyToMessage,
                            cancellationToken: cancellationToken
                        );
                        break;
                }
            }
        }
    }

    private async Task SendLatestSpamHamRecords(Message message, CancellationToken cancellationToken)
    {
        var (ok, count, errorMessage) = ParseLatestSpamHamCount(message.Text!);
        if (!ok)
        {
            await _bot.SendMessage(message.Chat.Id, errorMessage!, replyParameters: message, cancellationToken: cancellationToken);
            return;
        }

        var records = await _classifier.GetLatestSpamHamRecords(count);
        if (records.Count == 0)
        {
            await _bot.SendMessage(
                message.Chat.Id,
                "В spam-ham таблице нет записей",
                replyParameters: message,
                cancellationToken: cancellationToken
            );
            return;
        }

        foreach (var record in records)
        {
            await _bot.SendMessage(message.Chat.Id, BuildSpamHamRecordMessage(record), cancellationToken: cancellationToken);
            await Task.Delay(LatestSpamHamRecordDelay, cancellationToken);
        }
    }

    private async Task UndoSpamHamRecord(Message message, CancellationToken cancellationToken)
    {
        var (ok, id, errorMessage) = ParseUndoSpamHamRecordId(message.Text!);
        if (!ok)
        {
            await _bot.SendMessage(message.Chat.Id, errorMessage!, replyParameters: message, cancellationToken: cancellationToken);
            return;
        }

        var deleted = await _classifier.DeleteSpamHamRecord(id);
        if (deleted == null)
        {
            await _bot.SendMessage(
                message.Chat.Id,
                $"Запись #{id} не найдена",
                replyParameters: message,
                cancellationToken: cancellationToken
            );
            return;
        }

        await _bot.SendMessage(
            message.Chat.Id,
            BuildDeletedSpamHamRecordMessage(deleted),
            replyParameters: message,
            cancellationToken: cancellationToken
        );
    }

    internal static string BuildSpamHamRecordMessage(SpamHamRecord record, int maxLength = TelegramMessageLimit)
    {
        const string truncatedSuffix = "\n[truncated]";
        var label = record.IsSpam ? "spam" : "ham";
        var header = $"#{record.Id} {label}";
        var message = $"{header}{Environment.NewLine}{record.Text}";
        if (message.Length <= maxLength)
            return message;

        var availableTextLength = maxLength - header.Length - Environment.NewLine.Length - truncatedSuffix.Length;
        return $"{header}{Environment.NewLine}{record.Text[..availableTextLength]}{truncatedSuffix}";
    }

    internal static string BuildDeletedSpamHamRecordMessage(SpamHamRecord record, int maxPreviewLength = DefaultUndoSpamHamPreviewLength)
    {
        var label = record.IsSpam ? "spam" : "ham";
        var preview =
            record.Text.Length <= maxPreviewLength ? record.Text : $"{record.Text[..maxPreviewLength]}{Environment.NewLine}[truncated]";
        return $"Запись #{record.Id} {label} удалена, переобучение запланировано{Environment.NewLine}{preview}";
    }

    internal static (bool Ok, int Count, string? ErrorMessage) ParseLatestSpamHamCount(string text)
    {
        var split = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (split.Length == 1)
            return (true, DefaultLatestSpamHamRecordCount, null);

        if (split.Length == 2 && int.TryParse(split[1], NumberStyles.None, CultureInfo.InvariantCulture, out var count) && count > 0)
            return (true, count, null);

        return (false, 0, "Использование: /latest [count], где count - положительное целое число");
    }

    internal static (bool Ok, int Id, string? ErrorMessage) ParseUndoSpamHamRecordId(string text)
    {
        var split = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (split.Length == 2 && int.TryParse(split[1], NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0)
            return (true, id, null);

        return (false, 0, "Использование: /undo <id>, где id - положительное целое число");
    }

    private async Task<bool> RestoreMessage(DeletedMessageInfo deletedInfo, Message? adminMessageForReply, long admChat)
    {
        _logger.LogDebug(
            "RestoreMessage called: chatId={ChatId}, userId={UserId}, hasPhoto={HasPhoto}, hasVideo={HasVideo}",
            deletedInfo.ChatId,
            deletedInfo.UserId,
            !string.IsNullOrWhiteSpace(deletedInfo.PhotoFileId),
            !string.IsNullOrWhiteSpace(deletedInfo.VideoFileId)
        );
        try
        {
            var username = string.IsNullOrWhiteSpace(deletedInfo.Username) ? "" : $"@{deletedInfo.Username}";
            var parts = new[] { deletedInfo.UserFirstName, deletedInfo.UserLastName, username };
            var full = string.Join(" ", parts.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
            var restoredText =
                $"Антиспам ошибочно удалил сообщение от {full}:{Environment.NewLine}{deletedInfo.Text ?? deletedInfo.Caption ?? "[медиа без текста]"}";

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

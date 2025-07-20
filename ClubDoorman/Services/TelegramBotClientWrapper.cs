using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Services;

/// <summary>
/// Реализация интерфейса-обертки для TelegramBotClient
/// </summary>
public class TelegramBotClientWrapper : ITelegramBotClientWrapper
{
    private readonly TelegramBotClient _bot;

    public TelegramBotClientWrapper(TelegramBotClient bot)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
    }

    /// <summary>
    /// Отправляет сообщение в чат
    /// </summary>
    public async Task<Message> SendMessageAsync(
        ChatId chatId,
        string text,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        return await _bot.SendMessage(
            chatId,
            text,
            parseMode: parseMode ?? ParseMode.Html,
            replyParameters: replyParameters,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Банит пользователя в чате
    /// </summary>
    public async Task<bool> BanChatMemberAsync(
        ChatId chatId,
        long userId,
        DateTime? untilDate = null,
        bool? revokeMessages = null,
        CancellationToken cancellationToken = default)
    {
        await _bot.BanChatMember(
            chatId,
            userId,
            untilDate: untilDate,
            revokeMessages: revokeMessages ?? false,
            cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>
    /// Удаляет сообщение из чата
    /// </summary>
    public async Task<bool> DeleteMessageAsync(
        ChatId chatId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        await _bot.DeleteMessage(
            chatId,
            messageId,
            cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>
    /// Разбанивает пользователя в чате
    /// </summary>
    public async Task<bool> UnbanChatMemberAsync(
        ChatId chatId,
        long userId,
        bool? onlyIfBanned = null,
        CancellationToken cancellationToken = default)
    {
        await _bot.UnbanChatMember(
            chatId,
            userId,
            onlyIfBanned: onlyIfBanned ?? false,
            cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>
    /// Получает информацию о боте
    /// </summary>
    public async Task<User> GetMe(CancellationToken cancellationToken = default)
    {
        return await _bot.GetMe(cancellationToken);
    }

    /// <summary>
    /// ID бота
    /// </summary>
    public long BotId => _bot.BotId;

    /// <summary>
    /// Удаляет сообщение
    /// </summary>
    public async Task DeleteMessage(ChatId chatId, int messageId, CancellationToken cancellationToken = default)
    {
        await _bot.DeleteMessage(chatId, messageId, cancellationToken);
    }

    /// <summary>
    /// Отправляет сообщение
    /// </summary>
    public async Task<Message> SendMessage(
        ChatId chatId,
        string text,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        return await _bot.SendMessage(
            chatId,
            text,
            parseMode: parseMode ?? ParseMode.Html,
            replyParameters: replyParameters,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Получает информацию о чате
    /// </summary>
    public async Task<Chat> GetChat(ChatId chatId, CancellationToken cancellationToken = default)
    {
        return await _bot.GetChat(chatId, cancellationToken);
    }

    /// <summary>
    /// Получает полную информацию о чате
    /// </summary>
    public async Task<ChatFullInfo> GetChatFullInfo(ChatId chatId, CancellationToken cancellationToken = default)
    {
        // В новой версии API используем GetChat для получения базовой информации
        // LinkedChatId доступен только через специальные методы, но для наших целей достаточно
        var chat = await _bot.GetChat(chatId, cancellationToken);
        return new ChatFullInfo
        {
            Id = chat.Id,
            Type = chat.Type,
            Title = chat.Title,
            Username = chat.Username,
            FirstName = chat.FirstName,
            LastName = chat.LastName,
            Bio = chat.Bio,
            Description = chat.Description,
            InviteLink = chat.InviteLink,
            // LinkedChatId не доступен через базовый GetChat, но это не критично для нашей логики
            LinkedChatId = null
        };
    }

    /// <summary>
    /// Пересылает сообщение
    /// </summary>
    public async Task<Message> ForwardMessage(
        ChatId chatId,
        ChatId fromChatId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        return await _bot.ForwardMessage(chatId, fromChatId, messageId, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Банит участника чата
    /// </summary>
    public async Task BanChatMember(
        ChatId chatId,
        long userId,
        DateTime? untilDate = null,
        bool revokeMessages = false,
        CancellationToken cancellationToken = default)
    {
        await _bot.BanChatMember(chatId, userId, untilDate, revokeMessages, cancellationToken);
    }

    /// <summary>
    /// Банит отправителя чата
    /// </summary>
    public async Task BanChatSenderChat(ChatId chatId, long senderChatId, CancellationToken cancellationToken = default)
    {
        await _bot.BanChatSenderChat(chatId, senderChatId, cancellationToken);
    }

    /// <summary>
    /// Ограничивает участника чата
    /// </summary>
    public async Task RestrictChatMember(
        ChatId chatId,
        long userId,
        ChatPermissions permissions,
        DateTime? untilDate = null,
        CancellationToken cancellationToken = default)
    {
        await _bot.RestrictChatMember(chatId, userId, permissions, untilDate: untilDate, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Отправляет фото
    /// </summary>
    public async Task<Message> SendPhoto(
        ChatId chatId,
        object photo,
        string? caption = null,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        return await TelegramBotClientExtensions.SendPhoto(
            _bot,
            chatId,
            (dynamic)photo,
            caption: caption,
            parseMode: parseMode ?? ParseMode.Html,
            replyParameters: replyParameters,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Получает количество участников чата
    /// </summary>
    public async Task<int> GetChatMemberCount(ChatId chatId, CancellationToken cancellationToken = default)
    {
        return await _bot.GetChatMemberCount(chatId, cancellationToken);
    }

    /// <summary>
    /// Получает обновления
    /// </summary>
    public async Task<Update[]> GetUpdates(
        int? offset = null,
        int? limit = null,
        int? timeout = null,
        IEnumerable<UpdateType>? allowedUpdates = null,
        CancellationToken cancellationToken = default)
    {
        return await _bot.GetUpdates(offset, limit, timeout, allowedUpdates, cancellationToken);
    }

    /// <summary>
    /// Получает информацию о файле и скачивает его
    /// </summary>
    public async Task GetInfoAndDownloadFile(string fileId, Stream destination, CancellationToken cancellationToken = default)
    {
        await _bot.GetInfoAndDownloadFile(fileId, destination, cancellationToken);
    }
} 
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс-обертка для TelegramBotClient для возможности мокирования в тестах
/// </summary>
public interface ITelegramBotClientWrapper
{
    /// <summary>
    /// Отправляет сообщение в чат
    /// </summary>
    Task<Message> SendMessageAsync(
        ChatId chatId,
        string text,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Банит пользователя в чате
    /// </summary>
    Task<bool> BanChatMemberAsync(
        ChatId chatId,
        long userId,
        DateTime? untilDate = null,
        bool? revokeMessages = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет сообщение из чата
    /// </summary>
    Task<bool> DeleteMessageAsync(
        ChatId chatId,
        int messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Разбанивает пользователя в чате
    /// </summary>
    Task<bool> UnbanChatMemberAsync(
        ChatId chatId,
        long userId,
        bool? onlyIfBanned = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает информацию о боте
    /// </summary>
    Task<User> GetMe(CancellationToken cancellationToken = default);

    /// <summary>
    /// ID бота
    /// </summary>
    long BotId { get; }

    /// <summary>
    /// Удаляет сообщение
    /// </summary>
    Task DeleteMessage(ChatId chatId, int messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отправляет сообщение
    /// </summary>
    Task<Message> SendMessage(
        ChatId chatId,
        string text,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает информацию о чате
    /// </summary>
    Task<Chat> GetChat(ChatId chatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает полную информацию о чате
    /// </summary>
    Task<ChatFullInfo> GetChatFullInfo(ChatId chatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Пересылает сообщение
    /// </summary>
    Task<Message> ForwardMessage(
        ChatId chatId,
        ChatId fromChatId,
        int messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Банит участника чата
    /// </summary>
    Task BanChatMember(
        ChatId chatId,
        long userId,
        DateTime? untilDate = null,
        bool revokeMessages = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Банит отправителя чата
    /// </summary>
    Task BanChatSenderChat(ChatId chatId, long senderChatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ограничивает участника чата
    /// </summary>
    Task RestrictChatMember(
        ChatId chatId,
        long userId,
        ChatPermissions permissions,
        DateTime? untilDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отправляет фото
    /// </summary>
    Task<Message> SendPhoto(
        ChatId chatId,
        object photo,
        string? caption = null,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает количество участников чата
    /// </summary>
    Task<int> GetChatMemberCount(ChatId chatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает обновления
    /// </summary>
    Task<Update[]> GetUpdates(
        int? offset = null,
        int? limit = null,
        int? timeout = null,
        IEnumerable<UpdateType>? allowedUpdates = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает информацию о файле и скачивает его
    /// </summary>
    Task GetInfoAndDownloadFile(string fileId, Stream destination, CancellationToken cancellationToken = default);
} 
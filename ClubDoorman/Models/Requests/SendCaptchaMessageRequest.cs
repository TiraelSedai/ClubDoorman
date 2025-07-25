using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.Models.Requests;

/// <summary>
/// Запрос на отправку сообщения капчи
/// </summary>
public class SendCaptchaMessageRequest
{
    /// <summary>
    /// Чат, в который отправляется капча
    /// </summary>
    public Chat Chat { get; }
    
    /// <summary>
    /// Текст сообщения капчи
    /// </summary>
    public string Message { get; }
    
    /// <summary>
    /// Параметры ответа (опционально)
    /// </summary>
    public ReplyParameters? ReplyParameters { get; }
    
    /// <summary>
    /// Клавиатура с кнопками капчи
    /// </summary>
    public InlineKeyboardMarkup ReplyMarkup { get; }
    
    /// <summary>
    /// Токен отмены операции
    /// </summary>
    public CancellationToken CancellationToken { get; }
    
    /// <summary>
    /// Создает новый запрос на отправку сообщения капчи
    /// </summary>
    /// <param name="chat">Чат</param>
    /// <param name="message">Текст сообщения</param>
    /// <param name="replyParameters">Параметры ответа (опционально)</param>
    /// <param name="replyMarkup">Клавиатура</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    public SendCaptchaMessageRequest(
        Chat chat,
        string message,
        ReplyParameters? replyParameters,
        InlineKeyboardMarkup replyMarkup,
        CancellationToken cancellationToken = default)
    {
        Chat = chat ?? throw new ArgumentNullException(nameof(chat));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        ReplyParameters = replyParameters;
        ReplyMarkup = replyMarkup ?? throw new ArgumentNullException(nameof(replyMarkup));
        CancellationToken = cancellationToken;
    }
} 
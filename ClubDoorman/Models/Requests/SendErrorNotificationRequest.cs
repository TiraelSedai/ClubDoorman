using Telegram.Bot.Types;

namespace ClubDoorman.Models.Requests;

/// <summary>
/// Запрос на отправку уведомления об ошибке
/// </summary>
public class SendErrorNotificationRequest
{
    /// <summary>
    /// Исключение, которое произошло
    /// </summary>
    public Exception Exception { get; }
    
    /// <summary>
    /// Контекст, в котором произошла ошибка
    /// </summary>
    public string Context { get; }
    
    /// <summary>
    /// Пользователь, связанный с ошибкой (опционально)
    /// </summary>
    public User? User { get; }
    
    /// <summary>
    /// Чат, связанный с ошибкой (опционально)
    /// </summary>
    public Chat? Chat { get; }
    
    /// <summary>
    /// Токен отмены операции
    /// </summary>
    public CancellationToken CancellationToken { get; }
    
    /// <summary>
    /// Создает новый запрос на отправку уведомления об ошибке
    /// </summary>
    /// <param name="exception">Исключение</param>
    /// <param name="context">Контекст ошибки</param>
    /// <param name="user">Пользователь (опционально)</param>
    /// <param name="chat">Чат (опционально)</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    public SendErrorNotificationRequest(
        Exception exception,
        string context,
        User? user = null,
        Chat? chat = null,
        CancellationToken cancellationToken = default)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        User = user;
        Chat = chat;
        CancellationToken = cancellationToken;
    }
} 
using Telegram.Bot.Types;

namespace ClubDoorman.Models.Requests;

/// <summary>
/// Запрос на отправку приветственного сообщения
/// </summary>
public class SendWelcomeMessageRequest
{
    /// <summary>
    /// Пользователь, которому отправляется приветствие
    /// </summary>
    public User User { get; }
    
    /// <summary>
    /// Чат, в который отправляется приветствие
    /// </summary>
    public Chat Chat { get; }
    
    /// <summary>
    /// Причина приветствия (по умолчанию "приветствие")
    /// </summary>
    public string Reason { get; }
    
    /// <summary>
    /// Токен отмены операции
    /// </summary>
    public CancellationToken CancellationToken { get; }
    
    /// <summary>
    /// Создает новый запрос на отправку приветственного сообщения
    /// </summary>
    /// <param name="user">Пользователь</param>
    /// <param name="chat">Чат</param>
    /// <param name="reason">Причина приветствия (по умолчанию "приветствие")</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    public SendWelcomeMessageRequest(
        User user, 
        Chat chat, 
        string reason = "приветствие", 
        CancellationToken cancellationToken = default)
    {
        User = user ?? throw new ArgumentNullException(nameof(user));
        Chat = chat ?? throw new ArgumentNullException(nameof(chat));
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        CancellationToken = cancellationToken;
    }
} 
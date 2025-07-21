using ClubDoorman.Models.Notifications;
using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для отправки уведомлений в Telegram
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Отправить уведомление в админский чат
    /// </summary>
    Task SendAdminNotificationAsync(AdminNotificationType type, NotificationData data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отправить уведомление в лог-чат
    /// </summary>
    Task SendLogNotificationAsync(LogNotificationType type, NotificationData data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отправить уведомление пользователю
    /// </summary>
    Task SendUserNotificationAsync(User user, Chat chat, UserNotificationType type, object data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Переслать сообщение в админский чат с уведомлением
    /// </summary>
    Task<Message?> ForwardToAdminWithNotificationAsync(Message originalMessage, AdminNotificationType type, NotificationData data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Переслать сообщение в лог-чат с уведомлением
    /// </summary>
    Task<Message?> ForwardToLogWithNotificationAsync(Message originalMessage, LogNotificationType type, NotificationData data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отправить уведомление об ошибке
    /// </summary>
    Task SendErrorNotificationAsync(Exception ex, string context, User? user = null, Chat? chat = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отправить уведомление о AI анализе профиля с фото
    /// </summary>
    Task SendAiProfileAnalysisAsync(AiProfileAnalysisData data, CancellationToken cancellationToken = default);
} 
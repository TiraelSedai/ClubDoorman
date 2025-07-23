using ClubDoorman.Models.Notifications;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

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
    /// Отправляет пользовательское уведомление и возвращает отправленное сообщение
    /// </summary>
    Task<Message> SendUserNotificationWithReplyAsync(User user, Chat chat, UserNotificationType type, object data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отправляет приветственное сообщение и автоматически удаляет его через 20 секунд
    /// </summary>
    Task<Message> SendWelcomeMessageAsync(User user, Chat chat, string reason = "приветствие", CancellationToken cancellationToken = default);

    /// <summary>
    /// Отправляет сообщение капчи с кнопками
    /// </summary>
    Task<Message> SendCaptchaMessageAsync(Chat chat, string message, ReplyParameters? replyParameters, InlineKeyboardMarkup replyMarkup, CancellationToken cancellationToken = default);
    
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
    
    /// <summary>
    /// Получить доступ к шаблонам сообщений
    /// </summary>
    MessageTemplates GetTemplates();
} 
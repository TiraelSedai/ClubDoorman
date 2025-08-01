using Telegram.Bot.Types;
using ClubDoorman.Models.Notifications;

namespace ClubDoorman.Services.Notifications;

/// <summary>
/// Сервис для отправки уведомлений и управления сообщениями
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Удаляет сообщение и отправляет уведомление в админ-чат
    /// </summary>
    Task DeleteAndReportMessage(Message message, string reason, bool isSilentMode, CancellationToken cancellationToken);

    /// <summary>
    /// Удаляет сообщение и отправляет уведомление в лог-чат
    /// </summary>
    Task DeleteAndReportToLogChat(Message message, string reason, CancellationToken cancellationToken);

    /// <summary>
    /// Не удаляет сообщение, но отправляет уведомление о подозрительном сообщении
    /// </summary>
    Task DontDeleteButReportMessage(Message message, User user, bool isSilentMode, CancellationToken cancellationToken);

    /// <summary>
    /// Отправляет подозрительное сообщение с кнопками для действий
    /// </summary>
    Task SendSuspiciousMessageWithButtons(Message message, User user, SuspiciousMessageNotificationData data, bool isSilentMode, CancellationToken cancellationToken);
} 
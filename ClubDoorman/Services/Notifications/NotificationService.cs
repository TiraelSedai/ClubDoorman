using Telegram.Bot.Types;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Handlers;

namespace ClubDoorman.Services.Notifications;

/// <summary>
/// Сервис для отправки уведомлений и управления сообщениями
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IMessageHandler _messageHandler;

    /// <summary>
    /// Создает экземпляр сервиса уведомлений
    /// </summary>
    /// <param name="messageHandler">MessageHandler для проксирования вызовов</param>
    public NotificationService(IMessageHandler messageHandler)
    {
        _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
    }

    /// <summary>
    /// Удаляет сообщение и отправляет уведомление в админ-чат
    /// </summary>
    public async Task DeleteAndReportMessage(Message message, string reason, bool isSilentMode, CancellationToken cancellationToken)
    {
        // Проксируем вызов к существующему методу в MessageHandler
        await _messageHandler.DeleteAndReportMessage(message, reason, isSilentMode, cancellationToken);
    }

    /// <summary>
    /// Удаляет сообщение и отправляет уведомление в лог-чат
    /// </summary>
    public async Task DeleteAndReportToLogChat(Message message, string reason, CancellationToken cancellationToken)
    {
        await _messageHandler.DeleteAndReportToLogChat(message, reason, cancellationToken);
    }

    /// <summary>
    /// Не удаляет сообщение, но отправляет уведомление о подозрительном сообщении
    /// </summary>
    public async Task DontDeleteButReportMessage(Message message, User user, bool isSilentMode, CancellationToken cancellationToken)
    {
        await _messageHandler.DontDeleteButReportMessage(message, user, isSilentMode, cancellationToken);
    }

    /// <summary>
    /// Отправляет подозрительное сообщение с кнопками для действий
    /// </summary>
    public async Task SendSuspiciousMessageWithButtons(Message message, User user, SuspiciousMessageNotificationData data, bool isSilentMode, CancellationToken cancellationToken)
    {
        await _messageHandler.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, cancellationToken);
    }
} 
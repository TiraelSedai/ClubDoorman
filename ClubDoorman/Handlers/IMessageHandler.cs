using Telegram.Bot.Types;
using ClubDoorman.Models.Notifications;

namespace ClubDoorman.Handlers;

/// <summary>
/// Интерфейс для MessageHandler
/// <tags>message-handler, interface, contract</tags>
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// Удаляет сообщение и отправляет уведомление
    /// <tags>delete-message, notification</tags>
    /// </summary>
    Task DeleteAndReportMessage(Message message, string reason, bool isSilentMode, CancellationToken cancellationToken);

    /// <summary>
    /// Удаляет сообщение и отправляет в лог-чат
    /// <tags>delete-message, log-chat</tags>
    /// </summary>
    Task DeleteAndReportToLogChat(Message message, string reason, CancellationToken cancellationToken);

    /// <summary>
    /// Отправляет уведомление без удаления сообщения
    /// <tags>notification, no-delete</tags>
    /// </summary>
    Task DontDeleteButReportMessage(Message message, User user, bool isSilentMode, CancellationToken cancellationToken);

    /// <summary>
    /// Отправляет подозрительное сообщение с кнопками
    /// <tags>suspicious-message, buttons</tags>
    /// </summary>
    Task SendSuspiciousMessageWithButtons(Message message, User user, SuspiciousMessageNotificationData data, bool isSilentMode, CancellationToken cancellationToken);

    /// <summary>
    /// Обрабатывает новые участники
    /// <tags>new-members, user-join</tags>
    /// </summary>
    Task HandleNewMembersAsync(Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Обрабатывает одного нового пользователя
    /// <tags>new-user, processing</tags>
    /// </summary>
    Task ProcessNewUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken);

    /// <summary>
    /// Определяет, может ли данный обработчик обработать указанное сообщение
    /// <tags>can-handle, message-validation</tags>
    /// </summary>
    bool CanHandle(Message message);

    /// <summary>
    /// Обрабатывает сообщение
    /// <tags>handle-message, processing</tags>
    /// </summary>
    Task HandleAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обрабатывает команду
    /// <tags>command, processing</tags>
    /// </summary>
    Task HandleCommandAsync(Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Обрабатывает сообщение от канала
    /// <tags>channel, moderation</tags>
    /// </summary>
    Task HandleChannelMessageAsync(Message message, CancellationToken cancellationToken);
} 
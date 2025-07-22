using ClubDoorman.Models.Notifications;
using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Диспетчер сервис-чатов для разделения сообщений по админ-чату и лог-чату
/// </summary>
public interface IServiceChatDispatcher
{
    /// <summary>
    /// Отправляет уведомление в админ-чат (требует реакции через кнопки)
    /// </summary>
    /// <param name="notification">Данные уведомления</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    Task SendToAdminChatAsync(NotificationData notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отправляет уведомление в лог-чат (для анализа и корректировки фильтров)
    /// </summary>
    /// <param name="notification">Данные уведомления</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    Task SendToLogChatAsync(NotificationData notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Определяет, куда отправить уведомление на основе его типа и содержимого
    /// </summary>
    /// <param name="notification">Данные уведомления</param>
    /// <returns>true если уведомление требует реакции (админ-чат), false если только для логов</returns>
    bool ShouldSendToAdminChat(NotificationData notification);
} 
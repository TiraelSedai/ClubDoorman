using ClubDoorman.Models.Logging;
using ClubDoorman.Models.Notifications;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для работы с конфигурацией логирования
/// </summary>
public interface ILoggingConfigurationService
{
    /// <summary>
    /// Получить конфигурацию логирования
    /// </summary>
    LoggingConfiguration GetConfiguration();
    
    /// <summary>
    /// Проверить, нужно ли отправлять уведомление в указанное направление
    /// </summary>
    bool ShouldSendNotification(string notificationType, NotificationDestination destination);
    
    /// <summary>
    /// Получить направления для админского уведомления
    /// </summary>
    NotificationDestination GetAdminNotificationDestinations(AdminNotificationType type);
    
    /// <summary>
    /// Получить направления для лог-уведомления
    /// </summary>
    NotificationDestination GetLogNotificationDestinations(LogNotificationType type);
    
    /// <summary>
    /// Получить направления для пользовательского уведомления
    /// </summary>
    NotificationDestination GetUserNotificationDestinations(UserNotificationType type);
    
    /// <summary>
    /// Проверить, включено ли файловое логирование
    /// </summary>
    bool IsFileLoggingEnabled();
    
    /// <summary>
    /// Проверить, включены ли Telegram уведомления
    /// </summary>
    bool IsTelegramNotificationsEnabled();
} 
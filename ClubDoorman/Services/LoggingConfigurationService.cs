using ClubDoorman.Models.Logging;
using ClubDoorman.Models.Notifications;
using Microsoft.Extensions.Options;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для работы с конфигурацией логирования
/// </summary>
public class LoggingConfigurationService : ILoggingConfigurationService
{
    private readonly LoggingConfiguration _configuration;
    private readonly ILogger<LoggingConfigurationService> _logger;

    public LoggingConfigurationService(IOptions<LoggingConfiguration> configuration, ILogger<LoggingConfigurationService> logger)
    {
        _configuration = configuration.Value;
        _logger = logger;
    }

    public LoggingConfiguration GetConfiguration() => _configuration;

    public bool ShouldSendNotification(string notificationType, NotificationDestination destination)
    {
        if (!_configuration.TelegramNotifications.Enabled)
            return false;

        // Проверяем общие настройки
        if (destination.HasFlag(NotificationDestination.AdminChat) && !_configuration.TelegramNotifications.AdminNotifications)
            return false;
        
        if (destination.HasFlag(NotificationDestination.LogChat) && !_configuration.TelegramNotifications.LogNotifications)
            return false;
        
        if (destination.HasFlag(NotificationDestination.UserChat) && !_configuration.TelegramNotifications.UserNotifications)
            return false;

        return true;
    }

    public NotificationDestination GetAdminNotificationDestinations(AdminNotificationType type)
    {
        var typeName = type.ToString();
        
        if (_configuration.TelegramNotifications.NotificationTypes.AdminNotifications.TryGetValue(typeName, out var destinations))
        {
            return destinations;
        }

        // Значения по умолчанию
        return type switch
        {
            AdminNotificationType.AutoBan or AdminNotificationType.AutoBanBlacklist or AdminNotificationType.AutoBanFromBlacklist 
                => NotificationDestination.FileLog,
            
            AdminNotificationType.SystemError 
                => NotificationDestination.AdminChat | NotificationDestination.LogChat | NotificationDestination.FileLog,
            
            AdminNotificationType.ChannelMessage or AdminNotificationType.BanChannel or AdminNotificationType.BanForLongName
                => NotificationDestination.AdminChat | NotificationDestination.LogChat | NotificationDestination.FileLog,
            
            AdminNotificationType.SilentMode
                => NotificationDestination.AdminChat | NotificationDestination.LogChat | NotificationDestination.FileLog,
            
            _ => NotificationDestination.AdminChat | NotificationDestination.FileLog
        };
    }

    public NotificationDestination GetLogNotificationDestinations(LogNotificationType type)
    {
        var typeName = type.ToString();
        
        if (_configuration.TelegramNotifications.NotificationTypes.LogNotifications.TryGetValue(typeName, out var destinations))
        {
            return destinations;
        }

        // Значения по умолчанию
        return type switch
        {
            LogNotificationType.AutoBanBlacklist or LogNotificationType.AutoBanFromBlacklist or LogNotificationType.AutoBanKnownSpam or LogNotificationType.BanForLongName or LogNotificationType.BanChannel
                => NotificationDestination.LogChat | NotificationDestination.FileLog,
            
            LogNotificationType.CriticalError
                => NotificationDestination.LogChat | NotificationDestination.FileLog,
            
            _ => NotificationDestination.LogChat | NotificationDestination.FileLog
        };
    }

    public NotificationDestination GetUserNotificationDestinations(UserNotificationType type)
    {
        var typeName = type.ToString();
        
        if (_configuration.TelegramNotifications.NotificationTypes.UserNotifications.TryGetValue(typeName, out var destinations))
        {
            return destinations;
        }

        // Значения по умолчанию
        return NotificationDestination.UserChat | NotificationDestination.UserFlowLog;
    }

    public bool IsFileLoggingEnabled() => _configuration.FileLogging.Enabled;

    public bool IsTelegramNotificationsEnabled() => _configuration.TelegramNotifications.Enabled;
} 
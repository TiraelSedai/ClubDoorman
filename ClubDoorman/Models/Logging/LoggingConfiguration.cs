namespace ClubDoorman.Models.Logging;

/// <summary>
/// Конфигурация логирования
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Настройки файлового логирования
    /// </summary>
    public FileLoggingSettings FileLogging { get; set; } = new();
    
    /// <summary>
    /// Настройки Telegram уведомлений
    /// </summary>
    public TelegramNotificationSettings TelegramNotifications { get; set; } = new();
    
    /// <summary>
    /// Настройки категорий логирования
    /// </summary>
    public CategoryLoggingSettings Categories { get; set; } = new();
}

/// <summary>
/// Настройки файлового логирования
/// </summary>
public class FileLoggingSettings
{
    /// <summary>
    /// Включить файловое логирование
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Путь к основному лог-файлу
    /// </summary>
    public string MainLogPath { get; set; } = "logs/clubdoorman-.log";
    
    /// <summary>
    /// Путь к файлу ошибок
    /// </summary>
    public string ErrorLogPath { get; set; } = "logs/errors-.log";
    
    /// <summary>
    /// Путь к файлу системных логов
    /// </summary>
    public string SystemLogPath { get; set; } = "logs/system-.log";
    
    /// <summary>
    /// Путь к файлу пользовательских логов
    /// </summary>
    public string UserFlowLogPath { get; set; } = "logs/userflow-.log";
    
    /// <summary>
    /// Количество дней хранения основных логов
    /// </summary>
    public int RetentionDays { get; set; } = 7;
    
    /// <summary>
    /// Количество дней хранения логов ошибок
    /// </summary>
    public int ErrorRetentionDays { get; set; } = 30;
    
    /// <summary>
    /// Количество дней хранения системных логов
    /// </summary>
    public int SystemRetentionDays { get; set; } = 14;
    
    /// <summary>
    /// Количество дней хранения пользовательских логов
    /// </summary>
    public int UserFlowRetentionDays { get; set; } = 7;
}

/// <summary>
/// Настройки Telegram уведомлений
/// </summary>
public class TelegramNotificationSettings
{
    /// <summary>
    /// Включить Telegram уведомления
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Отправлять админские уведомления
    /// </summary>
    public bool AdminNotifications { get; set; } = true;
    
    /// <summary>
    /// Отправлять лог-уведомления
    /// </summary>
    public bool LogNotifications { get; set; } = true;
    
    /// <summary>
    /// Отправлять пользовательские уведомления
    /// </summary>
    public bool UserNotifications { get; set; } = true;
    
    /// <summary>
    /// Настройки для разных типов уведомлений
    /// </summary>
    public NotificationTypeSettings NotificationTypes { get; set; } = new();
}

/// <summary>
/// Настройки типов уведомлений
/// </summary>
public class NotificationTypeSettings
{
    /// <summary>
    /// Настройки админских уведомлений
    /// </summary>
    public Dictionary<string, NotificationDestination> AdminNotifications { get; set; } = new()
    {
        ["AutoBan"] = NotificationDestination.AdminChat | NotificationDestination.LogChat | NotificationDestination.FileLog,
        ["AutoBanBlacklist"] = NotificationDestination.AdminChat | NotificationDestination.LogChat | NotificationDestination.FileLog,
        ["SuspiciousUser"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["AiProfileAnalysis"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["SystemError"] = NotificationDestination.AdminChat | NotificationDestination.LogChat | NotificationDestination.FileLog,
        ["ChannelMessage"] = NotificationDestination.AdminChat | NotificationDestination.LogChat | NotificationDestination.FileLog,
        ["UserCleanup"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["UserApproved"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["UserRestricted"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["UserRemovedFromApproved"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["AiDetectAutoDelete"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["AiDetectSuspicious"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["PrivateChatBanAttempt"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["BanForLongName"] = NotificationDestination.AdminChat | NotificationDestination.LogChat | NotificationDestination.FileLog,
        ["BanChannel"] = NotificationDestination.AdminChat | NotificationDestination.LogChat | NotificationDestination.FileLog,
        ["ChannelError"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["ModerationError"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["SuspiciousMessage"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["RemovedFromApproved"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["SystemInfo"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["Success"] = NotificationDestination.AdminChat | NotificationDestination.FileLog,
        ["Warning"] = NotificationDestination.AdminChat | NotificationDestination.FileLog
    };
    
    /// <summary>
    /// Настройки лог-уведомлений
    /// </summary>
    public Dictionary<string, NotificationDestination> LogNotifications { get; set; } = new()
    {
        ["AutoBanBlacklist"] = NotificationDestination.LogChat | NotificationDestination.FileLog,
        ["AutoBanFromBlacklist"] = NotificationDestination.LogChat | NotificationDestination.FileLog,
        ["BanForLongName"] = NotificationDestination.LogChat | NotificationDestination.FileLog,
        ["BanChannel"] = NotificationDestination.LogChat | NotificationDestination.FileLog,
        ["SuspiciousUser"] = NotificationDestination.LogChat | NotificationDestination.FileLog,
        ["AiProfileAnalysis"] = NotificationDestination.LogChat | NotificationDestination.FileLog,
        ["CriticalError"] = NotificationDestination.LogChat | NotificationDestination.FileLog,
        ["ChannelMessage"] = NotificationDestination.LogChat | NotificationDestination.FileLog
    };
    
    /// <summary>
    /// Настройки пользовательских уведомлений
    /// </summary>
    public Dictionary<string, NotificationDestination> UserNotifications { get; set; } = new()
    {
        ["ModerationWarning"] = NotificationDestination.UserChat | NotificationDestination.UserFlowLog,
        ["MessageDeleted"] = NotificationDestination.UserChat | NotificationDestination.UserFlowLog,
        ["UserBanned"] = NotificationDestination.UserChat | NotificationDestination.UserFlowLog,
        ["UserRestricted"] = NotificationDestination.UserChat | NotificationDestination.UserFlowLog,
        ["CaptchaShown"] = NotificationDestination.UserChat | NotificationDestination.UserFlowLog,
        ["Welcome"] = NotificationDestination.UserChat | NotificationDestination.UserFlowLog,
        ["Warning"] = NotificationDestination.UserChat | NotificationDestination.UserFlowLog,
        ["Success"] = NotificationDestination.UserChat | NotificationDestination.UserFlowLog,
        ["SystemInfo"] = NotificationDestination.UserChat | NotificationDestination.UserFlowLog,
        ["CaptchaWelcome"] = NotificationDestination.UserChat | NotificationDestination.UserFlowLog
    };
}

/// <summary>
/// Настройки категорий логирования
/// </summary>
public class CategoryLoggingSettings
{
    /// <summary>
    /// Логировать системные события
    /// </summary>
    public bool SystemEvents { get; set; } = true;
    
    /// <summary>
    /// Логировать пользовательские действия
    /// </summary>
    public bool UserActions { get; set; } = true;
    
    /// <summary>
    /// Логировать модерацию
    /// </summary>
    public bool Moderation { get; set; } = true;
    
    /// <summary>
    /// Логировать AI события
    /// </summary>
    public bool AiEvents { get; set; } = true;
    
    /// <summary>
    /// Логировать статистику
    /// </summary>
    public bool Statistics { get; set; } = true;
    
    /// <summary>
    /// Логировать ошибки
    /// </summary>
    public bool Errors { get; set; } = true;
}

/// <summary>
/// Направления отправки уведомлений
/// </summary>
[Flags]
public enum NotificationDestination
{
    /// <summary>
    /// Никуда не отправлять
    /// </summary>
    None = 0,
    
    /// <summary>
    /// В админский чат
    /// </summary>
    AdminChat = 1 << 0,
    
    /// <summary>
    /// В лог-чат
    /// </summary>
    LogChat = 1 << 1,
    
    /// <summary>
    /// Пользователю в чат
    /// </summary>
    UserChat = 1 << 2,
    
    /// <summary>
    /// В файловые логи
    /// </summary>
    FileLog = 1 << 3,
    
    /// <summary>
    /// В системные логи
    /// </summary>
    SystemLog = 1 << 4,
    
    /// <summary>
    /// В пользовательские логи
    /// </summary>
    UserFlowLog = 1 << 5
} 
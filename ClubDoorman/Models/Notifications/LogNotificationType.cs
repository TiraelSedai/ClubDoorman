namespace ClubDoorman.Models.Notifications;

/// <summary>
/// Типы уведомлений для лог-чата
/// </summary>
public enum LogNotificationType
{
    /// <summary>
    /// Автобан по блэклисту lols.bot
    /// </summary>
    AutoBanBlacklist,
    
    /// <summary>
    /// Автобан из блэклиста
    /// </summary>
    AutoBanFromBlacklist,
    
    /// <summary>
    /// Автобан за известное спам-сообщение
    /// </summary>
    AutoBanKnownSpam,
    
    /// <summary>
    /// Бан за длинное имя
    /// </summary>
    BanForLongName,
    
    /// <summary>
    /// Бан канала
    /// </summary>
    BanChannel,
    
    /// <summary>
    /// Подозрительный пользователь
    /// </summary>
    SuspiciousUser,
    
    /// <summary>
    /// AI анализ профиля
    /// </summary>
    AiProfileAnalysis,
    
    /// <summary>
    /// Критическая ошибка
    /// </summary>
    CriticalError,
    
    /// <summary>
    /// Сообщение от канала
    /// </summary>
    ChannelMessage
} 
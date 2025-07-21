namespace ClubDoorman.Models.Notifications;

/// <summary>
/// Типы уведомлений для админского чата
/// </summary>
public enum AdminNotificationType
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
    /// Попытка бана в приватном чате
    /// </summary>
    PrivateChatBanAttempt,
    
    /// <summary>
    /// Бан за длинное имя
    /// </summary>
    BanForLongName,
    
    /// <summary>
    /// Бан канала
    /// </summary>
    BanChannel,
    
    /// <summary>
    /// Удаление из списка одобренных
    /// </summary>
    RemovedFromApproved,
    
    /// <summary>
    /// Сообщение от канала
    /// </summary>
    ChannelMessage,
    
    /// <summary>
    /// Подозрительный пользователь
    /// </summary>
    SuspiciousUser,
    
    /// <summary>
    /// AI анализ профиля
    /// </summary>
    AiProfileAnalysis,
    
    /// <summary>
    /// Ошибка модерации
    /// </summary>
    ModerationError,
    
    /// <summary>
    /// Системная ошибка
    /// </summary>
    SystemError
} 
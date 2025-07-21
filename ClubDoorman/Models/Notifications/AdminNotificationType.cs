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
    SystemError,
    
    /// <summary>
    /// Автобан пользователя
    /// </summary>
    AutoBan,
    
    /// <summary>
    /// Подозрительное сообщение (требует проверки)
    /// </summary>
    SuspiciousMessage,
    
    /// <summary>
    /// Ошибка при работе с каналом
    /// </summary>
    ChannelError,
    
    /// <summary>
    /// Очистка пользователя из списков
    /// </summary>
    UserCleanup,

    /// <summary>
    /// Пользователь одобрен администратором
    /// </summary>
    UserApproved,

    /// <summary>
    /// Системная информация
    /// </summary>
    SystemInfo,

    /// <summary>
    /// Успешная операция
    /// </summary>
    Success,

    /// <summary>
    /// Предупреждение
    /// </summary>
    Warning,

    /// <summary>
    /// AI детект - автоудаление спама
    /// </summary>
    AiDetectAutoDelete,

    /// <summary>
    /// AI детект - подозрительное сообщение
    /// </summary>
    AiDetectSuspicious
} 
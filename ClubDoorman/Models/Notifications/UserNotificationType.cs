namespace ClubDoorman.Models.Notifications;

/// <summary>
/// Типы уведомлений для пользователей
/// </summary>
public enum UserNotificationType
{
    /// <summary>
    /// Предупреждение о модерации
    /// </summary>
    ModerationWarning,
    
    /// <summary>
    /// Сообщение удалено
    /// </summary>
    MessageDeleted,
    
    /// <summary>
    /// Пользователь забанен
    /// </summary>
    UserBanned,
    
    /// <summary>
    /// Пользователь ограничен
    /// </summary>
    UserRestricted,
    
    /// <summary>
    /// Капча показана
    /// </summary>
    CaptchaShown,
    
    /// <summary>
    /// Приветствие
    /// </summary>
    Welcome,
    
    /// <summary>
    /// Предупреждение
    /// </summary>
    Warning,
    
    /// <summary>
    /// Успешная операция
    /// </summary>
    Success,

    /// <summary>
    /// Системная информация
    /// </summary>
    SystemInfo,

    /// <summary>
    /// Приветствие после капчи
    /// </summary>
    CaptchaWelcome
} 
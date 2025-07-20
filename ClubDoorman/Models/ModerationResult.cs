namespace ClubDoorman.Models;

/// <summary>
/// Результат модерации сообщения
/// </summary>
public record ModerationResult(
    /// <summary>
    /// Действие, которое нужно выполнить с сообщением
    /// </summary>
    ModerationAction Action,
    
    /// <summary>
    /// Причина принятого решения
    /// </summary>
    string Reason,
    
    /// <summary>
    /// Уровень уверенности в решении (0.0 - 1.0)
    /// </summary>
    double? Confidence = null
);

/// <summary>
/// Действие модерации
/// </summary>
public enum ModerationAction
{
    /// <summary>
    /// Разрешить сообщение
    /// </summary>
    Allow,
    
    /// <summary>
    /// Удалить сообщение
    /// </summary>
    Delete,
    
    /// <summary>
    /// Забанить пользователя
    /// </summary>
    Ban,
    
    /// <summary>
    /// Отправить жалобу администраторам
    /// </summary>
    Report,
    
    /// <summary>
    /// Требуется ручная проверка
    /// </summary>
    RequireManualReview
} 
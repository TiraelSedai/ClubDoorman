namespace ClubDoorman.Models;

/// <summary>
/// Информация о подозрительном пользователе
/// </summary>
public record SuspiciousUserInfo(
    /// <summary>
    /// Дата и время добавления в список подозрительных
    /// </summary>
    DateTime SuspiciousAt,
    
    /// <summary>
    /// Первые три сообщения пользователя, которые привели к подозрению
    /// </summary>
    List<string> FirstMessages,
    
    /// <summary>
    /// Оценка ML классификатора мимикрии (0.0 - 1.0)
    /// </summary>
    double MimicryScore,
    
    /// <summary>
    /// Включен ли AI-детект для следующих сообщений этого пользователя
    /// </summary>
    bool AiDetectEnabled,
    
    /// <summary>
    /// Количество сообщений, написанных после попадания в подозрительные (для счетчика до одобрения)
    /// </summary>
    int MessagesSinceSuspicious
); 
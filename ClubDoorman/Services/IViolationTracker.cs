namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс для сервиса отслеживания нарушений пользователей
/// </summary>
public interface IViolationTracker
{
    /// <summary>
    /// Регистрирует нарушение и возвращает true если нужно забанить пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="violationType">Тип нарушения</param>
    /// <returns>true если нужно забанить пользователя</returns>
    bool RegisterViolation(long userId, long chatId, ViolationType violationType);
    
    /// <summary>
    /// Получает количество нарушений для пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="violationType">Тип нарушения</param>
    /// <returns>Количество нарушений</returns>
    int GetViolationCount(long userId, long chatId, ViolationType violationType);
    
    /// <summary>
    /// Сбрасывает счетчик нарушений для пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="violationType">Тип нарушения</param>
    void ResetViolations(long userId, long chatId, ViolationType violationType);
} 
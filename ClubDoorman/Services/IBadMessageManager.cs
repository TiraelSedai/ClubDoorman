namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс для менеджера плохих сообщений
/// </summary>
public interface IBadMessageManager
{
    /// <summary>
    /// Проверяет, является ли сообщение известным плохим (по хэшу).
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    /// <returns>true, если сообщение известно как плохое</returns>
    bool KnownBadMessage(string message);

    /// <summary>
    /// Добавляет сообщение в список плохих (если оно не пустое и не было добавлено ранее).
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    ValueTask MarkAsBad(string message);
} 
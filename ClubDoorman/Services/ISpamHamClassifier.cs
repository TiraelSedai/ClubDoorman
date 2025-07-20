namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс для классификатора спам/не-спам сообщений
/// </summary>
public interface ISpamHamClassifier
{
    /// <summary>
    /// Проверяет, является ли сообщение спамом
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    /// <returns>Кортеж (является ли спамом, оценка)</returns>
    Task<(bool Spam, float Score)> IsSpam(string message);

    /// <summary>
    /// Добавляет сообщение как спам в обучающий набор
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    Task AddSpam(string message);

    /// <summary>
    /// Добавляет сообщение как не-спам в обучающий набор
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    Task AddHam(string message);
} 
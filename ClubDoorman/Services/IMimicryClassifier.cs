namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс для классификатора мимикрии
/// </summary>
public interface IMimicryClassifier
{
    /// <summary>
    /// Анализ первых трех сообщений пользователя
    /// </summary>
    /// <param name="messages">Список первых сообщений</param>
    /// <returns>Оценка подозрительности от 0.0 (не подозрительно) до 1.0 (очень подозрительно)</returns>
    double AnalyzeMessages(List<string> messages);
} 
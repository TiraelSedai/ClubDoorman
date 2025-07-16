using Telegram.Bot.Types;

namespace ClubDoorman.Handlers;

/// <summary>
/// Базовый интерфейс для обработки обновлений Telegram
/// </summary>
public interface IUpdateHandler
{
    /// <summary>
    /// Определяет, может ли данный обработчик обработать указанное обновление
    /// </summary>
    bool CanHandle(Update update);

    /// <summary>
    /// Обрабатывает обновление
    /// </summary>
    Task HandleAsync(Update update, CancellationToken cancellationToken = default);
} 
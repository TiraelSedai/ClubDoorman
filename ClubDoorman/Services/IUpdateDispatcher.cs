using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Диспетчер обновлений Telegram
/// </summary>
public interface IUpdateDispatcher
{
    /// <summary>
    /// Обрабатывает обновление, находя подходящий обработчик
    /// </summary>
    Task DispatchAsync(Update update, CancellationToken cancellationToken = default);
} 
using ClubDoorman.Handlers;
using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Диспетчер обновлений Telegram
/// </summary>
public class UpdateDispatcher : IUpdateDispatcher
{
    private readonly IEnumerable<IUpdateHandler> _updateHandlers;
    private readonly ILogger<UpdateDispatcher> _logger;

    public UpdateDispatcher(
        IEnumerable<IUpdateHandler> updateHandlers,
        ILogger<UpdateDispatcher> logger)
    {
        _updateHandlers = updateHandlers;
        _logger = logger;
    }

    public async Task DispatchAsync(Update update, CancellationToken cancellationToken = default)
    {
        try
        {
            var handler = _updateHandlers.FirstOrDefault(h => h.CanHandle(update));
            if (handler == null)
            {
                _logger.LogDebug("Не найден обработчик для обновления типа {UpdateType}", update.Type);
                return;
            }

            await handler.HandleAsync(update, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке обновления {UpdateType}", update.Type);
        }
    }
} 
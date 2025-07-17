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
            _logger.LogDebug("🚀 Dispatcher получил update: Message={Msg}, Callback={CB}, ChatMember={CM}", 
                update.Message != null, update.CallbackQuery != null, update.ChatMember != null);
                
            foreach (var handler in _updateHandlers)
            {
                if (handler.CanHandle(update))
                {
                    _logger.LogDebug("✅ Handler {Type} принял update", handler.GetType().Name);
                    await handler.HandleAsync(update, cancellationToken);
                }
                else
                {
                    _logger.LogDebug("❌ Handler {Type} отклонил update", handler.GetType().Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке обновления {UpdateType}", update.Type);
        }
    }
} 
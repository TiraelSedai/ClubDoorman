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

    /// <summary>
    /// Создает экземпляр диспетчера обновлений.
    /// </summary>
    /// <param name="updateHandlers">Коллекция обработчиков обновлений</param>
    /// <param name="logger">Логгер для записи событий</param>
    /// <exception cref="ArgumentNullException">Если updateHandlers или logger равны null</exception>
    public UpdateDispatcher(
        IEnumerable<IUpdateHandler> updateHandlers,
        ILogger<UpdateDispatcher> logger)
    {
        _updateHandlers = updateHandlers ?? throw new ArgumentNullException(nameof(updateHandlers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Обрабатывает обновление Telegram, передавая его подходящим обработчикам.
    /// </summary>
    /// <param name="update">Обновление для обработки</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <exception cref="ArgumentNullException">Если update равен null</exception>
    public async Task DispatchAsync(Update update, CancellationToken cancellationToken = default)
    {
        if (update == null) throw new ArgumentNullException(nameof(update));

        try
        {
            _logger.LogDebug("🚀 Dispatcher получил update: Message={Msg}, Callback={CB}, ChatMember={CM}", 
                update.Message != null, update.CallbackQuery != null, update.ChatMember != null);
                
            // ARCHITECTURE - Consider if parallel processing or early termination is needed
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
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Обработка обновления была отменена");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке обновления {UpdateType}", update.Type);
            throw;
        }
    }
} 
using Telegram.Bot.Types;

namespace ClubDoorman.Handlers;

/// <summary>
/// Интерфейс для обработки callback запросов
/// </summary>
public interface ICallbackQueryHandler
{
    /// <summary>
    /// Определяет, может ли данный обработчик обработать указанный callback
    /// </summary>
    bool CanHandle(CallbackQuery callbackQuery);

    /// <summary>
    /// Обрабатывает callback запрос
    /// </summary>
    Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default);
} 
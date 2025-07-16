using Telegram.Bot.Types;

namespace ClubDoorman.Handlers;

/// <summary>
/// Интерфейс для обработки сообщений
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// Определяет, может ли данный обработчик обработать указанное сообщение
    /// </summary>
    bool CanHandle(Message message);

    /// <summary>
    /// Обрабатывает сообщение
    /// </summary>
    Task HandleAsync(Message message, CancellationToken cancellationToken = default);
} 
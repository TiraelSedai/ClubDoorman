using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для обработки команд
/// <tags>commands, processing, proxy</tags>
/// </summary>
public interface ICommandProcessingService
{
    /// <summary>
    /// Обрабатывает команду
    /// <tags>commands, processing, proxy</tags>
    /// </summary>
    /// <param name="message">Сообщение с командой</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task HandleCommandAsync(Message message, CancellationToken cancellationToken = default);
} 
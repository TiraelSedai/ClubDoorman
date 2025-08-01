using ClubDoorman.Handlers;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для обработки команд
/// <tags>commands, processing, proxy</tags>
/// </summary>
public class CommandProcessingService : ICommandProcessingService
{
    private readonly IMessageHandler _messageHandler;
    private readonly ILogger<CommandProcessingService> _logger;

    /// <summary>
    /// Создает экземпляр CommandProcessingService
    /// <tags>commands, constructor, dependency-injection</tags>
    /// </summary>
    /// <param name="messageHandler">Обработчик сообщений</param>
    /// <param name="logger">Логгер</param>
    public CommandProcessingService(IMessageHandler messageHandler, ILogger<CommandProcessingService> logger)
    {
        _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Обрабатывает команду
    /// <tags>commands, processing, proxy</tags>
    /// </summary>
    /// <param name="message">Сообщение с командой</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task HandleCommandAsync(Message message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("CommandProcessingService: Проксируем HandleCommandAsync к MessageHandler");
        await _messageHandler.HandleCommandAsync(message, cancellationToken);
    }
} 
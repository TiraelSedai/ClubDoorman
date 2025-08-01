using ClubDoorman.Handlers;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для модерации каналов
/// <tags>channel, moderation, proxy</tags>
/// </summary>
public class ChannelModerationService : IChannelModerationService
{
    private readonly IMessageHandler _messageHandler;
    private readonly ILogger<ChannelModerationService> _logger;

    /// <summary>
    /// Создает экземпляр ChannelModerationService
    /// <tags>channel, constructor, dependency-injection</tags>
    /// </summary>
    /// <param name="messageHandler">Обработчик сообщений</param>
    /// <param name="logger">Логгер</param>
    public ChannelModerationService(IMessageHandler messageHandler, ILogger<ChannelModerationService> logger)
    {
        _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Обрабатывает сообщение от канала
    /// <tags>channel, moderation, proxy</tags>
    /// </summary>
    /// <param name="message">Сообщение от канала</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task HandleChannelMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ChannelModerationService: Проксируем HandleChannelMessageAsync к MessageHandler");
        await _messageHandler.HandleChannelMessageAsync(message, cancellationToken);
    }
} 
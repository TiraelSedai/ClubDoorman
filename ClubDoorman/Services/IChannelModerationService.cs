using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для модерации каналов
/// <tags>channel, moderation, proxy</tags>
/// </summary>
public interface IChannelModerationService
{
    /// <summary>
    /// Обрабатывает сообщение от канала
    /// <tags>channel, moderation, proxy</tags>
    /// </summary>
    /// <param name="message">Сообщение от канала</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task HandleChannelMessageAsync(Message message, CancellationToken cancellationToken = default);
} 
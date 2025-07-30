using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Заглушка для совместимости с тестами
/// </summary>
public class UserBanService : IUserBanService
{
    private readonly ILogger<UserBanService> _logger;

    public UserBanService(ILogger<UserBanService> logger)
    {
        _logger = logger;
    }

    public Task BanUserForLongNameAsync(Message? userJoinMessage, User user, string reason, TimeSpan? banDuration, CancellationToken cancellationToken)
    {
        _logger.LogInformation("BanUserForLongNameAsync stub called: userId={UserId}, reason={Reason}", user?.Id, reason);
        return Task.CompletedTask;
    }

    public Task BanBlacklistedUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken)
    {
        _logger.LogInformation("BanBlacklistedUserAsync stub called: userId={UserId}", user?.Id);
        return Task.CompletedTask;
    }

    public Task AutoBanAsync(Message message, string reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("AutoBanAsync stub called: messageId={MessageId}, reason={Reason}", message?.MessageId, reason);
        return Task.CompletedTask;
    }

    public Task AutoBanChannelAsync(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("AutoBanChannelAsync stub called: messageId={MessageId}", message?.MessageId);
        return Task.CompletedTask;
    }

    public Task HandleBlacklistBanAsync(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HandleBlacklistBanAsync stub called: userId={UserId}, chatId={ChatId}", user?.Id, chat?.Id);
        return Task.CompletedTask;
    }
} 
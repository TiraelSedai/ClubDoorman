using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using ClubDoorman.Handlers;

namespace ClubDoorman.Services;

/// <summary>
/// Мостик к реальной логике банов в MessageHandler
/// </summary>
public class UserBanService : IUserBanService
{
    private readonly ILogger<UserBanService> _logger;
    private readonly MessageHandler _messageHandler;

    public UserBanService(ILogger<UserBanService> logger, MessageHandler messageHandler)
    {
        _logger = logger;
        _messageHandler = messageHandler;
    }

    public Task BanUserForLongNameAsync(Message? userJoinMessage, User user, string reason, TimeSpan? banDuration, CancellationToken cancellationToken)
    {
        return _messageHandler.BanUserForLongName(userJoinMessage, user, reason, banDuration, cancellationToken);
    }

    public Task BanBlacklistedUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken)
    {
        return _messageHandler.BanBlacklistedUser(userJoinMessage, user, cancellationToken);
    }

    public Task AutoBanAsync(Message message, string reason, CancellationToken cancellationToken)
    {
        return _messageHandler.AutoBan(message, reason, cancellationToken);
    }

    public Task AutoBanChannelAsync(Message message, CancellationToken cancellationToken)
    {
        return _messageHandler.AutoBanChannel(message, cancellationToken);
    }

    public Task HandleBlacklistBanAsync(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        return _messageHandler.HandleBlacklistBan(message, user, chat, cancellationToken);
    }
} 
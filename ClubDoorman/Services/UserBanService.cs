using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Handlers;
using ClubDoorman.Models.Notifications;

namespace ClubDoorman.Services;

/// <summary>
/// Мостик к реальной логике банов в MessageHandler
/// </summary>
public class UserBanService : IUserBanService
{
    private readonly MessageHandler _messageHandler;

    public UserBanService(MessageHandler messageHandler)
    {
        _messageHandler = messageHandler;
    }

    public async Task BanUserForLongNameAsync(Message? userJoinMessage, User user, string reason, TimeSpan? banDuration, CancellationToken cancellationToken)
    {
        await _messageHandler.BanUserForLongName(userJoinMessage, user, reason, banDuration, cancellationToken);
    }

    public async Task BanBlacklistedUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken)
    {
        await _messageHandler.BanBlacklistedUser(userJoinMessage, user, cancellationToken);
    }

    public async Task AutoBanAsync(Message message, string reason, CancellationToken cancellationToken)
    {
        await _messageHandler.AutoBan(message, reason, cancellationToken);
    }

    public async Task AutoBanChannelAsync(Message message, CancellationToken cancellationToken)
    {
        await _messageHandler.AutoBanChannel(message, cancellationToken);
    }

    public Task HandleBlacklistBanAsync(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        return _messageHandler.HandleBlacklistBan(message, user, chat, cancellationToken);
    }
} 
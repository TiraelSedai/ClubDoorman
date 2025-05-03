using System.Runtime.Caching;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ClubDoorman;

internal class ReactionHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly UserManager _userManager;
    private readonly AiChecks _aiChecks;
    private readonly ILogger<ReactionHandler> _logger;

    public ReactionHandler(ITelegramBotClient bot, UserManager userManager, AiChecks aiChecks, ILogger<ReactionHandler> logger)
    {
        _bot = bot;
        _userManager = userManager;
        _aiChecks = aiChecks;
        _logger = logger;
    }

    public async ValueTask HandleReaction(MessageReactionUpdated reaction)
    {
        var user = reaction.User;
        if (user == null)
            return;
        if (_userManager.Approved(user.Id))
            return;
        if (reaction.NewReaction.Length == 0)
            return;

        var userKey = $"user:{user.Id}";
        var cache = new ReactionCache();
        if (
            MemoryCache.Default.AddOrGetExisting(
                userKey,
                new ReactionCache(),
                new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(15) }
            )
            is ReactionCache reactionCache
        )
        {
            cache = reactionCache;
        }
        cache.ReactionCount++;

        if (cache.ReactionCount > 0 && Config.MultiAdminChatMap.ContainsKey(reaction.Chat.Id))
        {
            _logger.LogDebug("Reaction number {Count} from {User} in chat {Chat}", cache.ReactionCount, Utils.FullName(user), reaction.Chat.Title);
            var admChat = Config.GetAdminChat(reaction.Chat.Id);
            var (attentionProb, photo, bio) = await _aiChecks.GetAttentionBaitProbability(user);
            _logger.LogDebug("Reaction bait spam probability {Prob}", attentionProb);
            if (attentionProb >= 0.75)
            {
                var postLink = Utils.LinkToMessage(reaction.Chat, reaction.MessageId);
                ReplyParameters? replyParameters = null;
                if (photo.Length != 0)
                {
                    using var ms = new MemoryStream(photo);
                    replyParameters = await _bot.SendPhoto(admChat, new InputFileStream(ms), bio);
                }
                await _bot.SendMessage(
                    admChat,
                    $"Вероятность что реакцию поставил бейт спаммер {attentionProb * 100}%. в тестовом режиме не забанен, просто сообщаю - забаньте руками если это так.{Environment.NewLine}Юзер {Utils.FullName(user)} из чата {reaction.Chat.Title}{Environment.NewLine}{postLink}",
                    replyParameters: replyParameters
                );
            }
        }
    }

    private class ReactionCache
    {
        public int ReactionCount;
    }
}

using System.Runtime.Caching;
using Microsoft.VisualBasic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

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
                new CacheItemPolicy { SlidingExpiration = TimeSpan.FromDays(1) }
            )
            is ReactionCache reactionCache
        )
        {
            cache = reactionCache;
        }
        cache.ReactionCount++;

        if (cache.ReactionCount < 1 && Config.MultiAdminChatMap.ContainsKey(reaction.Chat.Id))
        {
            _logger.LogDebug(
                "Reaction number {Count} from {User} in chat {Chat}",
                cache.ReactionCount,
                Utils.FullName(user),
                reaction.Chat.Title
            );
            var admChat = Config.GetAdminChat(reaction.Chat.Id);
            var (attentionProb, photo, bio) = await _aiChecks.GetAttentionBaitProbability(user);
            _logger.LogDebug("Reaction bait spam probability {Prob}", attentionProb);
            if (attentionProb >= Consts.LlmLowProbability)
            {
                var postLink = Utils.LinkToMessage(reaction.Chat, reaction.MessageId);
                ReplyParameters? replyParameters = null;
                if (photo.Length != 0)
                {
                    using var ms = new MemoryStream(photo);
                    replyParameters = await _bot.SendPhoto(admChat, new InputFileStream(ms), bio);
                }

                var keyboard = new List<InlineKeyboardButton>
                {
                    new("👍 ok") { CallbackData = $"attOk_{user.Id}" },
                    new("🤖 ban") { CallbackData = $"ban_{reaction.Chat.Id}_{user.Id}" },
                };
                await _bot.SendMessage(
                    admChat,
                    $"Вероятность что реакцию поставил бейт спаммер {attentionProb * 100}%. Бан не сможет снять реакцию, если хотите - сходите по ссылке в посте и зарепортите его вручную.{Environment.NewLine}Юзер {Utils.FullName(user)} из чата {reaction.Chat.Title}{Environment.NewLine}{postLink}",
                    replyParameters: replyParameters,
                    replyMarkup: new InlineKeyboardMarkup(keyboard)
                );
            }
        }
    }

    private class ReactionCache
    {
        public int ReactionCount;
    }
}

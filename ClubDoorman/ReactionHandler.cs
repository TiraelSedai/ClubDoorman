using System.Runtime.Caching;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman;

internal class ReactionHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly UserManager _userManager;
    private readonly AiChecks _aiChecks;
    private readonly Config _config;
    private readonly ILogger<ReactionHandler> _logger;

    public ReactionHandler(
        ITelegramBotClient bot,
        UserManager userManager,
        AiChecks aiChecks,
        Config config,
        ILogger<ReactionHandler> logger
    )
    {
        _bot = bot;
        _userManager = userManager;
        _aiChecks = aiChecks;
        _config = config;
        _logger = logger;
    }

    public async ValueTask HandleReaction(MessageReactionUpdated reaction)
    {
        var user = reaction.User;
        if (user == null)
            return;
        if (_userManager.Approved(user.Id))
            return;
        var chat = reaction.Chat;
        if (await _userManager.InBanlist(user.Id))
        {
            try
            {
                await _bot.BanChatMember(chat.Id, user.Id);
                _logger.LogDebug("Banned blacklisted user {FullName} @{Username} based on reaction", Utils.FullName(user), user.Username);
            }
            catch { }
            return;
        }
        if (reaction.NewReaction.Length == 0)
            return;

        var userKey = $"attention:{user.Id}";
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

        if (cache.ReactionCount <= 1 && _config.MultiAdminChatMap.ContainsKey(chat.Id))
        {
            _logger.LogDebug("Reaction number {Count} from {User} in chat {Chat}", cache.ReactionCount, Utils.FullName(user), chat.Title);
            var admChat = _config.GetAdminChat(chat.Id);
            var (attention, photo, bio) = await _aiChecks.GetAttentionBaitProbability(user, null, true);
            _logger.LogDebug("Reaction bait spam probability {Prob}", attention.Probability);
            if (attention.Probability >= Consts.LlmLowProbability)
            {
                var postLink = Utils.LinkToMessage(chat, reaction.MessageId);
                ReplyParameters? replyParameters = null;
                if (photo.Length != 0)
                {
                    using var ms = new MemoryStream(photo);
                    replyParameters = await _bot.SendPhoto(admChat, new InputFileStream(ms), bio);
                }

                var keyboard = new List<InlineKeyboardButton>
                {
                    new(Consts.BanButton) { CallbackData = $"ban_{chat.Id}_{user.Id}" },
                    new(Consts.OkButton) { CallbackData = $"attOk_{user.Id}" },
                };
                var at = user.Username == null ? "" : $" @{user.Username} ";
                await _bot.SendMessage(
                    admChat,
                    $"Вероятность что реакцию поставил бейт спаммер {attention.Probability * 100}%.{Environment.NewLine}{attention.Reason}{Environment.NewLine}Бан не сможет снять реакцию, если хотите - сходите по ссылке в посте и зарепортите его вручную.{Environment.NewLine}Юзер {Utils.FullName(user)}{at} из чата {chat.Title}{Environment.NewLine}{postLink}",
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

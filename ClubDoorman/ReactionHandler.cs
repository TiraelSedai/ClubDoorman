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
        var chat = reaction.Chat;
        if (_userManager.Approved(user.Id))
            return;
        if (await _userManager.InBanlist(user.Id))
        {
            try
            {
                await _bot.BanChatMember(chat.Id, user.Id);
                _logger.LogDebug("Banned blacklisted user {FullName} @{Username} based on reaction", Utils.FullName(user), user.Username);
                if (_config.NonFreeChat(chat.Id))
                {
                    await _bot.SendMessage(
                        _config.GetAdminChat(chat.Id),
                        BuildReactionAutobanNotificationMessage(chat, user, reaction.MessageId)
                    );
                }
            }
            catch { }
            return;
        }
        if (reaction.NewReaction.Length == 0)
            return;

        var count = CountReaction($"reactions:{user.Id}");

        if (count <= 1 && _config.MultiAdminChatMap.ContainsKey(chat.Id))
        {
            _logger.LogDebug("Reaction number {Count} from {User} in chat {Chat}", count, Utils.FullName(user), chat.Title);
            var admChat = _config.GetAdminChat(chat.Id);
            if (_config.OpenRouterApi == null)
                return;
            ChatFullInfo userChat;
            try
            {
                userChat = await _bot.GetChat(user.Id);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to fetch chat info for reaction bait check");
                return;
            }
            var (attention, photo, bio) = await _aiChecks.GetAttentionBaitProbability(user, userChat);
            _logger.LogDebug("Reaction bait spam probability {Prob}", attention.EroticProbability);
            if (attention.EroticProbability >= Consts.LlmLowProbability)
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
                    $"Вероятность что реакцию поставил профиль эротического содержания {attention.EroticProbability * 100}%.{Environment.NewLine}{attention.Reason}{Environment.NewLine}Бан не сможет снять реакцию, если хотите - сходите по ссылке в посте и зарепортите его вручную.{Environment.NewLine}Юзер {Utils.FullName(user)}{at} из чата {chat.Title}{Environment.NewLine}{postLink}",
                    replyParameters: replyParameters,
                    replyMarkup: new InlineKeyboardMarkup(keyboard)
                );
            }
        }
    }

    /// <summary>Reactions seen from one user so far, counting this one. Starts at 1.</summary>
    internal static int CountReaction(string userKey)
    {
        var fresh = new ReactionCache();
        // AddOrGetExisting returns null when it inserts, so the fallback has to be the very instance we handed it:
        // incrementing a throwaway would let the caller's "first reaction only" gate open twice
        var counter =
            MemoryCache.Default.AddOrGetExisting(userKey, fresh, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromDays(1) })
                as ReactionCache
            ?? fresh;
        return Interlocked.Increment(ref counter.ReactionCount);
    }

    private class ReactionCache
    {
        public int ReactionCount;
    }

    internal static string BuildReactionAutobanNotificationMessage(Chat chat, User user, int messageId)
    {
        var at = user.Username == null ? "" : $" @{user.Username}";
        return $"Авто-бан по реакции: пользователь из банлиста{Environment.NewLine}Юзер {Utils.FullName(user)}{at} из чата {chat.Title}{Environment.NewLine}{Utils.LinkToMessage(chat, messageId)}";
    }
}

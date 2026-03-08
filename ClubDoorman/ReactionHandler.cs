using System.Collections.Concurrent;
using System.Runtime.Caching;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman;

internal class ReactionHandler
{
    private static readonly TimeSpan EmojiCheckIgnoreFor = TimeSpan.FromMinutes(1);

    private readonly ITelegramBotClient _bot;
    private readonly UserManager _userManager;
    private readonly AiChecks _aiChecks;
    private readonly Config _config;
    private readonly ILogger<ReactionHandler> _logger;
    private readonly ConcurrentDictionary<(long ChatId, int MessageId), EmojiOnlyCheck> _emojiOnlyChecks = new();

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
        {
            if (IsIgnoredEmojiOnlyCheck(chat.Id, reaction.MessageId))
                return;
            return;
        }
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
        if (TryHandleEmojiOnlyCheckReaction(chat.Id, reaction.MessageId, user.Id, reaction.NewReaction.Length != 0))
            return;
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
        var count = Interlocked.Increment(ref cache.ReactionCount);

        if (count <= 1 && _config.MultiAdminChatMap.ContainsKey(chat.Id))
        {
            _logger.LogDebug("Reaction number {Count} from {User} in chat {Chat}", count, Utils.FullName(user), chat.Title);
            var admChat = _config.GetAdminChat(chat.Id);
            var (attention, photo, bio) = await _aiChecks.GetAttentionBaitProbability(user, null, true);
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

    private class ReactionCache
    {
        public int ReactionCount;
    }

    private sealed class EmojiOnlyCheck(long userId)
    {
        public long UserId { get; } = userId;
        public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task RegisterEmojiOnlyCheck(long chatId, int messageId, long userId)
    {
        var key = (chatId, messageId);
        var state = new EmojiOnlyCheck(userId);
        _emojiOnlyChecks[key] = state;
        MemoryCache.Default.Remove(EmojiOnlyCheckIgnoreKey(chatId, messageId));
        return state.Completion.Task;
    }

    public void FinishEmojiOnlyCheck(long chatId, int messageId)
    {
        _emojiOnlyChecks.TryRemove((chatId, messageId), out _);
        MemoryCache.Default.Set(EmojiOnlyCheckIgnoreKey(chatId, messageId), true, DateTimeOffset.UtcNow.Add(EmojiCheckIgnoreFor));
    }

    private bool TryHandleEmojiOnlyCheckReaction(long chatId, int messageId, long reactingUserId, bool hasReaction)
    {
        var key = (chatId, messageId);
        if (_emojiOnlyChecks.TryGetValue(key, out var check))
        {
            if (hasReaction && reactingUserId == check.UserId && _emojiOnlyChecks.TryRemove(key, out var activeCheck))
            {
                activeCheck.Completion.TrySetResult(true);
                FinishEmojiOnlyCheck(chatId, messageId);
            }
            return true;
        }

        return IsIgnoredEmojiOnlyCheck(chatId, messageId);
    }

    private static bool IsIgnoredEmojiOnlyCheck(long chatId, int messageId) =>
        MemoryCache.Default.Contains(EmojiOnlyCheckIgnoreKey(chatId, messageId));

    private static string EmojiOnlyCheckIgnoreKey(long chatId, int messageId) => $"emoji_check_ignore:{chatId}:{messageId}";
}

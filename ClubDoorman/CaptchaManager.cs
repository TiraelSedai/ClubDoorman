using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman;

internal partial class CaptchaManager
{
    private sealed class CaptchaInfo
    {
        public long ChatId { get; set; }
        public string? ChatTitle { get; set; }
        public DateTime Timestamp { get; set; }
        public required User User { get; set; }
        public int CorrectAnswer { get; set; }
        public CancellationTokenSource Cts { get; } = new();
    }

    private sealed class InlineChallenge(int correctAnswer)
    {
        public int CorrectAnswer { get; } = correctAnswer;
        public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private readonly ConcurrentDictionary<string, CaptchaInfo> _captchaNeededUsers = new();
    private readonly ConcurrentDictionary<string, InlineChallenge> _inlineChallenges = new();
    private readonly ConcurrentDictionary<long, bool> _optoutChats = new();
    private readonly ITelegramBotClient _bot;
    private readonly UserManager _userManager;
    private readonly StatisticsReporter _statistics;
    private readonly Config _config;
    private readonly HybridCache _cache;
    private readonly ILogger<CaptchaManager> _logger;

    public CaptchaManager(
        ITelegramBotClient bot,
        UserManager userManager,
        StatisticsReporter statistics,
        Config config,
        HybridCache cache,
        ILogger<CaptchaManager> logger
    )
    {
        _bot = bot;
        _userManager = userManager;
        _statistics = statistics;
        _config = config;
        _cache = cache;
        _logger = logger;
    }

    public bool IsCaptchaNeeded(long chatId, User user) => _captchaNeededUsers.ContainsKey(UserToKey(chatId, user));

    public async Task CaptchaLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), token);
            _ = BanNoCaptchaUsers();
        }
    }

    public async Task HandleCaptchaCallback(Update update)
    {
        var cb = update.CallbackQuery;
        Debug.Assert(cb != null);
        var cbData = cb.Data;
        if (cbData == null)
            return;
        var message = cb.Message;
        Debug.Assert(message != null);

        if (ParseCaptchaCallback(cbData) is not { } answer)
            return;
        // Prevent other people from ruining the flow
        if (cb.From.Id != answer.UserId)
        {
            await _bot.AnswerCallbackQuery(cb.Id);
            return;
        }

        var chat = message.Chat;
        var key = UserToKey(chat.Id, cb.From);

        if (answer.ChallengedMessageId is { } challengedMessageId)
        {
            await _bot.AnswerCallbackQuery(cb.Id);
            // ChallengeInChat owns the message and deletes it once the challenge is resolved
            var inlineKey = InlineChallengeKey(chat.Id, cb.From.Id, challengedMessageId);
            if (_inlineChallenges.TryGetValue(inlineKey, out var challenge))
                challenge.Completion.TrySetResult(challenge.CorrectAnswer == answer.Chosen);
            return;
        }

        var ok = _captchaNeededUsers.TryRemove(key, out var info);
        await DeleteMessageSafe(message);
        if (!ok)
        {
            _logger.LogWarning("{Key} was not found in the dictionary _captchaNeededUsers", key);
            return;
        }
        Debug.Assert(info != null);
        await info.Cts.CancelAsync();
        if (info.CorrectAnswer != answer.Chosen)
        {
            var stats = _statistics.Stats.GetOrAdd(chat.Id, new Stats(chat.Title) { Id = chat.Id });
            stats.StoppedCaptcha++;
            await _bot.BanChatMember(chat, answer.UserId, DateTime.UtcNow + TimeSpan.FromMinutes(10), revokeMessages: false);
            UnbanUserLater(chat, answer.UserId);
        }
    }

    public async ValueTask IntroFlow(User user, Chat chat)
    {
        if (_userManager.Approved(user.Id))
            return;
        var clubUser = await _userManager.GetClubUsername(user.Id);
        if (clubUser != null)
            return;

        var chatId = chat.Id;

        if (await BanIfBlacklisted(user, chat))
            return;

        if (_optoutChats.ContainsKey(chatId))
            return;

        if (_config.CaptchaDisabledChats.Contains(chatId))
            return;

        if (await IsDiscussionChatAsync(chatId) == true)
            return;

        var key = UserToKey(chatId, user);

        var justAdded = _captchaNeededUsers.TryAdd(key, new CaptchaInfo() { Timestamp = DateTime.MaxValue, User = user });
        var captchaInfo = _captchaNeededUsers[key];
        if (!justAdded)
        {
            _logger.LogDebug("This user is already awaiting captcha challenge");
            return;
        }

        var (correctAnswer, keyboard) = BuildChallenge(user.Id, challengedMessageId: null);

        try
        {
            // The captcha is ephemeral: only the newcomer sees it, so no need to sanitize their name for the chat
            var sent = await _bot.SendMessage(
                chatId,
                $"Привет, {Utils.FullName(user)}! Антиспам: на какой кнопке {Captcha.CaptchaList[correctAnswer].Description}?",
                replyMarkup: keyboard,
                receiverUserId: user.Id
            );
            DeleteMessageLater(sent, TimeSpan.FromSeconds(45), captchaInfo.Cts.Token);
        }
        catch (ApiRequestException e) when (e.Message.Contains("TOPIC_CLOSED"))
        {
            _captchaNeededUsers.TryRemove(key, out _);
            _optoutChats.TryAdd(chatId, true);
            _logger.LogInformation("Topic closed, chat = {Chat}", chat.Title);
            return;
        }
        catch (Exception e)
        {
            // Without the captcha in flight the user would stay muted forever: IsCaptchaNeeded eats all their messages
            _captchaNeededUsers.TryRemove(key, out _);
            _logger.LogWarning(e, "Unable to send captcha, chat = {Chat}, user = {User}", chat.Title, user.Id);
            return;
        }

        captchaInfo.ChatId = chatId;
        captchaInfo.ChatTitle = chat.Title;
        captchaInfo.Timestamp = DateTime.UtcNow;
        captchaInfo.CorrectAnswer = correctAnswer;

        return;
    }

    /// <summary>
    /// Challenges an existing chat member with an ephemeral captcha replying to <paramref name="message"/>.
    /// Returns true if they picked the right button within <paramref name="wait"/>.
    /// </summary>
    public async Task<bool> ChallengeInChat(Message message, string prompt, TimeSpan wait, CancellationToken cancellationToken)
    {
        var user = message.From!;
        // Keyed per challenged message: the same user may post several such messages before answering
        var key = InlineChallengeKey(message.Chat.Id, user.Id, message.MessageId);
        var (correctAnswer, keyboard) = BuildChallenge(user.Id, message.MessageId);
        var challenge = new InlineChallenge(correctAnswer);
        if (!_inlineChallenges.TryAdd(key, challenge))
        {
            // Same message challenged twice - an edit arrives while the captcha is live. The in-flight
            // challenge owns this message and will delete it if unanswered, so this call must not report it
            _logger.LogDebug("{Key} is already being challenged in chat", key);
            return true;
        }

        Message? sent = null;
        try
        {
            sent = await _bot.SendMessage(
                message.Chat.Id,
                $"{prompt} Нажмите кнопку, на которой {Captcha.CaptchaList[correctAnswer].Description}, у вас {wait.TotalSeconds:0} секунд.",
                replyParameters: message,
                replyMarkup: keyboard,
                receiverUserId: user.Id,
                cancellationToken: cancellationToken
            );
            var completed = await Task.WhenAny(challenge.Completion.Task, Task.Delay(wait, cancellationToken));
            return completed == challenge.Completion.Task && challenge.Completion.Task.Result;
        }
        finally
        {
            // Must cover the send too: nothing else ever removes from _inlineChallenges
            _inlineChallenges.TryRemove(key, out _);
            if (sent != null)
                await DeleteMessageSafe(sent, cancellationToken);
        }
    }

    internal static (int CorrectAnswer, InlineKeyboardMarkup Keyboard) BuildChallenge(long userId, int? challengedMessageId)
    {
        var prefix = challengedMessageId is null ? JoinCaptchaPrefix : InlineCaptchaPrefix;
        var suffix = challengedMessageId is { } id ? $"_{id}" : "";
        const int challengeLength = 8;
        var challenge = new List<int>(challengeLength);
        while (challenge.Count < challengeLength)
        {
            var rand = Random.Shared.Next(Captcha.CaptchaList.Count);
            if (!challenge.Contains(rand))
                challenge.Add(rand);
        }
        var correctAnswer = challenge[Random.Shared.Next(challengeLength)];
        var keyboard = challenge
            .Select(x => new InlineKeyboardButton(Captcha.CaptchaList[x].Emoji) { CallbackData = $"{prefix}_{userId}_{x}{suffix}" })
            .ToList();
        return (correctAnswer, new InlineKeyboardMarkup(keyboard));
    }

    /// <param name="ChallengedMessageId">The message an inline captcha guards; null for the join captcha.</param>
    internal readonly record struct CaptchaAnswer(long UserId, int Chosen, int? ChallengedMessageId);

    //$"cap_{user.Id}_{x}" for the join captcha, $"capi_{user.Id}_{x}_{messageId}" for the inline one
    internal static CaptchaAnswer? ParseCaptchaCallback(string cbData)
    {
        var split = cbData.Split('_');
        if (split.Length < 3)
            return null;
        if (!long.TryParse(split[1], out var userId))
            return null;
        if (!int.TryParse(split[2], out var chosen))
            return null;
        switch (split[0])
        {
            case JoinCaptchaPrefix:
                return new CaptchaAnswer(userId, chosen, null);
            case InlineCaptchaPrefix when split.Length > 3 && int.TryParse(split[3], out var challengedMessageId):
                return new CaptchaAnswer(userId, chosen, challengedMessageId);
            default:
                return null;
        }
    }

    private async Task<bool> BanIfBlacklisted(User user, Chat chat)
    {
        if (!_config.BlacklistAutoBan)
            return false;
        if (!await _userManager.InBanlist(user.Id))
            return false;

        try
        {
            var stats = _statistics.Stats.GetOrAdd(chat.Id, new Stats(chat.Title) { Id = chat.Id });
            stats.BlacklistBanned++;
            await _bot.BanChatMember(chat.Id, user.Id);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to ban");
            if (_config.NonFreeChat(chat.Id))
                await _bot.SendMessage(
                    _config.GetAdminChat(chat.Id),
                    $"Не могу забанить юзера из блеклиста. Не хватает могущества? Сходите забаньте руками, чат {chat.Title}"
                );
        }

        return false;
    }

    private async Task BanNoCaptchaUsers()
    {
        if (_captchaNeededUsers.IsEmpty)
            return;
        var now = DateTime.UtcNow;
        var users = _captchaNeededUsers.ToArray();
        foreach (var (key, info) in users)
        {
            var seconds = (now - info.Timestamp).TotalSeconds;
            if (seconds > 45)
            {
                var stats = _statistics.Stats.GetOrAdd(info.ChatId, new Stats(info.ChatTitle) { Id = info.ChatId });
                stats.StoppedCaptcha++;
                _captchaNeededUsers.TryRemove(key, out _);
                await _bot.BanChatMember(info.ChatId, info.User.Id, now + TimeSpan.FromMinutes(20), revokeMessages: false);
                UnbanUserLater(info.ChatId, info.User.Id);
            }
        }
    }

    private void UnbanUserLater(ChatId chatId, long userId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                await _bot.UnbanChatMember(chatId, userId);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, nameof(UnbanUserLater));
            }
        });
    }

    private void DeleteMessageLater(Message message, TimeSpan after = default, CancellationToken cancellationToken = default)
    {
        if (after == default)
            after = TimeSpan.FromMinutes(5);
        _ = Task.Run(
            async () =>
            {
                await Task.Delay(after, cancellationToken);
                await DeleteMessageSafe(message, cancellationToken);
            },
            cancellationToken
        );
    }

    private async Task DeleteMessageSafe(Message message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Ephemeral messages have MessageId == 0 and live under their own id space
            if (message.EphemeralMessageId is { } ephemeralMessageId)
                await _bot.DeleteEphemeralMessage(message.Chat.Id, message.ReceiverUser!.Id, ephemeralMessageId, cancellationToken);
            else
                await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "DeleteMessage, chat = {Chat}, messageId = {MessageId}, ephemeralMessageId = {EphemeralMessageId}",
                message.Chat.Id,
                message.MessageId,
                message.EphemeralMessageId
            );
        }
    }

    private const string JoinCaptchaPrefix = "cap";
    private const string InlineCaptchaPrefix = "capi";

    private static string UserToKey(long chatId, User user) => $"{chatId}_{user.Id}";

    private static string InlineChallengeKey(long chatId, long userId, int messageId) => $"{chatId}_{userId}_{messageId}";

    private static string DiscussionChatCacheKey(long chatId) => $"discussion_chat:{chatId}";

    private async ValueTask<bool?> IsDiscussionChatAsync(long chatId, CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync<bool?>(
            DiscussionChatCacheKey(chatId),
            async ct =>
            {
                try
                {
                    var chat = await _bot.GetChat(chatId, cancellationToken: ct);
                    return chat.LinkedChatId != null;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to check if chat {ChatId} is discussion chat", chatId);
                    return null;
                }
            },
            new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromHours(24) },
            cancellationToken: ct
        );
    }
}

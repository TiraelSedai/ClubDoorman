using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static Telegram.Bot.Extensions.Markdown;

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

    private readonly ConcurrentDictionary<string, CaptchaInfo> _captchaNeededUsers = new();
    private readonly ConcurrentDictionary<long, bool> _optoutChats = new();
    private readonly ITelegramBotClient _bot;
    private readonly UserManager _userManager;
    private readonly StatisticsReporter _statistics;
    private readonly Config _config;
    private readonly HybridCache _cache;
    private readonly ILogger<CaptchaManager> _logger;

    private readonly List<string> _namesBlacklist =
    [
        "p0rn",
        "porn",
        "порн",
        "п0рн",
        "pоrn",
        "пoрн",
        "орно",
        "ponr",
        "bot",
        "b0t",
        "вот",
        "в0т",
        "child",
        "chlid",
    ];

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

        //$"cap_{user.Id}_{x}"
        var split = cbData.Split('_');
        if (split.Length < 3)
            return;
        if (split[0] != "cap")
            return;
        if (!long.TryParse(split[1], out var userId))
            return;
        if (!int.TryParse(split[2], out var chosen))
            return;
        // Prevent other people from ruining the flow
        if (cb.From.Id != userId)
        {
            await _bot.AnswerCallbackQuery(cb.Id);
            return;
        }

        var chat = message.Chat;
        var key = UserToKey(chat.Id, cb.From);
        var ok = _captchaNeededUsers.TryRemove(key, out var info);
        await _bot.DeleteMessage(chat, message.MessageId);
        if (!ok)
        {
            _logger.LogWarning("{Key} was not found in the dictionary _captchaNeededUsers", key);
            return;
        }
        Debug.Assert(info != null);
        await info.Cts.CancelAsync();
        if (info.CorrectAnswer != chosen)
        {
            var stats = _statistics.Stats.GetOrAdd(chat.Id, new Stats(chat.Title) { Id = chat.Id });
            stats.StoppedCaptcha++;
            await _bot.BanChatMember(chat, userId, DateTime.UtcNow + TimeSpan.FromMinutes(10), revokeMessages: false);
            UnbanUserLater(chat, userId);
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

        const int challengeLength = 8;
        var correctAnswerIndex = Random.Shared.Next(challengeLength);
        var challenge = new List<int>(challengeLength);
        while (challenge.Count < challengeLength)
        {
            var rand = Random.Shared.Next(Captcha.CaptchaList.Count);
            if (!challenge.Contains(rand))
                challenge.Add(rand);
        }
        var correctAnswer = challenge[correctAnswerIndex];
        var keyboard = challenge
            .Select(x => new InlineKeyboardButton(Captcha.CaptchaList[x].Emoji) { CallbackData = $"cap_{user.Id}_{x}" })
            .ToList();

        var fullName = Utils.FullName(user);
        var fullNameLower = fullName.ToLowerInvariant();
        var usernameLower = user.Username?.ToLower();
        if (
            _namesBlacklist.Any(fullNameLower.Contains)
            || (usernameLower != null && _namesBlacklist.Any(usernameLower.Contains))
            || SimpleFilters.FindAllRussianWordsWithLookalikeSymbols(fullNameLower).Any()
            || (usernameLower != null && SimpleFilters.FindAllRussianWordsWithLookalikeSymbols(usernameLower).Any())
        )
            fullName = "новый участник чата";

        try
        {
            var del = await _bot.SendMessage(
                chatId,
                $"Привет, [{Escape(fullName)}](tg://user?id={user.Id})! Антиспам: на какой кнопке {Captcha.CaptchaList[correctAnswer].Description}?",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(keyboard)
            );
            DeleteMessageLater(del, TimeSpan.FromSeconds(45), captchaInfo.Cts.Token);
        }
        catch (ApiRequestException e) when (e.Message.Contains("TOPIC_CLOSED"))
        {
            _optoutChats.TryAdd(chatId, true);
            _logger.LogInformation("Topic closed, chat = {Chat}", chat.Title);
            return;
        }

        captchaInfo.ChatId = chatId;
        captchaInfo.ChatTitle = chat.Title;
        captchaInfo.Timestamp = DateTime.UtcNow;
        captchaInfo.CorrectAnswer = correctAnswer;

        return;
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
                try
                {
                    await Task.Delay(after, cancellationToken);
                    await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "DeleteMessage");
                }
            },
            cancellationToken
        );
    }

    private static string UserToKey(long chatId, User user) => $"{chatId}_{user.Id}";

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

using System.Collections.Concurrent;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static Telegram.Bot.Extensions.Markdown;

namespace ClubDoorman;

internal class CaptchaManager
{
    private sealed class CaptchaInfo
    {
        public long ChatId { get; set; }
        public string? ChatTitle { get; set; }
        public DateTime Timestamp { get; set; }
        public required User User { get; set; }
        public int CorrectAnswer { get; set; }
        public CancellationTokenSource Cts { get; } = new();
        public Message? UserJoinedMessage { get; set; }
    }

    private readonly ConcurrentDictionary<string, CaptchaInfo> _captchaNeededUsers = new();
    private readonly ITelegramBotClient _bot;
    private readonly UserManager _userManager;
    private readonly StatisticsReporter _statistics;
    private readonly Config _config;
    private readonly ILogger<CaptchaManager> _logger;

    private readonly List<string> _namesBlacklist = ["p0rn", "porn", "порн", "п0рн", "pоrn", "пoрн", "ponr", "bot", "child", "chlid"];

    public CaptchaManager(
        ITelegramBotClient bot,
        UserManager userManager,
        StatisticsReporter statistics,
        Config config,
        ILogger<CaptchaManager> logger
    )
    {
        _bot = bot;
        _userManager = userManager;
        _statistics = statistics;
        _config = config;
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
            if (info.UserJoinedMessage != null)
                await _bot.DeleteMessage(chat, info.UserJoinedMessage.MessageId);
            UnbanUserLater(chat, userId);
        }
    }

    public async ValueTask IntroFlow(Message? userJoinMessage, User user, Chat? chat = default)
    {
        if (_userManager.Approved(user.Id))
            return;
        var clubUser = await _userManager.GetClubUsername(user.Id);
        if (clubUser != null)
            return;

        chat = userJoinMessage?.Chat ?? chat;
        Debug.Assert(chat != null);
        var chatId = chat.Id;

        if (await BanIfBlacklisted(user, userJoinMessage?.Chat ?? chat))
            return;

        var key = UserToKey(chatId, user);

        var justAdded = _captchaNeededUsers.TryAdd(key, new CaptchaInfo() { Timestamp = DateTime.MaxValue, User = user });
        var captchaInfo = _captchaNeededUsers[key];
        if (!justAdded)
        {
            _logger.LogDebug("This user is already awaiting captcha challenge");
            if (userJoinMessage != null)
            {
                captchaInfo.UserJoinedMessage = userJoinMessage;
                DeleteMessageLater(userJoinMessage, TimeSpan.FromSeconds(45), captchaInfo.Cts.Token);
            }
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

        ReplyParameters? replyParams = null;
        if (userJoinMessage != null)
            replyParams = userJoinMessage;

        var fullName = Utils.FullName(user);
        var fullNameLower = fullName.ToLowerInvariant();
        var usernameLower = user.Username?.ToLower();
        if (_namesBlacklist.Any(fullNameLower.Contains) || (usernameLower != null && _namesBlacklist.Any(x => x == usernameLower)))
            fullName = "новый участник чата";

        var del = await _bot.SendMessage(
            chatId,
            $"Привет, [{Escape(fullName)}](tg://user?id={user.Id})! Антиспам: на какой кнопке {Captcha.CaptchaList[correctAnswer].Description}?",
            parseMode: ParseMode.Markdown,
            replyParameters: replyParams,
            replyMarkup: new InlineKeyboardMarkup(keyboard)
        );

        DeleteMessageLater(del, TimeSpan.FromSeconds(45), captchaInfo.Cts.Token);
        captchaInfo.ChatId = chatId;
        captchaInfo.ChatTitle = chat.Title;
        captchaInfo.Timestamp = DateTime.UtcNow;
        captchaInfo.CorrectAnswer = correctAnswer;
        if (userJoinMessage != null)
        {
            captchaInfo.UserJoinedMessage = userJoinMessage;
            DeleteMessageLater(userJoinMessage, TimeSpan.FromSeconds(45), captchaInfo.Cts.Token);
        }

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
}

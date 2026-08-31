using System.Collections.Frozen;
using System.Globalization;
using Microsoft.Extensions.Caching.Hybrid;
using Telegram.Bot;

namespace ClubDoorman;

internal class Config
{
    private readonly HybridCache _hybridCache;
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<Config> _logger;

    public Config(HybridCache hybridCache, ILogger<Config> logger)
    {
        _hybridCache = hybridCache;
        _bot = new TelegramBotClient(BotApi);
        _logger = logger;
        MultiAdminChatMap = new Dictionary<long, long>().ToFrozenDictionary();
        _ = InitMultiAdminChatMap();
        ChannelsCheckExclusionChats = GetChatsFromEnv("DOORMAN_CHANNEL_AUTOBAN_EXCLUSION");
        EmojiCheckDisabledChats = GetChatsFromEnv("DOORMAN_CHANNELS_EMOJI_DISABLE");
        MarketologsChats = GetChatsFromEnv("DOORMAN_CHANNEL_MARKETOLOGY_EXCLUSION");
        CaptchaDisabledChats = GetChatsFromEnv("DOORMAN_CAPTCHA_DISABLE");
        StatisticsFallbackAdminChats = GetChatsFromEnv("DOORMAN_STATISTICS_FALLBACK_ADMIN_CHATS");
    }

    public bool BlacklistAutoBan { get; } = !GetEnvironmentBool("DOORMAN_BLACKLIST_AUTOBAN_DISABLE");
    public bool ChannelAutoBan { get; } = !GetEnvironmentBool("DOORMAN_CHANNELS_AUTOBAN_DISABLE");
    public bool LookAlikeAutoBan { get; } = !GetEnvironmentBool("DOORMAN_LOOKALIKE_AUTOBAN_DISABLE");
    public bool LowConfidenceHamForward { get; } = GetEnvironmentBool("DOORMAN_LOW_CONFIDENCE_HAM_ENABLE");
    public bool ButtonAutoBan { get; } = !GetEnvironmentBool("DOORMAN_BUTTON_AUTOBAN_DISABLE");
    public bool HighConfidenceAutoBan { get; } = !GetEnvironmentBool("DOORMAN_HIGH_CONFIDENCE_AUTOBAN_DISABLE");
    public bool ApprovedUsersMlSpamCheck { get; } = !GetEnvironmentBool("DOORMAN_APPROVED_ML_SPAM_CHECK_DISABLE");
    public string BotApi { get; } =
        Environment.GetEnvironmentVariable("DOORMAN_BOT_API") ?? throw new InvalidOperationException("DOORMAN_BOT_API variable not set");

    public string? OpenRouterApi { get; } = Environment.GetEnvironmentVariable("DOORMAN_OPENROUTER_API");

    public FreeLlmSettings? FreeLlm { get; } = FreeLlmSettings.FromEnv();

    public long AdminChatId { get; } =
        long.Parse(
            Environment.GetEnvironmentVariable("DOORMAN_ADMIN_CHAT")
                ?? throw new InvalidOperationException("DOORMAN_ADMIN_CHAT variable not set"),
            CultureInfo.InvariantCulture
        );

    public FrozenDictionary<long, long> MultiAdminChatMap { get; private set; }
    public FrozenSet<long> ChannelsCheckExclusionChats { get; }
    public FrozenSet<long> EmojiCheckDisabledChats { get; }
    public FrozenSet<long> MarketologsChats { get; }
    public FrozenSet<long> CaptchaDisabledChats { get; }
    public FrozenSet<long> StatisticsFallbackAdminChats { get; }

    public bool NonFreeChat(long chatId) => MultiAdminChatMap.Count == 0 || MultiAdminChatMap.ContainsKey(chatId);

    // free chats do not pay for the bot, so they do not get to spend tokens either
    public bool LlmEnabled(long chatId) => OpenRouterApi != null && NonFreeChat(chatId);

    // ...they get their own endpoint instead, if there is one: slow, dumb, and allowed to warn but never to ban
    public bool FreeLlmEnabled(long chatId) => FreeLlm != null && !NonFreeChat(chatId);

    private FrozenSet<long> GetChatsFromEnv(string env)
    {
        var chats = ParseChatIdSet(Environment.GetEnvironmentVariable(env));
        _logger.LogInformation("{Env} chats {@Chats}", env, chats);
        return chats;
    }

    internal static FrozenSet<long> ParseChatIdSet(string? items)
    {
        if (items == null)
            return FrozenSet<long>.Empty;

        var list = new List<long>();
        foreach (var ch in items.Split(','))
        {
            if (long.TryParse(ch.Trim(), out var group))
                list.Add(group);
        }

        return list.ToFrozenSet();
    }

    public long GetAdminChat(long chatId) => MultiAdminChatMap.TryGetValue(chatId, out var adm) ? adm : AdminChatId;

    private async Task InitMultiAdminChatMap()
    {
        var map = new Dictionary<long, long>();
        var items = Environment.GetEnvironmentVariable("DOORMAN_ADMIN_CHAT_MAP");
        if (items != null)
        {
            foreach (var pair in items.Split(','))
            {
                var split = pair.Split('=').ToList();
                if (split.Count > 1 && long.TryParse(split[0].Trim(), out var from) && long.TryParse(split[1].Trim(), out var to))
                {
                    map.TryAdd(from, to);
                    try
                    {
                        var toChat = await GetChat(to);
                        var fromChat = await GetChat(from);
                        _logger.LogInformation(
                            "Messages from chat {FromId} {FromTitle} are going to admin chat {ToId} {ToTitle}",
                            fromChat.Id,
                            fromChat.Title,
                            toChat.Id,
                            toChat.Title
                        );
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Cannot get chat");
                    }
                }
            }
        }
        MultiAdminChatMap = map.ToFrozenDictionary();
    }

    private async Task<ChatInfo> GetChat(long id)
    {
        return await _hybridCache.GetOrCreateAsync(
            $"full_chan:{id}",
            async ct =>
            {
                using var logScope = _logger.BeginScope("Get chat {ChId}", id);
                try
                {
                    var chat = await _bot.GetChat(id, cancellationToken: ct);
                    return new ChatInfo(id, chat.Title);
                }
                catch (Exception e)
                {
                    _logger.LogDebug(e, "GetChat");
                }
                return new ChatInfo(id, "FAIL");
            },
            new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromMinutes(5) }
        );
    }

    public string? ClubServiceToken { get; } = Environment.GetEnvironmentVariable("DOORMAN_CLUB_SERVICE_TOKEN");
    public string ClubUrl { get; } = GetClubUrlOrDefault();

    private static bool GetEnvironmentBool(string envName)
    {
        var env = Environment.GetEnvironmentVariable(envName);
        if (env == null)
            return false;
        if (int.TryParse(env, out var num) && num == 1)
            return true;
        if (bool.TryParse(env, out var b) && b)
            return true;
        return false;
    }

    private static string GetClubUrlOrDefault()
    {
        var url = Environment.GetEnvironmentVariable("DOORMAN_CLUB_URL");
        if (url == null)
            return "https://vas3k.club/";
        if (!url.EndsWith('/'))
            url += '/';
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            throw new InvalidOperationException("DOORMAN_CLUB_URL variable is set to invalid URL");
        return url;
    }

    internal sealed record ChatInfo(long Id, string? Title);

    /// <summary>OpenAI compatible endpoint used for the free chats. All three parts are required or there is no endpoint at all.</summary>
    internal sealed record FreeLlmSettings(Uri BaseUrl, string ApiKey, string Model)
    {
        public static FreeLlmSettings? FromEnv()
        {
            var url = Environment.GetEnvironmentVariable("DOORMAN_FREE_LLM_URL");
            if (url == null)
                return null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var baseUrl))
                throw new InvalidOperationException("DOORMAN_FREE_LLM_URL variable is set to invalid URL");
            var model = Environment.GetEnvironmentVariable("DOORMAN_FREE_LLM_MODEL");
            if (string.IsNullOrWhiteSpace(model))
                throw new InvalidOperationException("DOORMAN_FREE_LLM_URL is set, but DOORMAN_FREE_LLM_MODEL is not");
            return new FreeLlmSettings(baseUrl, Environment.GetEnvironmentVariable("DOORMAN_FREE_LLM_API") ?? "", model);
        }
    }
}

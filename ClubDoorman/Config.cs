using System.Collections.Frozen;
using Serilog;

namespace ClubDoorman;

internal static class Config
{
    public static bool BlacklistAutoBan { get; } = !GetEnvironmentBool("DOORMAN_BLACKLIST_AUTOBAN_DISABLE");
    public static bool ChannelAutoBan { get; } = !GetEnvironmentBool("DOORMAN_CHANNELS_AUTOBAN_DISABLE");
    public static bool LookAlikeAutoBan { get; } = !GetEnvironmentBool("DOORMAN_LOOKALIKE_AUTOBAN_DISABLE");
    public static bool LowConfidenceHamForward { get; } = GetEnvironmentBool("DOORMAN_LOW_CONFIDENCE_HAM_ENABLE");
    public static bool ApproveButtonEnabled { get; } = GetEnvironmentBool("DOORMAN_APPROVE_BUTTON");
    public static bool ButtonAutoBan { get; } = !GetEnvironmentBool("DOORMAN_BUTTON_AUTOBAN_DISABLE");
    public static string BotApi { get; } =
        Environment.GetEnvironmentVariable("DOORMAN_BOT_API") ?? throw new Exception("DOORMAN_BOT_API variable not set");

    public static string? OpenRouterApi { get; } = Environment.GetEnvironmentVariable("DOORMAN_OPENROUTER_API");

    public static long AdminChatId { get; } =
        long.Parse(Environment.GetEnvironmentVariable("DOORMAN_ADMIN_CHAT") ?? throw new Exception("DOORMAN_ADMIN_CHAT variable not set"));

    public static FrozenDictionary<long, long> MultiAdminChatMap { get; } = GetMultiAdminChatMap();

    public static long GetAdminChat(long chatId) => MultiAdminChatMap.TryGetValue(chatId, out var adm) ? adm : AdminChatId;

    private static FrozenDictionary<long, long> GetMultiAdminChatMap()
    {
        var map = new Dictionary<long, long>();
        var items = Environment.GetEnvironmentVariable("DOORMAN_ADMIN_CHAT_MAP");
        Log.Logger.Information("MultiAdminChatMap raw: {Raw}", items);
        if (items != null)
        {
            foreach (var pair in items.Split(','))
            {
                var split = pair.Split('=').ToList();
                if (split.Count > 1 && long.TryParse(split[0].Trim(), out var from) && long.TryParse(split[1].Trim(), out var to))
                    map.TryAdd(from, to);
            }
        }
        Log.Logger.Information("MultiAdminChatMap parsed: {@Chats}", map);
        return map.ToFrozenDictionary();
    }

    public static string? ClubServiceToken { get; } = Environment.GetEnvironmentVariable("DOORMAN_CLUB_SERVICE_TOKEN");
    public static string ClubUrl { get; } = GetClubUrlOrDefault();

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
            throw new Exception("DOORMAN_CLUB_URL variable is set to invalid URL");
        return url;
    }
}

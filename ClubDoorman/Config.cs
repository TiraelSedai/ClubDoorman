namespace ClubDoorman
{
    public static class Config
    {
        public static bool BlacklistAutoBan { get; } = GetBlacklistAutoBan();
        public static string BotApi { get; } =
            Environment.GetEnvironmentVariable("DOORMAN_BOT_API") ?? throw new Exception("DOORMAN_BOT_API variable not set");
        public static long AdminChatId { get; } =
            long.Parse(
                Environment.GetEnvironmentVariable("DOORMAN_ADMIN_CHAT") ?? throw new Exception("DOORMAN_ADMIN_CHAT variable not set")
            );
        public static string? ClubServiceToken { get; } = Environment.GetEnvironmentVariable("DOORMAN_CLUB_SERVICE_TOKEN");
        public static string ClubUrl { get; } = GetClubUrlOrDefault();

        private static bool GetBlacklistAutoBan()
        {
            var env = Environment.GetEnvironmentVariable("DOORMAN_BLACKLIST_AUTOBAN_DISABLE");
            if (env == null)
                return true;
            if (int.TryParse(env, out var num) && num == 1)
                return false;
            if (bool.TryParse(env, out var b) && b)
                return false;
            return true;
        }

        private static string GetClubUrlOrDefault()
        {
            var url = Environment.GetEnvironmentVariable("DOORMAN_CLUB_URL");
            if (url == null)
                return "https://vas3k.club/";
            if (!url.EndsWith('/'))
                url = url + '/';
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                throw new Exception("DOORMAN_CLUB_URL variable is set to invalid URL");
            return url;
        }
    }
}

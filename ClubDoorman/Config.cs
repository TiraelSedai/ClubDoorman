using System.Text.Encodings.Web;

namespace ClubDoorman
{
    internal static class Config
    {
        public static bool BlacklistAutoBan { get; } = !GetEnvironmentBool("DOORMAN_BLACKLIST_AUTOBAN_DISABLE");
        public static bool ChannelAutoBan { get; } = !GetEnvironmentBool("DOORMAN_CHANNELS_AUTOBAN_DISABLE");
        public static bool LookAlikeAutoBan { get; } = !GetEnvironmentBool("DOORMAN_LOOKALIKE_AUTOBAN_DISABLE");
        public static bool LowConfidenceHamForward { get; } = GetEnvironmentBool("DOORMAN_LOW_CONFIDENCE_HAM_ENABLE");
        public static bool ApproveButtonEnabled { get; } = GetEnvironmentBool("DOORMAN_APPROVE_BUTTON");
        public static string BotApi { get; } =
            Environment.GetEnvironmentVariable("DOORMAN_BOT_API") ?? throw new Exception("DOORMAN_BOT_API variable not set");
        public static long AdminChatId { get; } =
            long.Parse(
                Environment.GetEnvironmentVariable("DOORMAN_ADMIN_CHAT") ?? throw new Exception("DOORMAN_ADMIN_CHAT variable not set")
            );
        public static string? ClubServiceToken { get; } = Environment.GetEnvironmentVariable("DOORMAN_CLUB_SERVICE_TOKEN");
        public static string ClubUrl { get; } = GetClubUrlOrDefault();
        public static HashSet<long> DisabledChats { get; } =
            (Environment.GetEnvironmentVariable("DOORMAN_DISABLED_CHATS") ?? "")
            .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
            .Select(x => long.Parse(x.Trim()))
            .ToHashSet();

        // Whitelist групп - если указан, бот работает только в этих группах
        public static HashSet<long> WhitelistChats { get; } =
            (Environment.GetEnvironmentVariable("DOORMAN_WHITELIST") ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => long.TryParse(x.Trim(), out var id) ? id : (long?)null)
            .Where(x => x.HasValue)
            .Select(x => x.Value)
            .ToHashSet();

        // Проверяет, разрешен ли бот в данном чате
        public static bool IsChatAllowed(long chatId)
        {
            // Если whitelist пуст - разрешены все чаты (кроме disabled)
            // Если whitelist не пуст - разрешены только чаты из whitelist
            return WhitelistChats.Count == 0 || WhitelistChats.Contains(chatId);
        }

        // Проверяет, разрешена ли команда /start в личке
        public static bool IsPrivateStartAllowed()
        {
            // Если whitelist не пуст - команда /start в личке отключена
            return WhitelistChats.Count == 0;
        }

        // AI проверки профилей
        public static string? OpenRouterApi { get; } = Environment.GetEnvironmentVariable("DOORMAN_OPENROUTER_API");
        public static bool ButtonAutoBan { get; } = !GetEnvironmentBool("DOORMAN_BUTTON_AUTOBAN_DISABLE");
        public static bool HighConfidenceAutoBan { get; } = !GetEnvironmentBool("DOORMAN_HIGH_CONFIDENCE_AUTOBAN_DISABLE");
        
        // Чаты для которых включены AI проверки профилей (если не указано - для всех)
        public static HashSet<long> AiEnabledChats { get; } = GetAiEnabledChats();

        private static HashSet<long> GetAiEnabledChats()
        {
            var chatsStr = Environment.GetEnvironmentVariable("DOORMAN_AI_ENABLED_CHATS");
            if (string.IsNullOrEmpty(chatsStr))
                return new HashSet<long>(); // Пустой = для всех чатов
            
            return chatsStr
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => long.TryParse(x.Trim(), out var id) ? id : (long?)null)
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .ToHashSet();
        }

        // Проверяет, включены ли AI проверки для данного чата
        public static bool IsAiEnabledForChat(long chatId)
        {
            // Если список пуст - AI включен для всех чатов
            // Если список не пуст - AI включен только для указанных чатов
            return AiEnabledChats.Count == 0 || AiEnabledChats.Contains(chatId);
        }

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

    public static class ChatSettingsManager
    {
        private static readonly string SettingsPath = Path.Combine("data", "chat_settings.json");
        private static DateTime _lastReadTime = DateTime.MinValue;
        private static Dictionary<long, Dictionary<string, string>> _chatSettings = new();
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

        public static string GetChatType(long chatId)
        {
            ReloadIfNeeded();
            return _chatSettings.TryGetValue(chatId, out var dict) && dict.TryGetValue("type", out var type) ? type : "default";
        }

        public static string? GetChatTitle(long chatId)
        {
            ReloadIfNeeded();
            return _chatSettings.TryGetValue(chatId, out var dict) && dict.TryGetValue("title", out var title) ? title : null;
        }

        public static void EnsureChatInConfig(long chatId, string? chatTitle)
        {
            ReloadIfNeeded();
            var changed = false;
            if (!_chatSettings.ContainsKey(chatId))
            {
                _chatSettings[chatId] = new Dictionary<string, string> { { "type", "default" }, { "title", chatTitle ?? "" } };
                changed = true;
            }
            else if (!string.IsNullOrEmpty(chatTitle) && _chatSettings[chatId].GetValueOrDefault("title") != chatTitle)
            {
                _chatSettings[chatId]["title"] = chatTitle;
                changed = true;
            }
            if (changed)
            {
                try
                {
                    var dict = _chatSettings.ToDictionary(
                        kv => kv.Key.ToString(),
                        kv => kv.Value
                    );
                    var json = System.Text.Json.JsonSerializer.Serialize(
                        dict,
                        new System.Text.Json.JsonSerializerOptions {
                            WriteIndented = true,
                            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        }
                    );
                    File.WriteAllText(SettingsPath, json);
                }
                catch { /* ignore */ }
            }
        }

        private static void ReloadIfNeeded()
        {
            if ((DateTime.UtcNow - _lastReadTime) < CacheDuration)
                return;
            _lastReadTime = DateTime.UtcNow;
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    _chatSettings = new();
                    return;
                }
                var json = File.ReadAllText(SettingsPath);
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
                _chatSettings = dict?.ToDictionary(
                    kv => long.Parse(kv.Key),
                    kv => kv.Value
                ) ?? new();
            }
            catch
            {
                _chatSettings = new();
            }
        }

        public static void InitConfigFileIfMissing()
        {
            if (!File.Exists(SettingsPath))
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                    File.WriteAllText(SettingsPath, "{}\n");
                }
                catch { /* ignore */ }
            }
        }
    }
}

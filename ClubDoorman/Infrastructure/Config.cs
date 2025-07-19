using System.Text.Encodings.Web;

namespace ClubDoorman.Infrastructure
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
        
        // Чат для логирования спама (если не указан - используется AdminChatId)
        public static long LogAdminChatId { get; } = GetLogAdminChatId();
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

        // Группы, где не показывать рекламу
        public static HashSet<long> NoVpnAdGroups { get; } =
            (Environment.GetEnvironmentVariable("NO_VPN_AD_GROUPS") ?? "")
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

        // ==== НАСТРОЙКИ АВТООДОБРЕНИЯ ====
        
        /// <summary>
        /// Режим автоодобрения пользователей:
        /// - true: глобальный режим (3 сообщения в любых группах → одобрение во всех группах)
        /// - false: групповой режим (3 сообщения в каждой группе → одобрение только в этой группе)
        /// </summary>
        public static bool GlobalApprovalMode { get; } = !GetEnvironmentBool("DOORMAN_GROUP_APPROVAL_MODE");

        /// <summary>
        /// Использовать новую систему одобрения (ApprovedUsersStorageV2)
        /// - true: использовать новую систему с поддержкой глобальных и групповых одобрений
        /// - false: использовать старую систему (ApprovedUsersStorage)
        /// </summary>
        public static bool UseNewApprovalSystem { get; } = GetEnvironmentBool("DOORMAN_USE_NEW_APPROVAL_SYSTEM");

        // ==== НАСТРОЙКИ СИСТЕМЫ ПОДОЗРИТЕЛЬНЫХ ПОЛЬЗОВАТЕЛЕЙ ====

        /// <summary>
        /// Включить систему детекции подозрительных пользователей
        /// - true: анализировать первые 3 сообщения на предмет мимикрии
        /// - false: использовать только основную систему одобрения
        /// </summary>
        public static bool SuspiciousDetectionEnabled { get; } = GetEnvironmentBool("DOORMAN_SUSPICIOUS_DETECTION_ENABLE");

        /// <summary>
        /// Порог для определения подозрительности (0.0 - 1.0)
        /// Если ML анализ превышает этот порог, пользователь помечается как подозрительный
        /// </summary>
        public static double MimicryThreshold { get; } = GetEnvironmentDouble("DOORMAN_MIMICRY_THRESHOLD", 0.7);

        /// <summary>
        /// Количество сообщений для перехода из подозрительных в одобренные
        /// </summary>
        public static int SuspiciousToApprovedMessageCount { get; } = GetEnvironmentInt("DOORMAN_SUSPICIOUS_TO_APPROVED_COUNT", 3);

        // ==== НАСТРОЙКИ ФИЛЬТРАЦИИ МЕДИА ====

        /// <summary>
        /// Отключить фильтрацию картинок/видео/документов глобально
        /// - true: фильтрация медиа отключена во всех группах
        /// - false: фильтрация медиа работает (по умолчанию)
        /// </summary>
        public static bool DisableMediaFiltering { get; } = GetEnvironmentBool("DOORMAN_DISABLE_MEDIA_FILTERING");

        /// <summary>
        /// Группы где фильтрация медиа отключена (независимо от глобальной настройки)
        /// </summary>
        public static HashSet<long> MediaFilteringDisabledChats { get; } = GetMediaFilteringDisabledChats();

        private static HashSet<long> GetMediaFilteringDisabledChats()
        {
            var chatsStr = Environment.GetEnvironmentVariable("DOORMAN_MEDIA_FILTERING_DISABLED_CHATS");
            if (string.IsNullOrEmpty(chatsStr))
                return new HashSet<long>();
            
            return chatsStr
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => long.TryParse(x.Trim(), out var id) ? id : (long?)null)
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .ToHashSet();
        }

        /// <summary>
        /// Проверяет, отключена ли фильтрация медиа для данного чата
        /// </summary>
        public static bool IsMediaFilteringDisabledForChat(long chatId)
        {
            // Если глобально отключено - фильтрация отключена везде
            if (DisableMediaFiltering)
                return true;

            // Если чат в списке исключений - фильтрация отключена
            return MediaFilteringDisabledChats.Contains(chatId);
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

        private static int GetEnvironmentInt(string envName, int defaultValue)
        {
            var env = Environment.GetEnvironmentVariable(envName);
            if (env == null)
                return defaultValue;
            if (int.TryParse(env, out var result))
                return result;
            return defaultValue;
        }

        private static double GetEnvironmentDouble(string envName, double defaultValue)
        {
            var env = Environment.GetEnvironmentVariable(envName);
            if (env == null)
                return defaultValue;
            if (double.TryParse(env, out var num))
                return num;
            return defaultValue;
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

        private static long GetLogAdminChatId()
        {
            var logChatVar = Environment.GetEnvironmentVariable("DOORMAN_LOG_ADMIN_CHAT");
            if (string.IsNullOrEmpty(logChatVar))
                return AdminChatId; // Если не указан, используем основной админский чат
            
            if (long.TryParse(logChatVar, out var logChatId))
                return logChatId;
            
            throw new Exception("DOORMAN_LOG_ADMIN_CHAT variable is set to invalid chat ID");
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

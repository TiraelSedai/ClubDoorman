using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Json;
using ClubDoorman.Infrastructure;

namespace ClubDoorman.Services;

internal sealed class UserManagerV2 : IUserManager
{
    private readonly ILogger<UserManagerV2> _logger;
    private readonly ApprovedUsersStorageV2 _approvedUsersStorage;
    private readonly ConcurrentDictionary<long, byte> _banlist = [];
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly HttpClient _clubHttpClient = new();
    private readonly HttpClient _httpClient = new();
    
    // Тестовый блэклист из переменной окружения DOORMAN_TEST_BLACKLIST_IDS
    private static readonly HashSet<long> _testBlacklist = LoadTestBlacklist();

    public UserManagerV2(ILogger<UserManagerV2> logger, ApprovedUsersStorageV2 approvedUsersStorage)
    {
        _logger = logger;
        _approvedUsersStorage = approvedUsersStorage;
        
        // Логируем состояние тестового блэклиста при создании UserManagerV2
        Console.WriteLine($"[DEBUG] UserManagerV2 создан: тестовый блэклист содержит {_testBlacklist.Count} ID(s): [{string.Join(", ", _testBlacklist)}]");
        
        if (Config.ClubServiceToken == null)
            _logger.LogWarning("DOORMAN_CLUB_SERVICE_TOKEN variable is not set, additional club checks disabled");
        else
            _clubHttpClient.DefaultRequestHeaders.Add("X-Service-Token", Config.ClubServiceToken);
    }

    public async Task RefreshBanlist()
    {
        try
        {
            await _semaphore.WaitAsync();
            try
            {
                var httpClient = new HttpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var banlist = await httpClient.GetFromJsonAsync<long[]>("https://lols.bot/spam/banlist.json", cts.Token);
                
                if (banlist != null && banlist.Length > 0)
                {
                    var oldCount = _banlist.Count;
                    
                    // Очищаем текущий список
                    foreach (var key in _banlist.Keys.ToArray())
                        _banlist.TryRemove(key, out _);
                    
                    // Заполняем его новыми значениями
                    foreach (var id in banlist)
                        _banlist.TryAdd(id, 0);
                    
                    _logger.LogInformation("Обновлен банлист из lols.bot: было {OldCount}, стало {NewCount} записей", oldCount, _banlist.Count);
                }
                else
                {
                    _logger.LogWarning("Получен пустой банлист от lols.bot или ответ был null");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось обновить банлист из lols.bot");
        }
    }

    /// <summary>
    /// Проверяет, одобрен ли пользователь
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы (для проверки группового одобрения)</param>
    /// <returns>true, если пользователь одобрен</returns>
    public bool Approved(long userId, long? groupId = null)
    {
        return _approvedUsersStorage.IsApproved(userId, groupId);
    }

    /// <summary>
    /// Одобряет пользователя в зависимости от настроек
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы (для группового одобрения)</param>
    public async ValueTask Approve(long userId, long? groupId = null)
    {
        if (Config.GlobalApprovalMode)
        {
            // Глобальный режим: одобряем глобально
            _approvedUsersStorage.ApproveUserGlobally(userId);
        }
        else
        {
            // Групповой режим: одобряем в конкретной группе
            if (groupId.HasValue)
            {
                _approvedUsersStorage.ApproveUserInGroup(userId, groupId.Value);
            }
            else
            {
                // Если группа не указана, одобряем глобально как fallback
                _approvedUsersStorage.ApproveUserGlobally(userId);
            }
        }
    }

    /// <summary>
    /// Удаляет одобрение пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы (для удаления группового одобрения)</param>
    /// <param name="removeAll">Удалить все одобрения пользователя</param>
    /// <returns>true, если одобрение было удалено</returns>
    public bool RemoveApproval(long userId, long? groupId = null, bool removeAll = false)
    {
        try
        {
            if (removeAll)
            {
                return _approvedUsersStorage.RemoveAllApprovals(userId);
            }
            
            if (groupId.HasValue)
            {
                // Удаляем одобрение в конкретной группе
                return _approvedUsersStorage.RemoveGroupApproval(userId, groupId.Value);
            }
            else
            {
                // Удаляем глобальное одобрение
                return _approvedUsersStorage.RemoveGlobalApproval(userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось удалить одобрение пользователя {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Получает информацию об одобрении пользователя
    /// </summary>
    public (bool isGlobal, Dictionary<long, GroupApprovalInfo> groupApprovals) GetApprovalInfo(long userId)
    {
        var isGlobal = _approvedUsersStorage.IsGloballyApproved(userId);
        var groupApprovals = _approvedUsersStorage.GetUserGroupApprovals(userId);
        return (isGlobal, groupApprovals);
    }

    /// <summary>
    /// Получает статистику одобрений
    /// </summary>
    public (int globalCount, int groupCount, int totalGroupApprovals) GetApprovalStats()
    {
        return _approvedUsersStorage.GetApprovalStats();
    }

    public async ValueTask<bool> InBanlist(long userId)
    {
        Console.WriteLine($"[DEBUG] UserManagerV2.InBanlist: проверяем пользователя {userId} (тестовых ID: {_testBlacklist.Count})");
        _logger.LogDebug("InBanlist: проверяем пользователя {UserId} (тестовых ID: {TestCount})", userId, _testBlacklist.Count);
        
        // Сначала проверяем тестовый блэклист
        if (_testBlacklist.Contains(userId))
        {
            Console.WriteLine($"[DEBUG] 🎯 НАЙДЕН в тестовом блэклисте: {userId}");
            _logger.LogWarning("🎯 Пользователь {UserId} найден в ТЕСТОВОМ блэклисте", userId);
            return true;
        }
        
        if (_banlist.ContainsKey(userId))
            return true;
        try
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var result = await _httpClient.GetFromJsonAsync<LolsBotApiResponse>($"https://api.lols.bot/account?id={userId}", cts.Token);
            if (!result!.banned)
                return false;

            _banlist.TryAdd(userId, 0);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "LolsBotApi exception");
            return false;
        }
    }

    public async ValueTask<string?> GetClubUsername(long userId)
    {
        if (Config.ClubServiceToken == null)
            return null;
        var url = $"{Config.ClubUrl}user/by_telegram_id/{userId}.json";
        try
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var get = await _clubHttpClient.GetAsync(url, cts.Token);
            var response = await get.Content.ReadFromJsonAsync<ClubByTgIdResponse>(cancellationToken: cts.Token);
            var fullName = response?.user?.full_name;
            if (!string.IsNullOrEmpty(fullName))
                await Approve(userId); // Одобряем глобально, так как это клубный пользователь
            return fullName;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "GetClubUsername");
            return null;
        }
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable IDE1006 // Naming Styles

    private record LolsBotApiResponse(long user_id, int? offenses, bool banned, bool ok, string? when, float? spam_factor, bool? scammer);

    internal class ClubByTgIdResponse
    {
        public Error? error { get; set; }
        public User? user { get; set; }
    }

    internal class Error
    {
        public string code { get; set; }
        public Data data { get; set; }
        public object message { get; set; }
        public string title { get; set; }
    }

    internal class Data { }

    internal class User
    {
        public string avatar { get; set; }
        public string bio { get; set; }
        public string city { get; set; }
        public string company { get; set; }
        public string country { get; set; }
        public DateTime created_at { get; set; }
        public string? full_name { get; set; }
        public string id { get; set; }
        public bool is_active_member { get; set; }
        public DateTime membership_expires_at { get; set; }
        public DateTime membership_started_at { get; set; }
        public string moderation_status { get; set; }
        public string payment_status { get; set; }
        public string position { get; set; }
        public string slug { get; set; }
        public int upvotes { get; set; }
    }

    /// <summary>
    /// Загружает тестовый блэклист из переменной окружения DOORMAN_TEST_BLACKLIST_IDS
    /// Формат: "123456,789012,345678" (ID через запятую)
    /// </summary>
    private static HashSet<long> LoadTestBlacklist()
    {
        var testIds = Environment.GetEnvironmentVariable("DOORMAN_TEST_BLACKLIST_IDS");
        if (string.IsNullOrWhiteSpace(testIds))
        {
            Console.WriteLine("[DEBUG] DOORMAN_TEST_BLACKLIST_IDS не задана - тестовый блэклист пустой");
            return [];
        }

        var result = new HashSet<long>();
        var ids = testIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var idStr in ids)
        {
            if (long.TryParse(idStr.Trim(), out var id))
            {
                result.Add(id);
            }
            else
            {
                Console.WriteLine($"[WARNING] Некорректный ID в DOORMAN_TEST_BLACKLIST_IDS: '{idStr}'");
            }
        }
        
        Console.WriteLine($"[DEBUG] Загружен тестовый блэклист: {result.Count} ID(s) [{string.Join(", ", result)}]");
        return result;
    }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore IDE1006 // Naming Styles
} 
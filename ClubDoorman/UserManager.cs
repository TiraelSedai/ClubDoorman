using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Json;

namespace ClubDoorman;

internal sealed class UserManager
{
    private readonly ILogger<UserManager> _logger;
    private readonly ApprovedUsersStorage _approvedUsersStorage;

    private async Task Init()
    {
        await RefreshBanlist();
    }

    public async Task RefreshBanlist()
    {
        try
        {
            await _semaphore.WaitAsync();
            try
            {
                var httpClient = new HttpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)); // Таймаут 15 сек
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

    public UserManager(ILogger<UserManager> logger, ApprovedUsersStorage approvedUsersStorage)
    {
        _logger = logger;
        _approvedUsersStorage = approvedUsersStorage;
        Task.Run(Init);
        if (Config.ClubServiceToken == null)
            _logger.LogWarning("DOORMAN_CLUB_SERVICE_TOKEN variable is not set, additional club checks disabled");
        else
            _clubHttpClient.DefaultRequestHeaders.Add("X-Service-Token", Config.ClubServiceToken);
    }

    private const string Path = "data/approved-users.txt";
    private readonly ConcurrentDictionary<long, byte> _banlist = [];
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly HashSet<long> _approved = File.ReadAllLines(Path).Select(long.Parse).ToHashSet();
    private readonly HttpClient _clubHttpClient = new();
    private readonly HttpClient _httpClient = new();

    public bool Approved(long userId) => _approvedUsersStorage.IsApproved(userId);

    public async ValueTask Approve(long userId)
    {
        _approvedUsersStorage.ApproveUser(userId);
    }

    public bool RemoveApproval(long userId)
    {
        try
        {
            return _approvedUsersStorage.RemoveApproval(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось удалить пользователя {UserId} из списка одобренных", userId);
            return false;
        }
    }

    public async ValueTask<bool> InBanlist(long userId)
    {
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
            // cannot use _clubHttpClient.GetFromJsonAsync here because the response is not 200 OK at the time of writing for when user is not found
            var get = await _clubHttpClient.GetAsync(url, cts.Token);
            var response = await get.Content.ReadFromJsonAsync<ClubByTgIdResponse>(cancellationToken: cts.Token);
            var fullName = response?.user?.full_name;
            if (!string.IsNullOrEmpty(fullName))
                await Approve(userId);
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
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore IDE1006 // Naming Styles
}

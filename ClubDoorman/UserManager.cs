using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Json;

namespace ClubDoorman;

public class UserManager
{
    private readonly ILogger<UserManager> _logger;

    private async Task Init()
    {
        var httpClient = new HttpClient();
        var banlist = await httpClient.GetFromJsonAsync<long[]>("https://lols.bot/spam/banlist.json");
        if (banlist != null)
            foreach (var id in banlist)
                _banlist.TryAdd(id, 0);
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(15));
                try
                {
                    var banlistOneHour = await httpClient.GetFromJsonAsync<string[]>("https://lols.bot/spam/banlist-1h.json") ?? [];
                    foreach (var id in banlistOneHour.Select(long.Parse))
                        _banlist.TryAdd(id, 0);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Exception during banlist update");
                }
            }
        });
    }

    public UserManager(ILogger<UserManager> logger)
    {
        _logger = logger;
        Task.Run(Init);
        if (Config.ClubServiceToken == null)
            _logger.LogWarning("DOORMAN_CLUB_SERVICE_TOKEN variable is not set, additional club checks disabled");
        else
            _httpClient.DefaultRequestHeaders.Add("X-Service-Token", Config.ClubServiceToken);
    }

    private const string Path = "data/approved-users.txt";
    private readonly ConcurrentDictionary<long, byte> _banlist = [];
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly HashSet<long> _approved = File.ReadAllLines(Path).Select(long.Parse).ToHashSet();
    private readonly HttpClient _httpClient = new();

    public bool Approved(long userId) => _approved.Contains(userId);

    public async ValueTask Approve(long userId)
    {
        if (_approved.Add(userId))
        {
            using var token = await SemaphoreHelper.AwaitAsync(_semaphore);
            await File.AppendAllLinesAsync(Path, [userId.ToString(CultureInfo.InvariantCulture)]);
        }
    }

    public bool InBanlist(long userId) => _banlist.ContainsKey(userId);

    public async ValueTask<string?> GetClubUsername(long userId)
    {
        if (Config.ClubServiceToken == null)
            return null;
        var url = $"{Config.ClubUrl}user/by_telegram_id/{userId}.json";
        try
        {
            var get = await _httpClient.GetAsync(url);
            var resp = await get.Content.ReadFromJsonAsync<ClubByTgIdResponse>();
            return resp?.user?.full_name;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "GetClubUsername");
            return null;
        }
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable IDE1006 // Naming Styles
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

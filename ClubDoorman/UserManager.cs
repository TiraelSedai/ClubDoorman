﻿using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClubDoorman;

internal sealed class UserManager
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly HybridCache _cache;
    private readonly Config _config;
    private readonly ILogger<UserManager> _logger;

    public UserManager(IServiceScopeFactory serviceScopeFactory, HybridCache cache, Config config, ILogger<UserManager> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _cache = cache;
        _config = config;
        _logger = logger;
        Task.Run(InjestChatHistories);
        if (_config.ClubServiceToken == null)
            _logger.LogWarning("DOORMAN_CLUB_SERVICE_TOKEN variable is not set, additional club checks disabled");
        else
            _clubHttpClient.DefaultRequestHeaders.Add("X-Service-Token", _config.ClubServiceToken);
    }

    private const string Path = "data/approved-users.txt";
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly HashSet<long> _approved = [.. File.ReadAllLines(Path).Select(long.Parse)];
    private readonly HttpClient _clubHttpClient = new();
    private readonly HttpClient _httpClient = new();

    public bool Approved(long userId) => _approved.Contains(userId);

    private async Task InjestChatHistories()
    {
        var jsons = Directory.EnumerateFiles("data", "*.json");
        foreach (var json in jsons)
        {
            try
            {
                var users = await GetUsersWithAtLeastThreeMessagesAsync(json);
                if (users.Count > 0)
                {
                    await Approve(users);
                    _logger.LogInformation("Added {Count} new approved users", users.Count);
                }
                File.Delete(json);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, nameof(InjestChatHistories));
            }
        }
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(15));
            _ = InjestChatHistories();
        });
    }

    public async Task<List<long>> GetUsersWithAtLeastThreeMessagesAsync(string jsonFilePath)
    {
        await using var stream = File.OpenRead(jsonFilePath);
        using var document = await JsonDocument.ParseAsync(stream);
        if (document.RootElement.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
            _logger.LogInformation("Injesting messages from chat {Name}", name.GetString());
        if (!document.RootElement.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array)
            return [];

        var userMessageCounts = new Dictionary<long, int>();
        foreach (var message in messages.EnumerateArray())
        {
            if (!message.TryGetProperty("from_id", out var fromIdProp) || fromIdProp.ValueKind != JsonValueKind.String)
                continue;

            var fromIdStr = fromIdProp.GetString();
            if (fromIdStr == null || !fromIdStr.StartsWith("user") || !long.TryParse(fromIdStr.AsSpan(4), out var userId))
                continue;

            if (userMessageCounts.ContainsKey(userId))
                userMessageCounts[userId]++;
            else
                userMessageCounts[userId] = 1;
        }
        return userMessageCounts.Where(x => x.Value >= 3).Select(x => x.Key).ToList();
    }

    public async ValueTask Approve(long userId)
    {
        if (_approved.Add(userId))
        {
            using var token = await SemaphoreHelper.AwaitAsync(_semaphore);
            await File.AppendAllLinesAsync(Path, [userId.ToString(CultureInfo.InvariantCulture)]);
        }
    }

    private async ValueTask Approve(IEnumerable<long> userId)
    {
        var newGuys = new HashSet<long>();
        foreach (var id in userId)
            if (_approved.Add(id))
                newGuys.Add(id);
        var lines = newGuys.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList();
        using var token = await SemaphoreHelper.AwaitAsync(_semaphore);
        await File.AppendAllLinesAsync(Path, lines);
    }

    public async ValueTask<bool> InBanlist(long userId)
    {
        var key = $"user:banned:{userId}";
        var justCreated = false;
        var banned = await _cache.GetOrCreateAsync(
            key,
            _ =>
            {
                justCreated = true;
                return GetUserBanned(userId);
            },
            new HybridCacheEntryOptions() { LocalCacheExpiration = TimeSpan.FromSeconds(5) }
        );
        if (justCreated && banned)
            await _cache.SetAsync(key, true, new HybridCacheEntryOptions() { LocalCacheExpiration = TimeSpan.FromHours(3) });
        return banned;
    }

    public async Task Unban(long userId, CancellationToken ct = default)
    {
        var key = $"user:banned:{userId}";
        using var scope = _serviceScopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.BlacklistedUsers.Where(x => x.Id == userId).ExecuteDeleteAsync(cancellationToken: ct);
        await db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(key, ct);
    }

    private async ValueTask<bool> GetUserBanned(long userId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.BlacklistedUsers.Where(x => x.Id == userId).CountAsync();
        if (count > 0)
            return true;
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            var result = await _httpClient.GetFromJsonAsync<LolsBotApiResponse>(
                $"https://api.lols.bot/account?id={userId}&quick=true",
                cts.Token
            );
            if (!result!.banned)
                return false;
            db.Add(new BlacklistedUser { Id = userId });
            await db.SaveChangesAsync();
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("LolsBotApi timeout");
            return false;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "LolsBotApi exception");
            return false;
        }
    }

    public async ValueTask<string?> GetClubUsername(long userId)
    {
        if (_config.ClubServiceToken == null)
            return null;
        var url = $"{_config.ClubUrl}user/by_telegram_id/{userId}.json";
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
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetClubUsername timeout");
            return null;
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

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace ClubDoorman;

public sealed class SpamDeduplicationCache
{
    private static readonly TimeSpan Window = TimeSpan.FromDays(1);
    private readonly ConcurrentDictionary<string, List<DuplicateEntry>> _store = new();
    private readonly Lock _lock = new();

    public SpamDeduplicationCache()
    {
        _ = RunSweepAsync(CancellationToken.None);
    }

    public sealed record DuplicateEntry(
        long ChatId,
        string? ChatTitle,
        long MessageId,
        long UserId,
        string UserFirstName,
        string? UserLastName,
        DateTimeOffset CreatedAt
    );

    public static string ComputeHash(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash);
    }

    public List<DuplicateEntry> AddAndCheck(
        string text,
        long chatId,
        string? chatTitle,
        long messageId,
        long userId,
        string userFirstName,
        string? userLastName
    )
    {
        var key = ComputeHash(text);
        var now = DateTimeOffset.UtcNow;
        var entry = new DuplicateEntry(chatId, chatTitle, messageId, userId, userFirstName, userLastName, now);

        lock (_lock)
        {
            if (!_store.TryGetValue(key, out var list))
            {
                list = [];
                _store[key] = list;
            }

            var duplicates = list
                .Where(e => !(e.UserId == userId && e.ChatId == chatId))
                .ToList();

            list.Add(entry);
            return duplicates;
        }
    }

    public void Remove(string text)
    {
        var key = ComputeHash(text);
        lock (_lock)
        {
            _store.TryRemove(key, out _);
        }
    }

    private async Task RunSweepAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var cutoff = DateTimeOffset.UtcNow - Window;
            lock (_lock)
            {
                var keys = _store.Keys.ToList();
                foreach (var key in keys)
                {
                    if (!_store.TryGetValue(key, out var list))
                        continue;

                    list.RemoveAll(e => e.CreatedAt < cutoff);

                    if (list.Count == 0)
                        _store.TryRemove(key, out _);
                }
            }
        }
    }
}

using System.Collections.Concurrent;
using Telegram.Bot.Types;

namespace ClubDoorman;

public sealed class RecentMessagesStorage
{
    public RecentMessagesStorage()
    {
        _ = RunSweepAsync(CancellationToken.None);
    }

    private static readonly TimeSpan Window = TimeSpan.FromHours(24);
    private readonly ConcurrentDictionary<(long UserId, long ChatId), ConcurrentQueue<Message>> _store = new();

    public void Add(long userId, long chatId, Message msg)
    {
        var q = _store.GetOrAdd((userId, chatId), _ => new ConcurrentQueue<Message>());
        q.Enqueue(msg);
    }

    public IReadOnlyList<Message> Get(long userId, long chatId)
    {
        if (!_store.TryGetValue((userId, chatId), out var q))
            return [];
        return q.ToList();
    }

    private static void TrimOld(ConcurrentQueue<Message> q, DateTimeOffset cutoff)
    {
        while (q.TryPeek(out var head) && head.Date < cutoff)
            q.TryDequeue(out _);
    }

    public async Task RunSweepAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var cutoff = DateTimeOffset.UtcNow - Window;
            var keys = _store.Keys.ToList();
            foreach (var k in keys)
            {
                var ok = _store.TryGetValue(k, out var q);
                if (!ok)
                    continue;
                TrimOld(q!, cutoff);
                if (q!.IsEmpty)
                    _store.TryRemove(k, out _);
            }
        }
    }
}

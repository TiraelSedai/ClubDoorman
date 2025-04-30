using System.Collections.Concurrent;

namespace ClubDoorman;

internal sealed class BadMessageManager
{
    private readonly ConcurrentDictionary<string, bool> _badMessages = new();

    public bool KnownBadMessage(string message) => _badMessages.ContainsKey(message);

    public ValueTask MarkAsBad(string message)
    {
        if (!string.IsNullOrWhiteSpace(message) && message.Length > 30)
            _badMessages.TryAdd(message, true);

        return ValueTask.CompletedTask;
    }
}

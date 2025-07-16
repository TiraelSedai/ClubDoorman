using System.Security.Cryptography;
using System.Text;
using ClubDoorman.Infrastructure;

namespace ClubDoorman.Services;

public sealed class BadMessageManager
{
    private const string Path = "data/bad-messages.txt";
    private readonly SemaphoreSlim _fileLock = new(1);

    // with our data size, we would never need O(1), in fact if this ever bloats I'd rather limit this to ~2048 last messages
    // space savings are very moderate, base64 string takes up ~128 bytes while byte[] takes only 64, but then again it's
    // never more than a few kilobytes anyway, so this is pure 'for the sake of it' optimization
    private readonly SortedSet<byte[]> _bad = new(new ByteArrayComparer());

    public BadMessageManager()
    {
        lock (_bad)
        {
            foreach (var item in File.ReadAllLines(Path))
                _bad.Add(Convert.FromBase64String(item));
        }
    }

    public bool KnownBadMessage(string message)
    {
        lock (_bad)
            return _bad.Contains(ComputeHash(message));
    }

    public async ValueTask MarkAsBad(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        var hash = ComputeHash(message);
        bool added;
        lock (_bad)
            added = _bad.Add(hash);
        if (!added)
            return;
        using var token = await SemaphoreHelper.AwaitAsync(_fileLock);
        await File.AppendAllLinesAsync(Path, [Convert.ToBase64String(hash)]);
    }

    private static byte[] ComputeHash(string message) => SHA512.HashData(Encoding.UTF8.GetBytes(message));
}

internal sealed class ByteArrayComparer : IComparer<byte[]>
{
    public int Compare(byte[]? x, byte[]? y)
    {
        if (x == null || y == null)
            return x == y ? 0 : (x == null ? -1 : 1);
        for (var i = 0; i < x.Length; i++)
        {
            var comparison = x[i].CompareTo(y[i]);
            if (comparison != 0)
                return comparison;
        }
        return x.Length.CompareTo(y.Length);
    }
}

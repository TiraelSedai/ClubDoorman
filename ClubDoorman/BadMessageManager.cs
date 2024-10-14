using System.Security.Cryptography;
using System.Text;

namespace ClubDoorman;

public class BadMessageManager
{
    private const string Path = "data/bad-messages.txt";
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly HashSet<string> _bad = [.. File.ReadAllLines(Path)];

    public bool KnownBadMessage(string message) => _bad.Contains(ComputeHash(message));

    public async ValueTask MarkAsBad(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        var hash = ComputeHash(message);
        if (_bad.Add(hash))
        {
            using var token = await SemaphoreHelper.AwaitAsync(_semaphore);
            await File.AppendAllLinesAsync(Path, [hash]);
        }
    }

    private static string ComputeHash(string message) => Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(message)));
}

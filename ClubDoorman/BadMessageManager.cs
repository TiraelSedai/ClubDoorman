using System.Security.Cryptography;
using System.Text;

namespace ClubDoorman;

public class BadMessageManager
{
    private const string Path = "data/bad-messages.txt";
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly HashSet<string> _bad = File.ReadAllLines(Path).ToHashSet();

    public bool Bad(string message) => _bad.Contains(ComputeHash(message));

    public async ValueTask MarkAsBad(string message)
    {
		if string.IsNullOrEmpty(message)
		{
			return;
		}
        var hash = ComputeHash(message);
        if (_bad.Add(hash))
        {
            using var token = await SemaphoreHelper.AwaitAsync(_semaphore);
            await File.AppendAllLinesAsync(Path, [hash]);
        }
    }

    private static string ComputeHash(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        using (var hasher = SHA512.Create())
        {
            var hashBytes = hasher.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}

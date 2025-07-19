using System.Security.Cryptography;
using System.Text;
using ClubDoorman.Infrastructure;

namespace ClubDoorman.Services;

/// <summary>
/// Менеджер для хранения и проверки известных плохих сообщений (спам/оскорбления и т.д.).
/// Хранит хэши сообщений, поддерживает потокобезопасность и асинхронную запись.
/// </summary>
public sealed class BadMessageManager
{
    private const string Path = "data/bad-messages.txt";
    private readonly SemaphoreSlim _fileLock = new(1);

    // Сортированное множество хэшей плохих сообщений
    private readonly SortedSet<byte[]> _bad = new(new ByteArrayComparer());

    /// <summary>
    /// Загружает известные плохие сообщения из файла при инициализации.
    /// </summary>
    public BadMessageManager()
    {
        lock (_bad)
        {
            try
            {
                if (File.Exists(Path))
                {
                    foreach (var item in File.ReadAllLines(Path))
                        _bad.Add(Convert.FromBase64String(item));
                }
            }
            catch (Exception ex)
            {
                // Не ломаем логику, просто логируем ошибку
                Console.WriteLine($"[BadMessageManager] Ошибка при загрузке файла: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Проверяет, является ли сообщение известным плохим (по хэшу).
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    /// <returns>true, если сообщение известно как плохое</returns>
    public bool KnownBadMessage(string message)
    {
        lock (_bad)
            return _bad.Contains(ComputeHash(message));
    }

    /// <summary>
    /// Добавляет сообщение в список плохих (если оно не пустое и не было добавлено ранее).
    /// </summary>
    /// <param name="message">Текст сообщения</param>
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
        try
        {
            using var token = await SemaphoreHelper.AwaitAsync(_fileLock);
            await File.AppendAllLinesAsync(Path, [Convert.ToBase64String(hash)]);
        }
        catch (Exception ex)
        {
            // Не ломаем логику, просто логируем ошибку
            Console.WriteLine($"[BadMessageManager] Ошибка при записи файла: {ex.Message}");
        }
    }

    /// <summary>
    /// Вычисляет SHA512-хэш сообщения.
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    /// <returns>Байтовый массив хэша</returns>
    private static byte[] ComputeHash(string message) => SHA512.HashData(Encoding.UTF8.GetBytes(message));
}

/// <summary>
/// Компаратор для сравнения байтовых массивов (хэшей сообщений).
/// </summary>
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

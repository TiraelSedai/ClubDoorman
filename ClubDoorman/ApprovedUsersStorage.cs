using System.Collections.Concurrent;
using System.Text.Json;

namespace ClubDoorman;

public class ApprovedUsersStorage
{
    private readonly string _filePath;
    private readonly ConcurrentDictionary<long, DateTime> _approvedUsers;
    private readonly ILogger<ApprovedUsersStorage> _logger;

    public ApprovedUsersStorage(ILogger<ApprovedUsersStorage> logger)
    {
        _logger = logger;
        _filePath = Path.Combine("data", "approved_users.json");
        _approvedUsers = LoadFromFile();
    }

    private ConcurrentDictionary<long, DateTime> LoadFromFile()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new ConcurrentDictionary<long, DateTime>();

            var json = File.ReadAllText(_filePath);
            var dict = JsonSerializer.Deserialize<Dictionary<long, DateTime>>(json);
            return new ConcurrentDictionary<long, DateTime>(dict ?? new Dictionary<long, DateTime>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке списка одобренных пользователей");
            return new ConcurrentDictionary<long, DateTime>();
        }
    }

    private void SaveToFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(_approvedUsers);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении списка одобренных пользователей");
        }
    }

    public bool IsApproved(long userId)
    {
        return _approvedUsers.ContainsKey(userId);
    }

    public void ApproveUser(long userId)
    {
        _approvedUsers.TryAdd(userId, DateTime.UtcNow);
        SaveToFile();
    }

    public void RemoveApproval(long userId)
    {
        if (_approvedUsers.TryRemove(userId, out _))
            SaveToFile();
    }
} 
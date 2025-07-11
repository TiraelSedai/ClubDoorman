using System.Collections.Concurrent;
using System.Text.Json;

namespace ClubDoorman;

// Структура для хранения информации об одобренном пользователе в группе
public record GroupApprovalInfo(DateTime ApprovedAt);

public class ApprovedUsersStorageV2
{
    private readonly string _globalFilePath;
    private readonly string _groupsFilePath;
    private readonly ILogger<ApprovedUsersStorageV2> _logger;
    private readonly object _lock = new object();
    private HashSet<long> _globalApprovedUsers;
    private Dictionary<long, Dictionary<long, GroupApprovalInfo>> _groupApprovedUsers;

    public ApprovedUsersStorageV2(ILogger<ApprovedUsersStorageV2> logger)
    {
        _logger = logger;
        _globalFilePath = Path.Combine("data", "approved_users.json");
        _groupsFilePath = Path.Combine("data", "approved_users_groups.json");
        
        _globalApprovedUsers = LoadGlobalFromFile();
        _groupApprovedUsers = LoadGroupsFromFile();
    }

    private HashSet<long> LoadGlobalFromFile()
    {
        try
        {
            if (!File.Exists(_globalFilePath))
            {
                return new HashSet<long>();
            }

            var json = File.ReadAllText(_globalFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new HashSet<long>();
            }

            try
            {
                // Пробуем сначала как List<long> (правильный формат)
                var list = JsonSerializer.Deserialize<List<long>>(json);
                return new HashSet<long>(list ?? new List<long>());
            }
            catch
            {
                try
                {
                    // Если не получилось, пробуем как Dictionary<long, DateTime> (старый формат ApprovedUsersStorage)
                    var dict = JsonSerializer.Deserialize<Dictionary<long, DateTime>>(json);
                    return dict != null ? new HashSet<long>(dict.Keys) : new HashSet<long>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось десериализовать глобальный список одобренных пользователей. Создаем пустой список.");
                    return new HashSet<long>();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке глобального списка одобренных пользователей");
            return new HashSet<long>();
        }
    }

    private Dictionary<long, Dictionary<long, GroupApprovalInfo>> LoadGroupsFromFile()
    {
        try
        {
            if (!File.Exists(_groupsFilePath))
            {
                return new Dictionary<long, Dictionary<long, GroupApprovalInfo>>();
            }

            var json = File.ReadAllText(_groupsFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<long, Dictionary<long, GroupApprovalInfo>>();
            }

            var data = JsonSerializer.Deserialize<Dictionary<long, Dictionary<long, GroupApprovalInfo>>>(json);
            return data ?? new Dictionary<long, Dictionary<long, GroupApprovalInfo>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке групповых списков одобренных пользователей");
            return new Dictionary<long, Dictionary<long, GroupApprovalInfo>>();
        }
    }

    private void SaveGlobalToFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(_globalFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var tempPath = Path.GetTempFileName();
            lock (_lock)
            {
                var list = _globalApprovedUsers.ToList();
                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tempPath, json);
            }
            
            File.Move(tempPath, _globalFilePath, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении глобального списка одобренных пользователей");
        }
    }

    private void SaveGroupsToFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(_groupsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var tempPath = Path.GetTempFileName();
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(_groupApprovedUsers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tempPath, json);
            }
            
            File.Move(tempPath, _groupsFilePath, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении групповых списков одобренных пользователей");
        }
    }

    /// <summary>
    /// Проверяет, одобрен ли пользователь глобально
    /// </summary>
    public bool IsGloballyApproved(long userId)
    {
        lock (_lock)
        {
            return _globalApprovedUsers.Contains(userId);
        }
    }

    /// <summary>
    /// Проверяет, одобрен ли пользователь в конкретной группе
    /// </summary>
    public bool IsApprovedInGroup(long userId, long groupId)
    {
        lock (_lock)
        {
            return _groupApprovedUsers.TryGetValue(groupId, out var groupUsers) && 
                   groupUsers.ContainsKey(userId);
        }
    }

    /// <summary>
    /// Проверяет, одобрен ли пользователь (глобально или в группе)
    /// </summary>
    public bool IsApproved(long userId, long? groupId = null)
    {
        lock (_lock)
        {
            // Сначала проверяем глобальное одобрение
            if (_globalApprovedUsers.Contains(userId))
                return true;

            // Если указана группа, проверяем одобрение в группе
            if (groupId.HasValue)
            {
                return IsApprovedInGroup(userId, groupId.Value);
            }

            return false;
        }
    }

    /// <summary>
    /// Одобряет пользователя глобально
    /// </summary>
    public void ApproveUserGlobally(long userId)
    {
        try
        {
            bool added;
            lock (_lock)
            {
                added = _globalApprovedUsers.Add(userId);
            }
            
            if (added)
            {
                _logger.LogInformation("Пользователь {UserId} добавлен в глобальный список одобренных", userId);
                SaveGlobalToFile();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении пользователя {UserId} в глобальный список одобренных", userId);
            throw;
        }
    }

    /// <summary>
    /// Одобряет пользователя в конкретной группе
    /// </summary>
    public void ApproveUserInGroup(long userId, long groupId)
    {
        try
        {
            bool added;
            lock (_lock)
            {
                if (!_groupApprovedUsers.ContainsKey(groupId))
                {
                    _groupApprovedUsers[groupId] = new Dictionary<long, GroupApprovalInfo>();
                }

                added = !_groupApprovedUsers[groupId].ContainsKey(userId);
                if (added)
                {
                    _groupApprovedUsers[groupId][userId] = new GroupApprovalInfo(DateTime.UtcNow);
                }
            }
            
            if (added)
            {
                _logger.LogInformation("Пользователь {UserId} добавлен в список одобренных группы {GroupId}", userId, groupId);
                SaveGroupsToFile();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении пользователя {UserId} в список одобренных группы {GroupId}", userId, groupId);
            throw;
        }
    }

    /// <summary>
    /// Удаляет пользователя из глобального списка одобренных
    /// </summary>
    public bool RemoveGlobalApproval(long userId)
    {
        try
        {
            bool removed;
            lock (_lock)
            {
                removed = _globalApprovedUsers.Remove(userId);
            }
            
            if (removed)
            {
                _logger.LogInformation("Пользователь {UserId} удален из глобального списка одобренных", userId);
                SaveGlobalToFile();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении пользователя {UserId} из глобального списка одобренных", userId);
            throw;
        }
    }

    /// <summary>
    /// Удаляет пользователя из списка одобренных конкретной группы
    /// </summary>
    public bool RemoveGroupApproval(long userId, long groupId)
    {
        try
        {
            bool removed;
            lock (_lock)
            {
                removed = _groupApprovedUsers.TryGetValue(groupId, out var groupUsers) && 
                          groupUsers.Remove(userId);
            }
            
            if (removed)
            {
                _logger.LogInformation("Пользователь {UserId} удален из списка одобренных группы {GroupId}", userId, groupId);
                SaveGroupsToFile();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении пользователя {UserId} из списка одобренных группы {GroupId}", userId, groupId);
            throw;
        }
    }

    /// <summary>
    /// Удаляет пользователя из всех списков одобренных (глобально и во всех группах)
    /// </summary>
    public bool RemoveAllApprovals(long userId)
    {
        try
        {
            bool globalRemoved = false;
            bool groupsRemoved = false;
            
            lock (_lock)
            {
                // Удаляем из глобального списка
                globalRemoved = _globalApprovedUsers.Remove(userId);
                
                // Удаляем из всех групп
                foreach (var groupUsers in _groupApprovedUsers.Values)
                {
                    if (groupUsers.Remove(userId))
                        groupsRemoved = true;
                }
            }
            
            // Сохраняем файлы только если были изменения
            if (globalRemoved)
            {
                SaveGlobalToFile();
            }
            if (groupsRemoved)
            {
                SaveGroupsToFile();
            }
            
            if (globalRemoved || groupsRemoved)
            {
                _logger.LogInformation("Пользователь {UserId} удален из всех списков одобренных", userId);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении пользователя {UserId} из всех списков одобренных", userId);
            throw;
        }
    }

    /// <summary>
    /// Получает информацию об одобрении пользователя в группе
    /// </summary>
    public GroupApprovalInfo? GetGroupApprovalInfo(long userId, long groupId)
    {
        lock (_lock)
        {
            if (_groupApprovedUsers.TryGetValue(groupId, out var groupUsers) &&
                groupUsers.TryGetValue(userId, out var info))
            {
                return info;
            }
            return null;
        }
    }

    /// <summary>
    /// Получает все группы, в которых пользователь одобрен
    /// </summary>
    public Dictionary<long, GroupApprovalInfo> GetUserGroupApprovals(long userId)
    {
        lock (_lock)
        {
            var result = new Dictionary<long, GroupApprovalInfo>();
            foreach (var (groupId, groupUsers) in _groupApprovedUsers)
            {
                if (groupUsers.TryGetValue(userId, out var info))
                {
                    result[groupId] = info;
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Получает статистику одобрений
    /// </summary>
    public (int globalCount, int groupCount, int totalGroupApprovals) GetApprovalStats()
    {
        lock (_lock)
        {
            var globalCount = _globalApprovedUsers.Count;
            var groupCount = _groupApprovedUsers.Count;
            var totalGroupApprovals = _groupApprovedUsers.Values.Sum(g => g.Count);
            
            return (globalCount, groupCount, totalGroupApprovals);
        }
    }
} 
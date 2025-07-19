using System.Text.Json;
using ClubDoorman.Models;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для управления списком подозрительных пользователей
/// Хранит данные в формате: chatId -> userId -> SuspiciousUserInfo
/// </summary>
public class SuspiciousUsersStorage
{
    private readonly string _filePath;
    private readonly ILogger<SuspiciousUsersStorage> _logger;
    private readonly object _lock = new object();
    private Dictionary<long, Dictionary<long, SuspiciousUserInfo>> _suspiciousUsers;

    public SuspiciousUsersStorage(ILogger<SuspiciousUsersStorage> logger)
    {
        _logger = logger;
        _filePath = Path.Combine("data", "suspicious_users.json");
        
        _suspiciousUsers = LoadFromFile();
    }

    /// <summary>
    /// Загрузка данных из файла
    /// </summary>
    private Dictionary<long, Dictionary<long, SuspiciousUserInfo>> LoadFromFile()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogInformation("Файл подозрительных пользователей не найден, создаем пустой список");
                return new Dictionary<long, Dictionary<long, SuspiciousUserInfo>>();
            }

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<long, Dictionary<long, SuspiciousUserInfo>>();
            }

            var data = JsonSerializer.Deserialize<Dictionary<long, Dictionary<long, SuspiciousUserInfo>>>(json);
            var count = data?.Values.Sum(g => g.Count) ?? 0;
            _logger.LogInformation("Загружено {Count} подозрительных пользователей из файла", count);
            
            return data ?? new Dictionary<long, Dictionary<long, SuspiciousUserInfo>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке списка подозрительных пользователей");
            return new Dictionary<long, Dictionary<long, SuspiciousUserInfo>>();
        }
    }

    /// <summary>
    /// Сохранение данных в файл
    /// </summary>
    private void SaveToFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_suspiciousUsers, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(_filePath, json);
            _logger.LogDebug("Список подозрительных пользователей сохранен в файл");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении списка подозрительных пользователей");
        }
    }

    /// <summary>
    /// Проверка, является ли пользователь подозрительным в данном чате
    /// </summary>
    public bool IsSuspicious(long userId, long chatId)
    {
        lock (_lock)
        {
            return _suspiciousUsers.TryGetValue(chatId, out var chatUsers) &&
                   chatUsers.ContainsKey(userId);
        }
    }

    /// <summary>
    /// Добавление пользователя в список подозрительных
    /// </summary>
    public bool AddSuspicious(long userId, long chatId, SuspiciousUserInfo info)
    {
        lock (_lock)
        {
            if (!_suspiciousUsers.ContainsKey(chatId))
            {
                _suspiciousUsers[chatId] = new Dictionary<long, SuspiciousUserInfo>();
            }

            var isNew = !_suspiciousUsers[chatId].ContainsKey(userId);
            _suspiciousUsers[chatId][userId] = info;
            
            SaveToFile();
            
            if (isNew)
            {
                _logger.LogInformation("Пользователь {UserId} добавлен в подозрительные для чата {ChatId}", 
                    userId, chatId);
            }
            
            return isNew;
        }
    }

    /// <summary>
    /// Удаление пользователя из списка подозрительных
    /// </summary>
    public bool RemoveSuspicious(long userId, long chatId)
    {
        lock (_lock)
        {
            var removed = false;
            
            if (_suspiciousUsers.TryGetValue(chatId, out var chatUsers))
            {
                removed = chatUsers.Remove(userId);
                
                // Удаляем пустую группу
                if (chatUsers.Count == 0)
                {
                    _suspiciousUsers.Remove(chatId);
                }
            }
            
            if (removed)
            {
                SaveToFile();
                _logger.LogInformation("Пользователь {UserId} удален из подозрительных для чата {ChatId}", 
                    userId, chatId);
            }
            
            return removed;
        }
    }

    /// <summary>
    /// Обновление счетчика сообщений для подозрительного пользователя
    /// </summary>
    public bool UpdateMessageCount(long userId, long chatId, int messageCount)
    {
        lock (_lock)
        {
            if (_suspiciousUsers.TryGetValue(chatId, out var chatUsers) &&
                chatUsers.TryGetValue(userId, out var info))
            {
                var updatedInfo = info with { MessagesSinceSuspicious = messageCount };
                chatUsers[userId] = updatedInfo;
                
                SaveToFile();
                return true;
            }
            
            return false;
        }
    }

    /// <summary>
    /// Включение/выключение AI детекта для пользователя
    /// </summary>
    public bool SetAiDetectEnabled(long userId, long chatId, bool enabled)
    {
        lock (_lock)
        {
            if (_suspiciousUsers.TryGetValue(chatId, out var chatUsers) &&
                chatUsers.TryGetValue(userId, out var info))
            {
                var updatedInfo = info with { AiDetectEnabled = enabled };
                chatUsers[userId] = updatedInfo;
                
                SaveToFile();
                _logger.LogInformation("AI детект для пользователя {UserId} в чате {ChatId}: {Status}", 
                    userId, chatId, enabled ? "включен" : "выключен");
                return true;
            }
            
            return false;
        }
    }

    /// <summary>
    /// Получение информации о подозрительном пользователе
    /// </summary>
    public SuspiciousUserInfo? GetSuspiciousInfo(long userId, long chatId)
    {
        lock (_lock)
        {
            if (_suspiciousUsers.TryGetValue(chatId, out var chatUsers) &&
                chatUsers.TryGetValue(userId, out var info))
            {
                return info;
            }
            
            return null;
        }
    }

    /// <summary>
    /// Получение списка пользователей с включенным AI детектом
    /// </summary>
    public List<(long UserId, long ChatId)> GetAiDetectUsers()
    {
        lock (_lock)
        {
            return _suspiciousUsers
                .SelectMany(chat => chat.Value
                    .Where(user => user.Value.AiDetectEnabled)
                    .Select(user => (user.Key, chat.Key)))
                .ToList();
        }
    }

    /// <summary>
    /// Получает информацию о конкретном подозрительном пользователе
    /// </summary>
    public SuspiciousUserInfo? GetSuspiciousUser(long userId, long chatId)
    {
        lock (_lock)
        {
            if (_suspiciousUsers.TryGetValue(chatId, out var chatUsers) &&
                chatUsers.TryGetValue(userId, out var userInfo))
            {
                return userInfo;
            }
            return null;
        }
    }

    /// <summary>
    /// Получение статистики по подозрительным пользователям
    /// </summary>
    public (int TotalSuspicious, int WithAiDetect, int GroupsCount) GetStats()
    {
        lock (_lock)
        {
            var totalSuspicious = _suspiciousUsers.Values.Sum(g => g.Count);
            var withAiDetect = _suspiciousUsers.Values
                .SelectMany(g => g.Values)
                .Count(info => info.AiDetectEnabled);
            var groupsCount = _suspiciousUsers.Count;
            
            return (totalSuspicious, withAiDetect, groupsCount);
        }
    }
} 
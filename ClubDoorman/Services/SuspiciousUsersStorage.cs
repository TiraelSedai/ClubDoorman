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

    /// <summary>
    /// Создает экземпляр хранилища подозрительных пользователей.
    /// </summary>
    /// <param name="logger">Логгер для записи событий</param>
    /// <exception cref="ArgumentNullException">Если logger равен null</exception>
    public SuspiciousUsersStorage(ILogger<SuspiciousUsersStorage> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _filePath = Path.Combine("data", "suspicious_users.json");
        
        _suspiciousUsers = LoadFromFile();
    }

    /// <summary>
    /// Загрузка данных из файла
    /// </summary>
    /// <returns>Словарь подозрительных пользователей</returns>
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

            // CRITICAL PERFORMANCE ISSUE - FIX REQUIRED
            // JsonSerializerOptions is recreated on every serialization call, causing unnecessary allocations
            // This can impact performance when saving suspicious users frequently
            // TODO: Cache JsonSerializerOptions as static readonly field to improve performance
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
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <returns>true, если пользователь подозрительный</returns>
    public bool IsSuspicious(long userId, long chatId)
    {
        // PERFORMANCE OPTIMIZATION - Consider ReaderWriterLockSlim for better read performance
        lock (_lock)
        {
            return _suspiciousUsers.TryGetValue(chatId, out var chatUsers) &&
                   chatUsers.ContainsKey(userId);
        }
    }

    /// <summary>
    /// Добавление пользователя в список подозрительных
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="info">Информация о подозрительном пользователе</param>
    /// <returns>true, если пользователь был добавлен впервые</returns>
    /// <exception cref="ArgumentNullException">Если info равен null</exception>
    public bool AddSuspicious(long userId, long chatId, SuspiciousUserInfo info)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));

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
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <returns>true, если пользователь был удален</returns>
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
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="messageCount">Количество сообщений</param>
    /// <returns>true, если счетчик был обновлен</returns>
    public bool UpdateMessageCount(long userId, long chatId, int messageCount)
    {
        if (messageCount < 0)
            return false;

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
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="enabled">Включить или выключить AI детект</param>
    /// <returns>true, если настройка была изменена</returns>
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
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <returns>Информация о пользователе или null, если не найден</returns>
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
    /// <returns>Список кортежей (UserId, ChatId) пользователей с AI детектом</returns>
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
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <returns>Информация о пользователе или null, если не найден</returns>
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
    /// <returns>Кортеж (Общее количество подозрительных, С AI детектом, Количество групп)</returns>
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
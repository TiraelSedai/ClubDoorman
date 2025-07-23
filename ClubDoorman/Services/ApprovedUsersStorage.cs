using System.Collections.Concurrent;
using System.Text.Json;

namespace ClubDoorman.Services;

/// <summary>
/// Хранилище одобренных пользователей с поддержкой атомарной сериализации и потокобезопасности.
/// </summary>
public class ApprovedUsersStorage
{
    private readonly string _filePath;
    private readonly HashSet<long> _approvedUsers;
    private readonly ILogger<ApprovedUsersStorage> _logger;
    private readonly object _lock = new object();

    /// <summary>
    /// Создает экземпляр хранилища одобренных пользователей.
    /// </summary>
    /// <param name="logger">Логгер для записи ошибок и событий</param>
    public ApprovedUsersStorage(ILogger<ApprovedUsersStorage> logger)
    {
        _logger = logger;
        _filePath = Path.Combine("data", "approved_users.json");
        _approvedUsers = LoadFromFile();
    }

    /// <summary>
    /// Загружает список одобренных пользователей из файла.
    /// </summary>
    /// <returns>Множество ID одобренных пользователей</returns>
    private HashSet<long> LoadFromFile()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new HashSet<long>();

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new HashSet<long>();

            try
            {
                // Пробуем сначала как List<long> (новый формат)
                var list = JsonSerializer.Deserialize<List<long>>(json);
                return new HashSet<long>(list ?? new List<long>());
            }
            catch
            {
                try
                {
                    // Если не получилось, пробуем как Dictionary<long, DateTime> (старый формат)
                    var dict = JsonSerializer.Deserialize<Dictionary<long, DateTime>>(json);
                    return dict != null ? new HashSet<long>(dict.Keys) : new HashSet<long>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось десериализовать JSON ни как список, ни как словарь. Создаем пустой список одобренных пользователей.");
                    return new HashSet<long>();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке списка одобренных пользователей");
            return new HashSet<long>();
        }
    }

    /// <summary>
    /// Сохраняет список одобренных пользователей в файл атомарно.
    /// </summary>
    private void SaveToFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Создаем временный файл
            var tempPath = Path.GetTempFileName();
            lock (_lock)
            {
                var list = _approvedUsers.ToList();
                var json = JsonSerializer.Serialize(list);
                
                // Сначала записываем во временный файл
                File.WriteAllText(tempPath, json);
            }
            
            // Затем атомарно заменяем целевой файл
            File.Move(tempPath, _filePath, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении списка одобренных пользователей");
        }
    }

    /// <summary>
    /// Проверяет, одобрен ли пользователь.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>true, если пользователь одобрен</returns>
    public bool IsApproved(long userId)
    {
        lock (_lock)
        {
            return _approvedUsers.Contains(userId);
        }
    }

    /// <summary>
    /// Добавляет пользователя в список одобренных и сохраняет изменения.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <exception cref="Exception">В случае ошибки сериализации или записи</exception>
    public void ApproveUser(long userId)
    {
        try
        {
            bool added;
            lock (_lock)
            {
                added = _approvedUsers.Add(userId);
            }
            
            if (added)
            {
                _logger.LogInformation("Пользователь {UserId} добавлен в список одобренных", userId);
                SaveToFile();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении пользователя {UserId} в список одобренных", userId);
            throw;
        }
    }

    /// <summary>
    /// Удаляет пользователя из списка одобренных и сохраняет изменения.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>true, если пользователь был удалён</returns>
    /// <exception cref="Exception">В случае ошибки сериализации или записи</exception>
    public bool RemoveApproval(long userId)
    {
        try
        {
            bool removed;
            lock (_lock)
            {
                removed = _approvedUsers.Remove(userId);
            }
            
            if (removed)
            {
                _logger.LogInformation("Пользователь {UserId} удален из списка одобренных", userId);
                SaveToFile();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении пользователя {UserId} из списка одобренных", userId);
            throw;
        }
    }
} 
using System.Collections.Concurrent;
using System.Runtime.Caching;

namespace ClubDoorman.Services;

/// <summary>
/// Типы нарушений для отслеживания
/// </summary>
public enum ViolationType
{
    /// <summary>
    /// ML фильтр
    /// </summary>
    MlSpam,
    
    /// <summary>
    /// Стоп-слова
    /// </summary>
    StopWords,
    
    /// <summary>
    /// Слишком много эмодзи
    /// </summary>
    TooManyEmojis,
    
    /// <summary>
    /// Lookalike символы
    /// </summary>
    LookalikeSymbols,
    
    /// <summary>
    /// Банальные приветствия
    /// </summary>
    BoringGreetings
}

/// <summary>
/// Сервис для отслеживания повторных нарушений пользователей
/// </summary>
public class ViolationTracker : IViolationTracker
{
    private readonly ILogger<ViolationTracker> _logger;
    private readonly IAppConfig _appConfig;
    
    public ViolationTracker(ILogger<ViolationTracker> logger, IAppConfig appConfig)
    {
        _logger = logger;
        _appConfig = appConfig;
    }
    
    /// <summary>
    /// Регистрирует нарушение и возвращает true если нужно забанить пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="violationType">Тип нарушения</param>
    /// <returns>true если нужно забанить пользователя</returns>
    public bool RegisterViolation(long userId, long chatId, ViolationType violationType)
    {
        var key = $"violations_{chatId}_{userId}_{violationType}";
        var currentCount = MemoryCache.Default.Get(key) as int? ?? 0;
        var newCount = currentCount + 1;
        
        // Сохраняем в кэш на 24 часа
        MemoryCache.Default.Set(key, newCount, DateTimeOffset.UtcNow.AddHours(24));
        
        var maxViolations = GetMaxViolationsForType(violationType);
        
        _logger.LogInformation("Нарушение {ViolationType} для пользователя {UserId} в чате {ChatId}: {CurrentCount}/{MaxViolations}", 
            violationType, userId, chatId, newCount, maxViolations);
        
        // Если достигли лимита нарушений
        if (maxViolations > 0 && newCount >= maxViolations)
        {
            _logger.LogWarning("Достигнут лимит нарушений {ViolationType} для пользователя {UserId} в чате {ChatId}: {Count}/{Max}", 
                violationType, userId, chatId, newCount, maxViolations);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Получает количество нарушений для пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="violationType">Тип нарушения</param>
    /// <returns>Количество нарушений</returns>
    public int GetViolationCount(long userId, long chatId, ViolationType violationType)
    {
        var key = $"violations_{chatId}_{userId}_{violationType}";
        return MemoryCache.Default.Get(key) as int? ?? 0;
    }
    
    /// <summary>
    /// Сбрасывает счетчик нарушений для пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="violationType">Тип нарушения</param>
    public void ResetViolations(long userId, long chatId, ViolationType violationType)
    {
        var key = $"violations_{chatId}_{userId}_{violationType}";
        MemoryCache.Default.Remove(key);
        _logger.LogInformation("Сброшен счетчик нарушений {ViolationType} для пользователя {UserId} в чате {ChatId}", 
            violationType, userId, chatId);
    }
    
    /// <summary>
    /// Получает максимальное количество нарушений для типа
    /// </summary>
    /// <param name="violationType">Тип нарушения</param>
    /// <returns>Максимальное количество нарушений</returns>
    private int GetMaxViolationsForType(ViolationType violationType)
    {
        return violationType switch
        {
            ViolationType.MlSpam => _appConfig.MlViolationsBeforeBan,
            ViolationType.StopWords => _appConfig.StopWordsViolationsBeforeBan,
            ViolationType.TooManyEmojis => _appConfig.EmojiViolationsBeforeBan,
            ViolationType.LookalikeSymbols => _appConfig.LookalikeViolationsBeforeBan,
            ViolationType.BoringGreetings => _appConfig.BoringGreetingsViolationsBeforeBan,
            _ => 0
        };
    }
    
    /// <summary>
    /// Получает название типа нарушения для логов
    /// </summary>
    /// <param name="violationType">Тип нарушения</param>
    /// <returns>Название нарушения</returns>
    public static string GetViolationTypeName(ViolationType violationType)
    {
        return violationType switch
        {
            ViolationType.MlSpam => "ML спам",
            ViolationType.StopWords => "стоп-слова",
            ViolationType.TooManyEmojis => "много эмодзи",
            ViolationType.LookalikeSymbols => "lookalike символы",
            ViolationType.BoringGreetings => "банальные приветствия",
            _ => "неизвестное нарушение"
        };
    }
} 
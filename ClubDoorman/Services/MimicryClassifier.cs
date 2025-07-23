using ClubDoorman.Infrastructure;

namespace ClubDoorman.Services;

/// <summary>
/// Классификатор для анализа первых сообщений пользователя на предмет мимикрии (имитации нормального поведения)
/// </summary>
public class MimicryClassifier : IMimicryClassifier
{
    private readonly ILogger<MimicryClassifier> _logger;
    
    // Шаблонные фразы, часто используемые спамерами
    private readonly HashSet<string> _templatePhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "привет", "приветствую", "здравствуйте", "добрый день", "добрый вечер",
        "как дела", "как у кого дела", "как дела у всех", "что нового",
        "?", "!", "ок", "понятно", "спасибо", "пасиб", "хорошо", "норм",
        "всем привет", "привет всем", "всем хай", "хай всем"
    };
    
    public MimicryClassifier(ILogger<MimicryClassifier> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Анализ первых трех сообщений пользователя
    /// </summary>
    /// <param name="messages">Список первых сообщений</param>
    /// <returns>Оценка подозрительности от 0.0 (не подозрительно) до 1.0 (очень подозрительно)</returns>
    public double AnalyzeMessages(List<string> messages)
    {
        if (messages == null || messages.Count != 3)
        {
            _logger.LogWarning("Анализ мимикрии: ожидается ровно 3 сообщения, получено {Count}", messages?.Count ?? 0);
            return 0.0;
        }
        
        try
        {
            var score = 0.0;
            var weights = new Dictionary<string, double>();
            
            // 1. Анализ длины сообщений
            var lengthScore = AnalyzeMessageLength(messages);
            weights["length"] = lengthScore;
            
            // 2. Анализ шаблонности
            var templateScore = AnalyzeTemplateUsage(messages);
            weights["template"] = templateScore;
            
            // 3. Анализ разнообразия
            var diversityScore = AnalyzeDiversity(messages);
            weights["diversity"] = diversityScore;
            
            // 4. Анализ контекстности (отвечает ли на что-то)
            var contextScore = AnalyzeContext(messages);
            weights["context"] = contextScore;
            
            // Взвешенная сумма
            score = lengthScore * 0.25 + templateScore * 0.35 + diversityScore * 0.25 + contextScore * 0.15;
            
            _logger.LogDebug("Анализ мимикрии: общий балл {Score:F2}, детали: {Details}", 
                score, string.Join(", ", weights.Select(kv => $"{kv.Key}={kv.Value:F2}")));
            
            return Math.Clamp(score, 0.0, 1.0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при анализе мимикрии");
            return 0.0;
        }
    }
    
    /// <summary>
    /// Анализ длины сообщений (короткие сообщения = подозрительно)
    /// </summary>
    private double AnalyzeMessageLength(List<string> messages)
    {
        var lengths = messages.Select(m => m?.Trim().Length ?? 0).ToList();
        var avgLength = lengths.Average();
        
        // Если все сообщения очень короткие (менее 3 символов в среднем) - подозрительно
        if (avgLength <= 2) return 1.0;
        if (avgLength <= 5) return 0.7;
        if (avgLength <= 10) return 0.3;
        
        return 0.0;
    }
    
    /// <summary>
    /// Анализ использования шаблонных фраз
    /// </summary>
    private double AnalyzeTemplateUsage(List<string> messages)
    {
        var templateCount = 0;
        
        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message))
                continue;
                
            var cleanMessage = message.Trim().ToLower();
            
            // Проверяем точное совпадение
            if (_templatePhrases.Contains(cleanMessage))
            {
                templateCount++;
                continue;
            }
            
            // Проверяем вхождение шаблонных фраз
            if (_templatePhrases.Any(template => cleanMessage.Contains(template)))
            {
                templateCount++;
            }
        }
        
        // Если все 3 сообщения шаблонные - очень подозрительно
        return templateCount / 3.0;
    }
    
    /// <summary>
    /// Анализ разнообразия сообщений
    /// </summary>
    private double AnalyzeDiversity(List<string> messages)
    {
        var cleanMessages = messages
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(m => m.Trim().ToLower())
            .ToList();
        
        if (cleanMessages.Count == 0) return 1.0;
        
        // Если все сообщения одинаковые - очень подозрительно
        if (cleanMessages.Distinct().Count() == 1) return 1.0;
        
        // Анализ разнообразия слов
        var allWords = cleanMessages
            .SelectMany(m => m.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToList();
        
        if (allWords.Count == 0) return 1.0;
        
        var uniqueWords = allWords.Distinct().Count();
        var diversityRatio = (double)uniqueWords / allWords.Count;
        
        // Низкое разнообразие = подозрительно
        if (diversityRatio < 0.3) return 0.8;
        if (diversityRatio < 0.5) return 0.5;
        if (diversityRatio < 0.7) return 0.2;
        
        return 0.0;
    }
    
    /// <summary>
    /// Анализ контекстности (содержат ли сообщения ответы, упоминания и т.д.)
    /// </summary>
    private double AnalyzeContext(List<string> messages)
    {
        var contextIndicators = 0;
        
        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message))
                continue;
                
            var msg = message.ToLower();
            
            // Признаки контекстности
            if (msg.Contains("@") ||                    // упоминания
                msg.Contains("согласен") ||             // согласие
                msg.Contains("не согласен") ||          // несогласие
                msg.Contains("выше") ||                 // ссылка на предыдущие сообщения
                msg.Contains("интересно") ||            // реакция
                msg.Contains("тоже") ||                 // сравнение
                msg.Contains("а я") ||                  // личный опыт
                msg.Contains("у меня"))                 // личный опыт
            {
                contextIndicators++;
            }
        }
        
        // Отсутствие контекста = подозрительно
        return 1.0 - (double)contextIndicators / 3.0;
    }
} 
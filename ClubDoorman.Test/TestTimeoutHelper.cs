using System.Text.Json;
using NUnit.Framework;

namespace ClubDoorman.Test;

/// <summary>
/// Утилита для управления таймаутами тестов на основе конфигурации
/// </summary>
public static class TestTimeoutHelper
{
    private static TestTimeoutConfig? _config;
    private static readonly object _lock = new object();

    /// <summary>
    /// Конфигурация таймаутов
    /// </summary>
    public class TestTimeoutConfig
    {
        public int DefaultTimeoutSeconds { get; set; } = 5;
        public Dictionary<string, TestClassConfig> TestTimeouts { get; set; } = new();
    }

    public class TestClassConfig
    {
        public int DefaultTimeoutSeconds { get; set; }
        public Dictionary<string, int> SpecificTests { get; set; } = new();
    }

    /// <summary>
    /// Загружает конфигурацию таймаутов из файла
    /// </summary>
    public static TestTimeoutConfig LoadConfig()
    {
        if (_config != null) return _config;

        lock (_lock)
        {
            if (_config != null) return _config;

            try
            {
                var configPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "test-timeouts.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    _config = JsonSerializer.Deserialize<TestTimeoutConfig>(json);
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Warning: Could not load test timeout config: {ex.Message}");
            }

            _config ??= new TestTimeoutConfig();
            return _config;
        }
    }

    /// <summary>
    /// Получает таймаут для конкретного теста
    /// </summary>
    public static int GetTimeoutForTest(string testClassName, string testMethodName)
    {
        var config = LoadConfig();
        
        // Проверяем специфичный таймаут для метода
        if (config.TestTimeouts.TryGetValue(testClassName, out var classConfig))
        {
            if (classConfig.SpecificTests.TryGetValue(testMethodName, out var specificTimeout))
            {
                return specificTimeout;
            }
            
            // Возвращаем таймаут по умолчанию для класса
            if (classConfig.DefaultTimeoutSeconds > 0)
            {
                return classConfig.DefaultTimeoutSeconds;
            }
        }
        
        // Возвращаем глобальный таймаут по умолчанию
        return config.DefaultTimeoutSeconds;
    }

    /// <summary>
    /// Создает CancellationTokenSource с таймаутом для текущего теста
    /// </summary>
    public static CancellationTokenSource CreateTimeoutToken(string testClassName, string testMethodName)
    {
        var timeoutSeconds = GetTimeoutForTest(testClassName, testMethodName);
        return new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
    }

    /// <summary>
    /// Создает CancellationTokenSource с таймаутом для текущего теста
    /// </summary>
    public static CancellationTokenSource CreateTimeoutToken()
    {
        var testName = TestContext.CurrentContext.Test.Name;
        var className = TestContext.CurrentContext.Test.ClassName?.Split('.').Last() ?? "Unknown";
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] TestTimeoutHelper: Creating timeout token for {className}.{testName}");
        
        var timeoutSeconds = GetTimeoutForTest(className, testName);
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] TestTimeoutHelper: Using timeout of {timeoutSeconds} seconds for {className}.{testName}");
        
        return new CancellationTokenSource(timeout);
    }
} 
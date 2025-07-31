using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ClubDoorman.Test.TestInfrastructure;

/// <summary>
/// Универсальный тест-раннер для сравнения поведения старой и новой логики
/// Использует FluentAssertions.BeEquivalentTo для глубокого сравнения
/// </summary>
public class BehaviorComparisonTestRunner<TRequest, TResponse>
{
    private readonly ILogger<BehaviorComparisonTestRunner<TRequest, TResponse>> _logger;
    private readonly string _testName;
    private readonly string _outputDir;

    public BehaviorComparisonTestRunner(
        ILogger<BehaviorComparisonTestRunner<TRequest, TResponse>> logger,
        string testName,
        string outputDir = "TestResults/behavior-comparison")
    {
        _logger = logger;
        _testName = testName;
        _outputDir = outputDir;
        
        // Создаем директорию для результатов
        Directory.CreateDirectory(outputDir);
    }

    /// <summary>
    /// Сравнивает поведение старой и новой логики
    /// </summary>
    /// <param name="request">Входные данные</param>
    /// <param name="oldHandler">Старый обработчик</param>
    /// <param name="newHandler">Новый обработчик</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат сравнения</returns>
    public async Task<ComparisonResult> CompareBehaviorAsync(
        TRequest request,
        Func<TRequest, CancellationToken, Task<TResponse>> oldHandler,
        Func<TRequest, CancellationToken, Task<TResponse>> newHandler,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 Начинаем сравнение поведения для {TestName}", _testName);

        var result = new ComparisonResult
        {
            TestName = _testName,
            Request = request,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Выполняем старую логику
            _logger.LogDebug("🔄 Выполняем старую логику...");
            var oldStartTime = DateTime.UtcNow;
            result.OldResponse = await oldHandler(request, cancellationToken);
            result.OldExecutionTime = DateTime.UtcNow - oldStartTime;

            // Выполняем новую логику
            _logger.LogDebug("🔄 Выполняем новую логику...");
            var newStartTime = DateTime.UtcNow;
            result.NewResponse = await newHandler(request, cancellationToken);
            result.NewExecutionTime = DateTime.UtcNow - newStartTime;

                            // Сравниваем результаты
                _logger.LogDebug("🔍 Сравниваем результаты...");
                result.AreEquivalent = CompareResponses((TResponse)result.OldResponse!, (TResponse)result.NewResponse!);

            if (result.AreEquivalent)
            {
                _logger.LogInformation("✅ Поведение идентично для {TestName}", _testName);
            }
            else
            {
                _logger.LogWarning("⚠️ Обнаружены различия в поведении для {TestName}", _testName);
                                        result.Differences = FindDifferences((TResponse)result.OldResponse!, (TResponse)result.NewResponse!);
            }

            // Сохраняем результаты
            await SaveResultsAsync(result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при сравнении поведения для {TestName}", _testName);
            result.Error = ex.Message;
            result.HasError = true;
            
            await SaveResultsAsync(result);
            throw;
        }
    }

    /// <summary>
    /// Сравнивает два ответа на эквивалентность
    /// </summary>
    private bool CompareResponses(TResponse oldResponse, TResponse newResponse)
    {
        try
        {
                                    oldResponse.Should().BeEquivalentTo(newResponse, options => options
                            .IncludingAllRuntimeProperties()
                            .IncludingAllDeclaredProperties()
                            .RespectingRuntimeTypes()
                            .WithAutoConversion());
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Сравнение не прошло: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Находит различия между ответами
    /// </summary>
    private List<string> FindDifferences(TResponse oldResponse, TResponse newResponse)
    {
        var differences = new List<string>();

        try
        {
                                    // Используем FluentAssertions для получения детальных различий
                        oldResponse.Should().BeEquivalentTo(newResponse, options => options
                            .IncludingAllRuntimeProperties()
                            .IncludingAllDeclaredProperties()
                            .RespectingRuntimeTypes()
                            .WithAutoConversion());
        }
        catch (FluentAssertions.Execution.AssertionFailedException ex)
        {
            differences.Add($"Структурные различия: {ex.Message}");
        }
        catch (Exception ex)
        {
            differences.Add($"Ошибка сравнения: {ex.Message}");
        }

        // Дополнительное сравнение через JSON
        try
        {
            var oldJson = JsonConvert.SerializeObject(oldResponse, Formatting.Indented);
            var newJson = JsonConvert.SerializeObject(newResponse, Formatting.Indented);
            
            if (oldJson != newJson)
            {
                differences.Add("JSON представления различаются");
                differences.Add($"Старый JSON: {oldJson}");
                differences.Add($"Новый JSON: {newJson}");
            }
        }
        catch (Exception ex)
        {
            differences.Add($"Ошибка JSON сравнения: {ex.Message}");
        }

        return differences;
    }

    /// <summary>
    /// Сохраняет результаты сравнения в файл
    /// </summary>
    private async Task SaveResultsAsync(ComparisonResult result)
    {
        try
        {
            var fileName = $"{_testName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(_outputDir, fileName);

            var json = JsonConvert.SerializeObject(result, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Include
            });

            await File.WriteAllTextAsync(filePath, json);
            _logger.LogInformation("💾 Результаты сохранены в {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при сохранении результатов");
        }
    }
}

/// <summary>
/// Результат сравнения поведения
/// </summary>
public class ComparisonResult
{
    public string TestName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object? Request { get; set; }
    public object? OldResponse { get; set; }
    public object? NewResponse { get; set; }
    public TimeSpan OldExecutionTime { get; set; }
    public TimeSpan NewExecutionTime { get; set; }
    public bool AreEquivalent { get; set; }
    public List<string> Differences { get; set; } = new();
    public bool HasError { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Расширения для удобного использования в тестах
/// </summary>
public static class BehaviorComparisonExtensions
{
    /// <summary>
    /// Создает BehaviorComparisonTestRunner для теста
    /// </summary>
    public static BehaviorComparisonTestRunner<TRequest, TResponse> CreateBehaviorComparison<TRequest, TResponse>(
        this TestContext context,
        string testName)
    {
        var logger = new Mock<ILogger<BehaviorComparisonTestRunner<TRequest, TResponse>>>().Object;
        return new BehaviorComparisonTestRunner<TRequest, TResponse>(logger, testName);
    }

    /// <summary>
    /// Утверждает, что поведение идентично
    /// </summary>
    public static void ShouldBeEquivalent(this ComparisonResult result)
    {
        result.AreEquivalent.Should().BeTrue("Поведение должно быть идентично");
        
        if (result.Differences.Any())
        {
            Assert.Fail($"Обнаружены различия:\n{string.Join("\n", result.Differences)}");
        }
    }

    /// <summary>
    /// Утверждает, что новая логика работает быстрее
    /// </summary>
    public static void ShouldBeFaster(this ComparisonResult result, double expectedImprovement = 0.1)
    {
        var improvement = (result.OldExecutionTime - result.NewExecutionTime) / result.OldExecutionTime;
        improvement.Should().BeGreaterThan(expectedImprovement, 
            $"Новая логика должна быть быстрее на {expectedImprovement:P0}");
    }
} 
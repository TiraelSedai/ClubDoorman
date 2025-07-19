using NUnit.Framework;

namespace ClubDoorman.Test;

/// <summary>
/// Базовый класс для тестов с автоматическим управлением таймаутами
/// </summary>
public abstract class TestBase
{
    protected CancellationTokenSource? TimeoutToken { get; private set; }

    [SetUp]
    public virtual void SetUp()
    {
        // Создаем токен таймаута для каждого теста
        TimeoutToken = TestTimeoutHelper.CreateTimeoutToken();
    }

    [TearDown]
    public virtual void TearDown()
    {
        // Освобождаем ресурсы
        TimeoutToken?.Dispose();
        TimeoutToken = null;
    }

    /// <summary>
    /// Выполняет асинхронную операцию с таймаутом
    /// </summary>
    protected async Task<T> ExecuteWithTimeout<T>(Func<CancellationToken, Task<T>> operation)
    {
        if (TimeoutToken == null)
            throw new InvalidOperationException("TimeoutToken not initialized");

        try
        {
            return await operation(TimeoutToken.Token);
        }
        catch (OperationCanceledException) when (TimeoutToken.Token.IsCancellationRequested)
        {
            var testName = TestContext.CurrentContext.Test.Name;
            var className = TestContext.CurrentContext.Test.ClassName?.Split('.').Last() ?? "Unknown";
            var timeout = TestTimeoutHelper.GetTimeoutForTest(className, testName);
            throw new TimeoutException($"Test '{testName}' timed out after {timeout} seconds");
        }
    }

    /// <summary>
    /// Выполняет асинхронную операцию с таймаутом (без возвращаемого значения)
    /// </summary>
    protected async Task ExecuteWithTimeout(Func<CancellationToken, Task> operation)
    {
        if (TimeoutToken == null)
            throw new InvalidOperationException("TimeoutToken not initialized");

        try
        {
            await operation(TimeoutToken.Token);
        }
        catch (OperationCanceledException) when (TimeoutToken.Token.IsCancellationRequested)
        {
            var testName = TestContext.CurrentContext.Test.Name;
            var className = TestContext.CurrentContext.Test.ClassName?.Split('.').Last() ?? "Unknown";
            var timeout = TestTimeoutHelper.GetTimeoutForTest(className, testName);
            throw new TimeoutException($"Test '{testName}' timed out after {timeout} seconds");
        }
    }
} 
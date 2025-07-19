using NUnit.Framework;

namespace ClubDoorman.Test;

/// <summary>
/// Базовый класс для тестов с автоматическим управлением таймаутами.
/// Предоставляет инфраструктуру для выполнения тестов с настраиваемыми таймаутами
/// и автоматическим управлением ресурсами.
/// </summary>
/// <remarks>
/// Этот класс автоматически создает и освобождает CancellationTokenSource для каждого теста,
/// обеспечивая корректное завершение тестов при превышении времени выполнения.
/// </remarks>
public abstract class TestBase
{
    /// <summary>
    /// Получает токен отмены с настроенным таймаутом для текущего теста.
    /// </summary>
    /// <value>
    /// CancellationTokenSource с таймаутом, настроенным для текущего теста,
    /// или null если токен не был инициализирован.
    /// </value>
    protected CancellationTokenSource? TimeoutToken { get; private set; }

    /// <summary>
    /// Инициализирует тестовое окружение и создает токен таймаута.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывается автоматически перед каждым тестом.
    /// Создает CancellationTokenSource с таймаутом, настроенным для текущего теста
    /// в соответствии с конфигурацией в test-timeouts.json.
    /// </remarks>
    [SetUp]
    public virtual void SetUp()
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] TestBase.SetUp: Creating timeout token for {TestContext.CurrentContext.Test.Name}");
        
        // Создаем токен таймаута для каждого теста
        TimeoutToken = TestTimeoutHelper.CreateTimeoutToken();
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] TestBase.SetUp: Timeout token created with {TimeoutToken.Token.CanBeCanceled} cancellation support");
    }

    /// <summary>
    /// Освобождает ресурсы тестового окружения.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывается автоматически после каждого теста.
    /// Освобождает CancellationTokenSource и связанные ресурсы.
    /// </remarks>
    [TearDown]
    public virtual void TearDown()
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] TestBase.TearDown: Disposing timeout token for {TestContext.CurrentContext.Test.Name}");
        
        // Освобождаем ресурсы
        TimeoutToken?.Dispose();
        TimeoutToken = null;
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] TestBase.TearDown: Timeout token disposed");
    }

    /// <summary>
    /// Выполняет асинхронную операцию с таймаутом
    /// </summary>
    protected async Task<T> ExecuteWithTimeout<T>(Func<CancellationToken, Task<T>> operation)
    {
        var testName = TestContext.CurrentContext.Test.Name;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ExecuteWithTimeout<T>: Starting async operation for {testName}");
        
        if (TimeoutToken == null)
            throw new InvalidOperationException("TimeoutToken not initialized");

        try
        {
            var result = await operation(TimeoutToken.Token);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ExecuteWithTimeout<T>: Completed async operation for {testName}");
            return result;
        }
        catch (OperationCanceledException) when (TimeoutToken.Token.IsCancellationRequested)
        {
            var className = TestContext.CurrentContext.Test.ClassName?.Split('.').Last() ?? "Unknown";
            var timeout = TestTimeoutHelper.GetTimeoutForTest(className, testName);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ExecuteWithTimeout<T>: TIMEOUT for {testName} after {timeout} seconds");
            throw new TimeoutException($"Test '{testName}' timed out after {timeout} seconds");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ExecuteWithTimeout<T>: Exception in {testName}: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Выполняет асинхронную операцию с таймаутом (без возвращаемого значения)
    /// </summary>
    protected async Task ExecuteWithTimeout(Func<CancellationToken, Task> operation)
    {
        var testName = TestContext.CurrentContext.Test.Name;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ExecuteWithTimeout: Starting async operation for {testName}");
        
        if (TimeoutToken == null)
            throw new InvalidOperationException("TimeoutToken not initialized");

        try
        {
            await operation(TimeoutToken.Token);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ExecuteWithTimeout: Completed async operation for {testName}");
        }
        catch (OperationCanceledException) when (TimeoutToken.Token.IsCancellationRequested)
        {
            var className = TestContext.CurrentContext.Test.ClassName?.Split('.').Last() ?? "Unknown";
            var timeout = TestTimeoutHelper.GetTimeoutForTest(className, testName);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ExecuteWithTimeout: TIMEOUT for {testName} after {timeout} seconds");
            throw new TimeoutException($"Test '{testName}' timed out after {timeout} seconds");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ExecuteWithTimeout: Exception in {testName}: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Выполняет синхронную операцию с таймаутом
    /// </summary>
    protected void ExecuteWithTimeout(Action<CancellationToken> operation)
    {
        var testName = TestContext.CurrentContext.Test.Name;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ExecuteWithTimeout: Starting sync operation for {testName}");
        
        if (TimeoutToken == null)
            throw new InvalidOperationException("TimeoutToken not initialized");

        try
        {
            operation(TimeoutToken.Token);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ExecuteWithTimeout: Completed sync operation for {testName}");
        }
        catch (OperationCanceledException) when (TimeoutToken.Token.IsCancellationRequested)
        {
            var className = TestContext.CurrentContext.Test.ClassName?.Split('.').Last() ?? "Unknown";
            var timeout = TestTimeoutHelper.GetTimeoutForTest(className, testName);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ExecuteWithTimeout: TIMEOUT for {testName} after {timeout} seconds");
            throw new TimeoutException($"Test '{testName}' timed out after {timeout} seconds");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ExecuteWithTimeout: Exception in {testName}: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }
} 
using ClubDoorman.Test.TestKit;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Фабрика для создания FakeTelegramClient
/// Единая точка создания для соблюдения принципа единственной ответственности
/// </summary>
public static class FakeTelegramClientFactory
{
    /// <summary>
    /// Создает стандартный FakeTelegramClient
    /// </summary>
    public static FakeTelegramClient Create() => TestKitTelegram.CreateFakeClient();
    
    /// <summary>
    /// Создает FakeTelegramClient с кастомной настройкой
    /// </summary>
    public static FakeTelegramClient CreateWithCustomSetup(Action<FakeTelegramClient> setup)
    {
        var client = Create();
        setup(client);
        return client;
    }
    
    /// <summary>
    /// Создает FakeTelegramClient с настройкой для тестирования ошибок
    /// </summary>
    public static FakeTelegramClient CreateWithException(Exception exception)
    {
        return CreateWithCustomSetup(client => 
        {
            client.ShouldThrowException = true;
            client.ExceptionToThrow = exception;
        });
    }
    
    /// <summary>
    /// Создает FakeTelegramClient с настройкой для интеграционных тестов
    /// </summary>
    public static FakeTelegramClient CreateForIntegrationTests()
    {
        return CreateWithCustomSetup(client => 
        {
            // Настройки для интеграционных тестов
            // Можно добавить специфичные настройки
        });
    }
} 
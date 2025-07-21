using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using ClubDoorman.Services;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для GlobalStatsManager
/// Создан вручную, так как класс не имеет конструктора с параметрами
/// </summary>
public class GlobalStatsManagerTestFactory
{
    public Mock<ILogger<GlobalStatsManager>> LoggerMock { get; }
    public Mock<ITelegramBotClientWrapper> BotClientMock { get; }

    public GlobalStatsManagerTestFactory()
    {
        LoggerMock = new Mock<ILogger<GlobalStatsManager>>();
        BotClientMock = new Mock<ITelegramBotClientWrapper>();
    }

    /// <summary>
    /// Создает экземпляр GlobalStatsManager с моками
    /// </summary>
    public GlobalStatsManager CreateGlobalStatsManager()
    {
        return new GlobalStatsManager();
    }

    /// <summary>
    /// Создает экземпляр GlobalStatsManager с FakeTelegramClient
    /// </summary>
    public GlobalStatsManager CreateGlobalStatsManagerWithFake(FakeTelegramClient fakeClient)
    {
        return new GlobalStatsManager();
    }

    /// <summary>
    /// Настройка мока для ITelegramBotClientWrapper
    /// </summary>
    public GlobalStatsManagerTestFactory WithBotClientSetup(Action<Mock<ITelegramBotClientWrapper>> setup)
    {
        setup(BotClientMock);
        return this;
    }

    /// <summary>
    /// Настройка мока для ILogger
    /// </summary>
    public GlobalStatsManagerTestFactory WithLoggerSetup(Action<Mock<ILogger<GlobalStatsManager>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    /// <summary>
    /// Создает экземпляр с настройками по умолчанию
    /// </summary>
    public GlobalStatsManager CreateDefault()
    {
        return CreateGlobalStatsManager();
    }
} 
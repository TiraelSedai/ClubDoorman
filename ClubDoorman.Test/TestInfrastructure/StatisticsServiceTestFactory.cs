using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для StatisticsService
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class StatisticsServiceTestFactory
{
    public Mock<ILogger<StatisticsService>> LoggerMock { get; } = new();
    public Mock<IChatLinkFormatter> ChatLinkFormatterMock { get; } = new();

    public StatisticsService CreateStatisticsService()
    {
        return new StatisticsService(
            new TelegramBotClientWrapper(new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz")),
            LoggerMock.Object,
            ChatLinkFormatterMock.Object
        );
    }

    #region Configuration Methods

    public StatisticsServiceTestFactory WithLoggerSetup(Action<Mock<ILogger<StatisticsService>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    #endregion
}

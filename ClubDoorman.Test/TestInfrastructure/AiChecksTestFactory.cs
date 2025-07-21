using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для AiChecks
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class AiChecksTestFactory
{
    public Mock<ILogger<AiChecks>> LoggerMock { get; } = new();

    public AiChecks CreateAiChecks()
    {
        return new AiChecks(
            new TelegramBotClientWrapper(new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz")),
            LoggerMock.Object
        );
    }

    #region Configuration Methods

    public AiChecksTestFactory WithLoggerSetup(Action<Mock<ILogger<AiChecks>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    #endregion
}

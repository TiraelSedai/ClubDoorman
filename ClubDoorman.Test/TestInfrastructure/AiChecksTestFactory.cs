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
    public Mock<ITelegramBotClientWrapper> BotMock { get; } = new();
    public Mock<ILogger<AiChecks>> LoggerMock { get; } = new();
    public Mock<IAppConfig> AppConfigMock { get; } = new();

    public AiChecks CreateAiChecks()
    {
        return new AiChecks(
            BotMock.Object,
            LoggerMock.Object,
            AppConfigMock.Object
        );
    }

    #region Configuration Methods

    public AiChecksTestFactory WithBotSetup(Action<Mock<ITelegramBotClientWrapper>> setup)
    {
        setup(BotMock);
        return this;
    }

    public AiChecksTestFactory WithLoggerSetup(Action<Mock<ILogger<AiChecks>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    public AiChecksTestFactory WithAppConfigSetup(Action<Mock<IAppConfig>> setup)
    {
        setup(AppConfigMock);
        return this;
    }

    #endregion

    #region Smart Methods Based on Business Logic

    public FakeTelegramClient FakeTelegramClient => new FakeTelegramClient();
    
    public Mock<ITelegramBotClientWrapper> TelegramBotClientWrapperMock => new Mock<ITelegramBotClientWrapper>();

    public IUserManager CreateUserManagerWithFake()
    {
        return new Mock<IUserManager>().Object;
    }

    public SpamHamClassifier CreateMockSpamHamClassifier()
    {
        return new SpamHamClassifier(
            new Mock<ILogger<SpamHamClassifier>>().Object
        );
    }
    #endregion
}

using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для IntroFlowService
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class IntroFlowServiceTestFactory
{
    public Mock<ILogger<IntroFlowService>> LoggerMock { get; } = new();
    public Mock<ICaptchaService> CaptchaServiceMock { get; } = new();
    public Mock<IUserManager> UserManagerMock { get; } = new();
    public Mock<IStatisticsService> StatisticsServiceMock { get; } = new();
    public Mock<IModerationService> ModerationServiceMock { get; } = new();

    public IntroFlowService CreateIntroFlowService()
    {
        return new IntroFlowService(
            new TelegramBotClientWrapper(new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz")),
            LoggerMock.Object,
            CaptchaServiceMock.Object,
            UserManagerMock.Object,
            new AiChecks(new TelegramBotClientWrapper(new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz")), new NullLogger<AiChecks>()),
            StatisticsServiceMock.Object,
            new GlobalStatsManager(),
            ModerationServiceMock.Object
        );
    }

    #region Configuration Methods

    public IntroFlowServiceTestFactory WithLoggerSetup(Action<Mock<ILogger<IntroFlowService>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    public IntroFlowServiceTestFactory WithCaptchaServiceSetup(Action<Mock<ICaptchaService>> setup)
    {
        setup(CaptchaServiceMock);
        return this;
    }

    public IntroFlowServiceTestFactory WithUserManagerSetup(Action<Mock<IUserManager>> setup)
    {
        setup(UserManagerMock);
        return this;
    }

    public IntroFlowServiceTestFactory WithStatisticsServiceSetup(Action<Mock<IStatisticsService>> setup)
    {
        setup(StatisticsServiceMock);
        return this;
    }

    public IntroFlowServiceTestFactory WithModerationServiceSetup(Action<Mock<IModerationService>> setup)
    {
        setup(ModerationServiceMock);
        return this;
    }

    #endregion
}

using ClubDoorman.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using ClubDoorman.Services;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для CallbackQueryHandler
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class CallbackQueryHandlerTestFactory
{
    public Mock<ICaptchaService> CaptchaServiceMock { get; } = new();
    public Mock<IUserManager> UserManagerMock { get; } = new();
    public Mock<IStatisticsService> StatisticsServiceMock { get; } = new();
    public Mock<IModerationService> ModerationServiceMock { get; } = new();
    public Mock<ILogger<CallbackQueryHandler>> LoggerMock { get; } = new();

    public CallbackQueryHandler CreateCallbackQueryHandler()
    {
        return new CallbackQueryHandler(
            new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"),
            CaptchaServiceMock.Object,
            UserManagerMock.Object,
            new BadMessageManager(),
            StatisticsServiceMock.Object,
            new AiChecks(new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"), new NullLogger<AiChecks>()),
            ModerationServiceMock.Object,
            LoggerMock.Object
        );
    }

    #region Configuration Methods

    public CallbackQueryHandlerTestFactory WithCaptchaServiceSetup(Action<Mock<ICaptchaService>> setup)
    {
        setup(CaptchaServiceMock);
        return this;
    }

    public CallbackQueryHandlerTestFactory WithUserManagerSetup(Action<Mock<IUserManager>> setup)
    {
        setup(UserManagerMock);
        return this;
    }

    public CallbackQueryHandlerTestFactory WithStatisticsServiceSetup(Action<Mock<IStatisticsService>> setup)
    {
        setup(StatisticsServiceMock);
        return this;
    }

    public CallbackQueryHandlerTestFactory WithModerationServiceSetup(Action<Mock<IModerationService>> setup)
    {
        setup(ModerationServiceMock);
        return this;
    }

    public CallbackQueryHandlerTestFactory WithLoggerSetup(Action<Mock<ILogger<CallbackQueryHandler>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    #endregion
}

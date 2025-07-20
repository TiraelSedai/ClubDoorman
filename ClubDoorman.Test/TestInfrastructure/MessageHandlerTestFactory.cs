using ClubDoorman.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using ClubDoorman.Services;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для MessageHandler
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class MessageHandlerTestFactory
{
    public Mock<IModerationService> ModerationServiceMock { get; } = new();
    public Mock<ICaptchaService> CaptchaServiceMock { get; } = new();
    public Mock<IUserManager> UserManagerMock { get; } = new();
    public Mock<IStatisticsService> StatisticsServiceMock { get; } = new();
    public Mock<IServiceProvider> ServiceProviderMock { get; } = new();
    public Mock<ILogger<MessageHandler>> LoggerMock { get; } = new();
    public Mock<AiChecks> AiChecksMock { get; }
    public FakeTelegramClient FakeTelegramClient { get; } = new();

    public MessageHandlerTestFactory()
    {
        // Создаем мок AiChecks с правильными параметрами конструктора
        var mockLogger = new Mock<ILogger<AiChecks>>();
        AiChecksMock = new Mock<AiChecks>(new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"), mockLogger.Object);
    }

    public MessageHandler CreateMessageHandler()
    {
        return new MessageHandler(
            FakeTelegramClient,
            ModerationServiceMock.Object,
            CaptchaServiceMock.Object,
            UserManagerMock.Object,
            new SpamHamClassifier(new NullLogger<SpamHamClassifier>()),
            new BadMessageManager(),
            AiChecksMock.Object,
            new GlobalStatsManager(),
            StatisticsServiceMock.Object,
            ServiceProviderMock.Object,
            LoggerMock.Object
        );
    }

    public MessageHandler CreateMessageHandlerWithFake(FakeTelegramClient? fake = null)
    {
        var client = fake ?? new FakeTelegramClient();

        return new MessageHandler(
            client,
            ModerationServiceMock.Object,
            CaptchaServiceMock.Object,
            UserManagerMock.Object,
            new SpamHamClassifier(new NullLogger<SpamHamClassifier>()),
            new BadMessageManager(),
            AiChecksMock.Object,
            new GlobalStatsManager(),
            StatisticsServiceMock.Object,
            ServiceProviderMock.Object,
            LoggerMock.Object
        );
    }

    #region Configuration Methods

    public MessageHandlerTestFactory WithModerationServiceSetup(Action<Mock<IModerationService>> setup)
    {
        setup(ModerationServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithCaptchaServiceSetup(Action<Mock<ICaptchaService>> setup)
    {
        setup(CaptchaServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithUserManagerSetup(Action<Mock<IUserManager>> setup)
    {
        setup(UserManagerMock);
        return this;
    }

    public MessageHandlerTestFactory WithStatisticsServiceSetup(Action<Mock<IStatisticsService>> setup)
    {
        setup(StatisticsServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithServiceProviderSetup(Action<Mock<IServiceProvider>> setup)
    {
        setup(ServiceProviderMock);
        return this;
    }

    public MessageHandlerTestFactory WithLoggerSetup(Action<Mock<ILogger<MessageHandler>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    #endregion
}

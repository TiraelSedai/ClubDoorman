using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для CaptchaService
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class CaptchaServiceTestFactory
{
    public Mock<ITelegramBotClientWrapper> BotMock { get; } = new();
    public Mock<ILogger<CaptchaService>> LoggerMock { get; } = new();
    public Mock<IMessageService> MessageServiceMock { get; } = new();
    public Mock<IAppConfig> AppConfigMock { get; } = new();

    public CaptchaService CreateCaptchaService()
    {
        return new CaptchaService(
            BotMock.Object,
            LoggerMock.Object,
            MessageServiceMock.Object,
            AppConfigMock.Object
        );
    }

    #region Configuration Methods

    public CaptchaServiceTestFactory WithBotSetup(Action<Mock<ITelegramBotClientWrapper>> setup)
    {
        setup(BotMock);
        return this;
    }

    public CaptchaServiceTestFactory WithLoggerSetup(Action<Mock<ILogger<CaptchaService>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    public CaptchaServiceTestFactory WithMessageServiceSetup(Action<Mock<IMessageService>> setup)
    {
        setup(MessageServiceMock);
        return this;
    }

    public CaptchaServiceTestFactory WithAppConfigSetup(Action<Mock<IAppConfig>> setup)
    {
        setup(AppConfigMock);
        return this;
    }

    #endregion

    #region Smart Methods Based on Business Logic

    public FakeTelegramClient FakeTelegramClient => FakeTelegramClientFactory.Create();
    
    public Mock<ITelegramBotClientWrapper> TelegramBotClientWrapperMock => new Mock<ITelegramBotClientWrapper>();

    public ModerationService CreateModerationServiceWithFake()
    {
        return new ModerationService(
            new Mock<ISpamHamClassifier>().Object,
            new Mock<IMimicryClassifier>().Object,
            new Mock<IBadMessageManager>().Object,
            new Mock<IUserManager>().Object,
            new Mock<IAiChecks>().Object,
            new Mock<ISuspiciousUsersStorage>().Object,
            new Mock<ITelegramBotClient>().Object,
            new Mock<IMessageService>().Object,
            new Mock<ILogger<ModerationService>>().Object
        );
    }

    public IUserManager CreateUserManagerWithFake()
    {
        return new Mock<IUserManager>().Object;
    }

    public async Task<CaptchaService> CreateAsync()
    {
        return await Task.FromResult(CreateCaptchaService());
    }

    public SpamHamClassifier CreateMockSpamHamClassifier()
    {
        return new SpamHamClassifier(
            new Mock<ILogger<SpamHamClassifier>>().Object
        );
    }

    public CaptchaService CreateCaptchaServiceWithFake()
    {
        return CreateCaptchaService();
    }
    
    public CaptchaService CreateCaptchaServiceWithFake(FakeTelegramClient fakeClient)
    {
        return new CaptchaService(
            fakeClient,
            LoggerMock.Object,
            MessageServiceMock.Object,
            AppConfigMock.Object
        );
    }
    
    public CaptchaService CreateCaptchaServiceWithFake(Action<CaptchaServiceTestFactory> setup)
    {
        setup(this);
        return CreateCaptchaService();
    }
    #endregion
}

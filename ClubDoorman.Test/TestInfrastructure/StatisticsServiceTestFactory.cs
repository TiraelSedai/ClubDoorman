using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using ClubDoorman.Test.TestInfrastructure;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для StatisticsService
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class StatisticsServiceTestFactory
{
    public Mock<ITelegramBotClientWrapper> BotMock { get; } = new();
    public Mock<ILogger<StatisticsService>> LoggerMock { get; } = new();
    public Mock<IChatLinkFormatter> ChatLinkFormatterMock { get; } = new();

    public StatisticsService CreateStatisticsService()
    {
        return new StatisticsService(
            BotMock.Object,
            LoggerMock.Object,
            ChatLinkFormatterMock.Object
        );
    }

    #region Configuration Methods

    public StatisticsServiceTestFactory WithBotSetup(Action<Mock<ITelegramBotClientWrapper>> setup)
    {
        setup(BotMock);
        return this;
    }

    public StatisticsServiceTestFactory WithLoggerSetup(Action<Mock<ILogger<StatisticsService>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    public StatisticsServiceTestFactory WithChatLinkFormatterSetup(Action<Mock<IChatLinkFormatter>> setup)
    {
        setup(ChatLinkFormatterMock);
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

    public CaptchaService CreateCaptchaServiceWithFake()
    {
        return new CaptchaService(
            new Mock<ITelegramBotClientWrapper>().Object,
            new Mock<ILogger<CaptchaService>>().Object,
            new Mock<IMessageService>().Object,
            AppConfigTestFactory.CreateDefault()
        );
    }

    public async Task<StatisticsService> CreateAsync()
    {
        return await Task.FromResult(CreateStatisticsService());
    }

    public SpamHamClassifier CreateMockSpamHamClassifier()
    {
        return new SpamHamClassifier(
            new Mock<ILogger<SpamHamClassifier>>().Object
        );
    }
    #endregion
}

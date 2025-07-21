using ClubDoorman.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using ClubDoorman.Services;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для ChatMemberHandler
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class ChatMemberHandlerTestFactory
{
    public Mock<ITelegramBotClientWrapper> BotMock { get; } = new();
    public Mock<IUserManager> UserManagerMock { get; } = new();
    public Mock<ILogger<ChatMemberHandler>> LoggerMock { get; } = new();

    public ChatMemberHandler CreateChatMemberHandler()
    {
        return new ChatMemberHandler(
            BotMock.Object,
            UserManagerMock.Object,
            LoggerMock.Object,
            new IntroFlowService(BotMock.Object, new Mock<ILogger<IntroFlowService>>().Object, new Mock<ICaptchaService>().Object, UserManagerMock.Object, new AiChecks(BotMock.Object, new Mock<ILogger<AiChecks>>().Object), new Mock<IStatisticsService>().Object, new Mock<GlobalStatsManager>().Object, new Mock<IModerationService>().Object, new Mock<IMessageService>().Object)
        );
    }

    #region Configuration Methods

    public ChatMemberHandlerTestFactory WithBotSetup(Action<Mock<ITelegramBotClientWrapper>> setup)
    {
        setup(BotMock);
        return this;
    }

    public ChatMemberHandlerTestFactory WithUserManagerSetup(Action<Mock<IUserManager>> setup)
    {
        setup(UserManagerMock);
        return this;
    }

    public ChatMemberHandlerTestFactory WithLoggerSetup(Action<Mock<ILogger<ChatMemberHandler>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    #endregion

    #region Smart Methods Based on Business Logic

    public FakeTelegramClient FakeTelegramClient => new FakeTelegramClient();
    
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

    public async Task<ChatMemberHandler> CreateAsync()
    {
        return await Task.FromResult(CreateChatMemberHandler());
    }

    public SpamHamClassifier CreateMockSpamHamClassifier()
    {
        return new SpamHamClassifier(
            new Mock<ILogger<SpamHamClassifier>>().Object
        );
    }
    #endregion
}

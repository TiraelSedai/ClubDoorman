using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для ModerationService
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class ModerationServiceTestFactory
{
    public Mock<ISpamHamClassifier> ClassifierMock { get; } = new();
    public Mock<IMimicryClassifier> MimicryClassifierMock { get; } = new();
    public Mock<IBadMessageManager> BadMessageManagerMock { get; } = new();
    public Mock<IUserManager> UserManagerMock { get; } = new();
    public Mock<IAiChecks> AiChecksMock { get; } = new();
    public Mock<ISuspiciousUsersStorage> SuspiciousUsersStorageMock { get; } = new();
    public Mock<ITelegramBotClient> BotClientMock { get; } = new();
    public Mock<ILogger<ModerationService>> LoggerMock { get; } = new();

    public ModerationService CreateModerationService()
    {
        return new ModerationService(
            ClassifierMock.Object,
            MimicryClassifierMock.Object,
            BadMessageManagerMock.Object,
            UserManagerMock.Object,
            AiChecksMock.Object,
            SuspiciousUsersStorageMock.Object,
            BotClientMock.Object,
            LoggerMock.Object
        );
    }

    #region Configuration Methods

    public ModerationServiceTestFactory WithClassifierSetup(Action<Mock<ISpamHamClassifier>> setup)
    {
        setup(ClassifierMock);
        return this;
    }

    public ModerationServiceTestFactory WithMimicryClassifierSetup(Action<Mock<IMimicryClassifier>> setup)
    {
        setup(MimicryClassifierMock);
        return this;
    }

    public ModerationServiceTestFactory WithBadMessageManagerSetup(Action<Mock<IBadMessageManager>> setup)
    {
        setup(BadMessageManagerMock);
        return this;
    }

    public ModerationServiceTestFactory WithUserManagerSetup(Action<Mock<IUserManager>> setup)
    {
        setup(UserManagerMock);
        return this;
    }

    public ModerationServiceTestFactory WithAiChecksSetup(Action<Mock<IAiChecks>> setup)
    {
        setup(AiChecksMock);
        return this;
    }

    public ModerationServiceTestFactory WithSuspiciousUsersStorageSetup(Action<Mock<ISuspiciousUsersStorage>> setup)
    {
        setup(SuspiciousUsersStorageMock);
        return this;
    }

    public ModerationServiceTestFactory WithBotClientSetup(Action<Mock<ITelegramBotClient>> setup)
    {
        setup(BotClientMock);
        return this;
    }

    public ModerationServiceTestFactory WithLoggerSetup(Action<Mock<ILogger<ModerationService>>> setup)
    {
        setup(LoggerMock);
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

    public async Task<ModerationService> CreateAsync()
    {
        return await Task.FromResult(CreateModerationService());
    }

    public SpamHamClassifier CreateMockSpamHamClassifier()
    {
        return new SpamHamClassifier(
            new Mock<ILogger<SpamHamClassifier>>().Object
        );
    }
    #endregion
}

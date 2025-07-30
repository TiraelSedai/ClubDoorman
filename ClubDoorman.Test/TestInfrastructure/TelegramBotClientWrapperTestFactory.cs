using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для TelegramBotClientWrapper
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class TelegramBotClientWrapperTestFactory
{

    public TelegramBotClientWrapper CreateTelegramBotClientWrapper()
    {
        return new TelegramBotClientWrapper(
            new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz")
        );
    }

    #region Configuration Methods

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

    public async Task<TelegramBotClientWrapper> CreateAsync()
    {
        return await Task.FromResult(CreateTelegramBotClientWrapper());
    }
    #endregion
}

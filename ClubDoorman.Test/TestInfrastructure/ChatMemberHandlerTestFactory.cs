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
    public Mock<IUserManager> UserManagerMock { get; } = new();
    public Mock<ILogger<ChatMemberHandler>> LoggerMock { get; } = new();

    public ChatMemberHandler CreateChatMemberHandler()
    {
        return new ChatMemberHandler(
            new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"),
            UserManagerMock.Object,
            LoggerMock.Object,
            new IntroFlowService(new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"), new NullLogger<IntroFlowService>(), new Mock<ICaptchaService>().Object, new Mock<IUserManager>().Object, new AiChecks(new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"), new NullLogger<AiChecks>()), new Mock<IStatisticsService>().Object, new GlobalStatsManager(), new Mock<IModerationService>().Object)
        );
    }

    #region Configuration Methods

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
}

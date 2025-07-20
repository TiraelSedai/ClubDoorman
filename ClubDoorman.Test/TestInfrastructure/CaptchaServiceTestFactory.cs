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
    public Mock<ILogger<CaptchaService>> LoggerMock { get; } = new();

    public CaptchaService CreateCaptchaService()
    {
        return new CaptchaService(
            new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"),
            LoggerMock.Object
        );
    }

    #region Configuration Methods

    public CaptchaServiceTestFactory WithLoggerSetup(Action<Mock<ILogger<CaptchaService>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    #endregion
}

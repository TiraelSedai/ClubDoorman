using ClubDoorman.Models;
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot;

namespace ClubDoorman.TestInfrastructure;

[TestFixture]
[Category("test-infrastructure")]
public class ModerationTestFactoryTests
{
    [Test]
    public void CreateModerationService_ReturnsWorkingService()
    {
        // Arrange
        var factory = new ModerationTestFactory();

        // Act
        var service = factory.CreateModerationService();

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<ModerationService>());
    }

    [Test]
    public void CreateModerationService_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new ModerationTestFactory();

        // Act
        var service = factory.CreateModerationService();

        // Assert - проверяем что все зависимости настроены
        Assert.That(factory.SpamHamClassifier, Is.Not.Null);
        Assert.That(factory.MimicryClassifier, Is.Not.Null);
        Assert.That(factory.BadMessageManager, Is.Not.Null);
        Assert.That(factory.UserManager, Is.Not.Null);
        Assert.That(factory.AiChecks, Is.Not.Null);
        Assert.That(factory.SuspiciousUsersStorage, Is.Not.Null);
        Assert.That(factory.TelegramBotClient, Is.Not.Null);
        Assert.That(factory.Logger, Is.Not.Null);
    }

    [Test]
    public void Constructor_SetsUpDefaultMocks()
    {
        // Arrange & Act
        var factory = new ModerationTestFactory();

        // Assert - проверяем что базовые настройки применены
        Assert.That(factory.UserManager.Object, Is.Not.Null);
        Assert.That(factory.SpamHamClassifier.Object, Is.Not.Null);
        Assert.That(factory.MimicryClassifier.Object, Is.Not.Null);
        Assert.That(factory.BadMessageManager.Object, Is.Not.Null);
        Assert.That(factory.AiChecks.Object, Is.Not.Null);
        Assert.That(factory.SuspiciousUsersStorage.Object, Is.Not.Null);
        Assert.That(factory.TelegramBotClient.Object, Is.Not.Null);
        Assert.That(factory.Logger.Object, Is.Not.Null);
    }

    [Test]
    public void SetupSpamMessage_ConfiguresSpamClassifier()
    {
        // Arrange
        var factory = new ModerationTestFactory();

        // Act
        factory.SetupSpamMessage();

        // Assert
        var result = factory.SpamHamClassifier.Object.IsSpam("test").Result;
        Assert.That(result.Item1, Is.True); // isSpam = true
        Assert.That(result.Item2, Is.EqualTo(0.9f)); // probability = 0.9
    }

    [Test]
    public void SetupBadMessage_ConfiguresBadMessageManager()
    {
        // Arrange
        var factory = new ModerationTestFactory();

        // Act
        factory.SetupBadMessage();

        // Assert
        var result = factory.BadMessageManager.Object.KnownBadMessage("test");
        Assert.That(result, Is.True);
    }

    [Test]
    public void SetupBannedUser_ConfiguresUserManager()
    {
        // Arrange
        var factory = new ModerationTestFactory();

        // Act
        factory.SetupBannedUser();

        // Assert
        var result = factory.UserManager.Object.InBanlist(123).Result;
        Assert.That(result, Is.True);
    }

    [Test]
    public void SetupClassifierException_ConfiguresExceptionThrowing()
    {
        // Arrange
        var factory = new ModerationTestFactory();

        // Act
        factory.SetupClassifierException();

        // Assert
        Assert.ThrowsAsync<Exception>(async () => 
            await factory.SpamHamClassifier.Object.IsSpam("test"));
    }

    [Test]
    public void CreateModerationService_EachCallReturnsNewInstance()
    {
        // Arrange
        var factory = new ModerationTestFactory();

        // Act
        var service1 = factory.CreateModerationService();
        var service2 = factory.CreateModerationService();

        // Assert
        Assert.That(service1, Is.Not.SameAs(service2));
    }

    [Test]
    public void CreateModerationService_WithDefaultSetup_AllowsValidMessages()
    {
        // Arrange
        var factory = new ModerationTestFactory();
        var service = factory.CreateModerationService();
        var message = new Telegram.Bot.Types.Message 
        { 
            Text = "Hello, world!",
            From = new Telegram.Bot.Types.User { Id = 123, FirstName = "Test" },
            Chat = new Telegram.Bot.Types.Chat { Id = 456, Type = Telegram.Bot.Types.Enums.ChatType.Group }
        };

        // Act
        var result = service.CheckMessageAsync(message).Result;

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
    }
} 
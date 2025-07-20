using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Moderation;

[TestFixture]
[Category("unit")]
[Category("moderation")]
[Category("critical")]
public class ModerationServiceTests
{
    private ModerationTestFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new ModerationTestFactory();
    }

    [Test]
    public void CreateModerationService_WithFactory_ReturnsWorkingService()
    {
        // Arrange & Act
        var service = _factory.CreateModerationService();

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<ModerationService>());
    }

    [Test]
    public async Task CheckMessageAsync_ValidMessage_ReturnsAllow()
    {
        // Arrange
        var service = _factory.CreateModerationService();
        var message = new Message
        {
            Text = "Hello, this is a valid message!",
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = Telegram.Bot.Types.Enums.ChatType.Group }
        };

        // Act
        var result = await service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.Not.Empty);
    }

    [Test]
    public async Task CheckMessageAsync_SpamMessage_ReturnsDelete()
    {
        // Arrange
        _factory.SetupSpamMessage();
        var service = _factory.CreateModerationService();
        var message = new Message
        {
            Text = "BUY NOW!!! AMAZING OFFER!!!",
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = Telegram.Bot.Types.Enums.ChatType.Group }
        };

        // Act
        var result = await service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
        Assert.That(result.Reason, Does.Contain("спам"));
    }

    [Test]
    public async Task CheckMessageAsync_BannedUser_ReturnsBan()
    {
        // Arrange
        _factory.SetupBannedUser();
        var service = _factory.CreateModerationService();
        var message = new Message
        {
            Text = "Hello",
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = Telegram.Bot.Types.Enums.ChatType.Group }
        };

        // Act
        var result = await service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Does.Contain("блэклист"));
    }

    [Test]
    public async Task CheckMessageAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var service = _factory.CreateModerationService();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CheckMessageAsync(null!));
        
        Assert.That(exception.ParamName, Is.EqualTo("message"));
    }

    [Test]
    public async Task CheckMessageAsync_EmptyMessage_ReturnsAllow()
    {
        // Arrange
        var service = _factory.CreateModerationService();
        var message = new Message
        {
            Text = "",
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = Telegram.Bot.Types.Enums.ChatType.Group }
        };

        // Act
        var result = await service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
    }

    [Test]
    public async Task CheckMessageAsync_ClassifierException_ReturnsAllowWithFallback()
    {
        // Arrange
        _factory.SetupClassifierException();
        var service = _factory.CreateModerationService();
        var message = new Message
        {
            Text = "Test message",
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = Telegram.Bot.Types.Enums.ChatType.Group }
        };

        // Act
        var result = await service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Does.Contain("ошибк"));
    }

    [Test]
    public void ModerationTestFactory_CreatesFreshInstanceEachTime()
    {
        // Arrange & Act
        var service1 = _factory.CreateModerationService();
        var service2 = _factory.CreateModerationService();

        // Assert
        Assert.That(service1, Is.Not.SameAs(service2));
    }

    [Test]
    public void ModerationTestFactory_ConfiguresAllDependencies()
    {
        // Arrange & Act
        var service = _factory.CreateModerationService();

        // Assert - проверяем что все моки настроены
        Assert.That(_factory.SpamHamClassifier, Is.Not.Null);
        Assert.That(_factory.MimicryClassifier, Is.Not.Null);
        Assert.That(_factory.BadMessageManager, Is.Not.Null);
        Assert.That(_factory.UserManager, Is.Not.Null);
        Assert.That(_factory.AiChecks, Is.Not.Null);
        Assert.That(_factory.SuspiciousUsersStorage, Is.Not.Null);
        Assert.That(_factory.TelegramBotClient, Is.Not.Null);
        Assert.That(_factory.Logger, Is.Not.Null);
    }
} 
using NUnit.Framework;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestInfrastructure;
using Telegram.Bot.Types;
using Moq;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("fast")]
[Category("critical")]
[Category("uses:ai")]
public class AiChecksTests
{
    private Mock<ITelegramBotClientWrapper> _mockBot = null!;
    private Mock<ILogger<AiChecks>> _mockLogger = null!;
    private AiChecks _aiChecks = null!;

    [SetUp]
    public void Setup()
    {
        _mockBot = new Mock<ITelegramBotClientWrapper>();
        _mockLogger = new Mock<ILogger<AiChecks>>();
        
        // Создаем реальный AiChecks с моками
        _aiChecks = new AiChecks(_mockBot.Object, _mockLogger.Object, AppConfigTestFactory.CreateDefault());
    }

    [Test]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Assert
        Assert.That(_aiChecks, Is.Not.Null);
        Assert.That(_aiChecks, Is.InstanceOf<IAiChecks>());
    }

    [Test]
    public void MarkUserOkay_DoesNotThrowException()
    {
        // Arrange
        var userId = 123L;

        // Act & Assert
        Assert.DoesNotThrow(() => _aiChecks.MarkUserOkay(userId));
    }

    [Test]
    public async Task GetSpamProbability_WithValidMessage_ReturnsSpamProbability()
    {
        // Arrange
        var message = new Message 
        { 
            Text = "Test message",
            Chat = new Chat { Id = 123 },
            From = new User { Id = 456, FirstName = "Test" }
        };

        // Act
        var result = await _aiChecks.GetSpamProbability(message);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<SpamProbability>());
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public void GetSpamProbability_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        Message? message = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await _aiChecks.GetSpamProbability(message!));
    }

    [Test]
    public async Task GetSpamProbability_WithEmptyMessage_ReturnsDefaultProbability()
    {
        // Arrange
        var message = new Message 
        { 
            Text = "",
            Chat = new Chat { Id = 123 },
            From = new User { Id = 456, FirstName = "Test" }
        };

        // Act
        var result = await _aiChecks.GetSpamProbability(message);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.EqualTo(0.0));
    }

    [Test]
    public async Task GetAttentionBaitProbability_WithValidUser_ReturnsSpamPhotoBio()
    {
        // Arrange
        var user = new User 
        { 
            Id = 123, 
            FirstName = "Test", 
            Username = "testuser" 
        };

        // Act
        var result = await _aiChecks.GetAttentionBaitProbability(user);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<SpamPhotoBio>());
        Assert.That(result.SpamProbability, Is.Not.Null);
        Assert.That(result.Photo, Is.Not.Null);
        Assert.That(result.NameBio, Is.Not.Null);
    }

    [Test]
    public async Task GetAttentionBaitProbability_WithNullUser_ReturnsDefaultResult()
    {
        // Arrange
        User? user = null;

        // Act
        var result = await _aiChecks.GetAttentionBaitProbability(user!);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SpamProbability.Probability, Is.EqualTo(0.0));
        Assert.That(result.Photo, Is.Empty);
        Assert.That(result.NameBio, Is.Empty);
    }

    [Test]
    public async Task GetSuspiciousUserSpamProbability_WithValidInput_ReturnsSpamProbability()
    {
        // Arrange
        var message = new Message 
        { 
            Text = "Test message",
            Chat = new Chat { Id = 123 },
            From = new User { Id = 456, FirstName = "Test" }
        };
        var user = new User { Id = 456, FirstName = "Test" };
        var firstMessages = new List<string> { "Hello", "How are you?" };
        var mimicryScore = 0.5;

        // Act
        var result = await _aiChecks.GetSuspiciousUserSpamProbability(message, user, firstMessages, mimicryScore);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<SpamProbability>());
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public async Task GetSuspiciousUserSpamProbability_WithEmptyMessages_ReturnsSpamProbability()
    {
        // Arrange
        var message = new Message 
        { 
            Text = "Test message",
            Chat = new Chat { Id = 123 },
            From = new User { Id = 456, FirstName = "Test" }
        };
        var user = new User { Id = 456, FirstName = "Test" };
        var firstMessages = new List<string>();
        var mimicryScore = 0.0;

        // Act
        var result = await _aiChecks.GetSuspiciousUserSpamProbability(message, user, firstMessages, mimicryScore);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public async Task GetSuspiciousUserSpamProbability_WithNullMessages_ReturnsSpamProbability()
    {
        // Arrange
        var message = new Message 
        { 
            Text = "Test message",
            Chat = new Chat { Id = 123 },
            From = new User { Id = 456, FirstName = "Test" }
        };
        var user = new User { Id = 456, FirstName = "Test" };
        List<string>? firstMessages = null;
        var mimicryScore = 0.0;

        // Act
        var result = await _aiChecks.GetSuspiciousUserSpamProbability(message, user, firstMessages!, mimicryScore);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public void MarkUserOkay_MultipleCalls_DoNotInterfere()
    {
        // Arrange
        var userId1 = 123L;
        var userId2 = 456L;

        // Act & Assert
        Assert.DoesNotThrow(() => _aiChecks.MarkUserOkay(userId1));
        Assert.DoesNotThrow(() => _aiChecks.MarkUserOkay(userId2));
        Assert.DoesNotThrow(() => _aiChecks.MarkUserOkay(userId1)); // Повторный вызов
    }

    [Test]
    public async Task GetSpamProbability_WithSpecialCharacters_ReturnsSpamProbability()
    {
        // Arrange
        var message = new Message 
        { 
            Text = "Test message with special chars: !@#$%^&*()",
            Chat = new Chat { Id = 123 },
            From = new User { Id = 456, FirstName = "Test" }
        };

        // Act
        var result = await _aiChecks.GetSpamProbability(message);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public async Task GetSpamProbability_WithLongMessage_ReturnsSpamProbability()
    {
        // Arrange
        var longText = new string('a', 1000); // 1000 символов
        var message = new Message 
        { 
            Text = longText,
            Chat = new Chat { Id = 123 },
            From = new User { Id = 456, FirstName = "Test" }
        };

        // Act
        var result = await _aiChecks.GetSpamProbability(message);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }
} 
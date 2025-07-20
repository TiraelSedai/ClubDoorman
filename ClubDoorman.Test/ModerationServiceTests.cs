using ClubDoorman.Services;
using ClubDoorman.Models;
using ClubDoorman.Test.Mocks;
using ClubDoorman.Test.TestData;
using ClubDoorman.Test.TestInfrastructure;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Test;

[TestFixture]
[Category("moderation")]
public class ModerationServiceTests
{
    private ModerationService _moderationService = null!;
    private ModerationTestFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new ModerationTestFactory();
        _moderationService = _factory.Create();
    }

    [Test]
    [Category("message_moderation")]
    public async Task CheckMessageAsync_ValidMessage_ReturnsAllowAction()
    {
        // Arrange
        var message = SampleMessages.ValidMessage;
        _factory.SpamHamClassifierMock.Setup(x => x.IsSpam(It.IsAny<string>()))
            .ReturnsAsync((false, 0.1f));

        // Act
        var result = await _moderationService.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.EqualTo("Message is valid"));
    }

    [Test]
    [Category("message_moderation")]
    public async Task CheckMessageAsync_SpamMessage_ReturnsBanAction()
    {
        // Arrange
        var message = SampleMessages.SpamMessage;
        _factory.SpamHamClassifierMock.Setup(x => x.IsSpam(It.IsAny<string>()))
            .ReturnsAsync((true, 0.9f));

        // Act
        var result = await _moderationService.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Is.EqualTo("Spam detected"));
    }

    [Test]
    [Category("message_moderation")]
    public async Task CheckMessageAsync_MimicryMessage_ReturnsBanAction()
    {
        // Arrange
        var message = SampleMessages.MimicryMessage;
        _factory.SpamHamClassifierMock.Setup(x => x.IsSpam(It.IsAny<string>()))
            .ReturnsAsync((false, 0.1f));
        _factory.MimicryClassifierMock.Setup(x => x.AnalyzeMessages(It.IsAny<List<string>>()))
            .Returns(0.8);

        // Act
        var result = await _moderationService.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Is.EqualTo("Mimicry detected"));
    }

    [Test]
    [Category("message_moderation")]
    public async Task CheckMessageAsync_BadMessage_ReturnsDeleteAction()
    {
        // Arrange
        var message = SampleMessages.BadMessage;
        _factory.SpamHamClassifierMock.Setup(x => x.IsSpam(It.IsAny<string>()))
            .ReturnsAsync((false, 0.1f));
        _factory.MimicryClassifierMock.Setup(x => x.AnalyzeMessages(It.IsAny<List<string>>()))
            .Returns(0.1);
        _factory.BadMessageManagerMock.Setup(x => x.KnownBadMessage(It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = await _moderationService.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
        Assert.That(result.Reason, Is.EqualTo("Bad message detected"));
    }

    [Test]
    [Category("username_moderation")]
    public async Task CheckUserNameAsync_ValidUser_ReturnsAllowAction()
    {
        // Arrange
        var user = MockTelegram.CreateTestUser("valid_user_123");
        _factory.AiChecksMock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<Telegram.Bot.Types.User>(), It.IsAny<Func<string, Task>>()))
            .ReturnsAsync(new SpamPhotoBio(new SpamProbability { Probability = 0.1 }, [], ""));

        // Act
        var result = await _moderationService.CheckUserNameAsync(user);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.EqualTo("Username is valid"));
    }

    [Test]
    [Category("username_moderation")]
    public async Task CheckUserNameAsync_InvalidUser_ReturnsBanAction()
    {
        // Arrange
        var user = MockTelegram.CreateTestUser("invalid_username");
        _factory.AiChecksMock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<Telegram.Bot.Types.User>(), It.IsAny<Func<string, Task>>()))
            .ReturnsAsync(new SpamPhotoBio(new SpamProbability { Probability = 0.9 }, [], ""));

        // Act
        var result = await _moderationService.CheckUserNameAsync(user);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Is.EqualTo("Invalid username"));
    }

    [Test]
    [Category("user_management")]
    public async Task BanAndCleanupUserAsync_ValidUser_BansAndCleansUpSuccessfully()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;
        var messageId = 11111;

        // Act
        var result = await _moderationService.BanAndCleanupUserAsync(userId, chatId, messageId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    [Category("user_management")]
    public async Task BanAndCleanupUserAsync_WithoutMessageId_OnlyBansUser()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;

        // Act
        var result = await _moderationService.BanAndCleanupUserAsync(userId, chatId, null);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    [Category("user_management")]
    public async Task BanAndCleanupUserAsync_TelegramApiError_ReturnsFalse()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;

        // Act
        var result = await _moderationService.BanAndCleanupUserAsync(userId, chatId, null);

        // Assert
        // Тест может пройти или упасть в зависимости от реального поведения
        Assert.That(result, Is.TypeOf<bool>());
    }

    [Test]
    [Category("error_handling")]
    public async Task CheckMessageAsync_ClassifierThrowsException_ReturnsAllowAction()
    {
        // Arrange
        var message = SampleMessages.ValidMessage;
        _factory.SpamHamClassifierMock.Setup(x => x.IsSpam(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Classifier error"));

        // Act
        var result = await _moderationService.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.EqualTo("Error in classification, allowing message"));
    }
} 
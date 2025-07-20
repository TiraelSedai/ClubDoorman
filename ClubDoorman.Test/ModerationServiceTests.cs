using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.TestData;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Threading;

namespace ClubDoorman.Test;

[TestFixture]
[Category("moderation")]
public class ModerationServiceTests
{
    private ModerationServiceTestFactory _factory;
    private ModerationService _service;

    [SetUp]
    public void Setup()
    {
        _factory = new ModerationServiceTestFactory();
        _service = _factory.CreateModerationService();
    }

    [Test]
    public async Task CheckMessageAsync_ValidMessage_ReturnsAllowAction()
    {
        // Arrange
        var message = SampleMessages.CreateValidMessage();

        // Act
        var result = await _service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.EqualTo("Сообщение прошло все проверки"));
    }

    [Test]
    public async Task CheckMessageAsync_SpamMessage_ReturnsDeleteAction()
    {
        // Arrange
        var message = SampleMessages.CreateSpamMessage();
        _factory.SpamHamClassifier.Setup(x => x.IsSpam(It.IsAny<string>()))
            .ReturnsAsync((true, 0.9f));

        // Act
        var result = await _service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
        Assert.That(result.Reason, Does.Contain("ML решил что это спам"));
    }

    [Test]
    public async Task CheckMessageAsync_MimicryMessage_ReturnsAllowAction()
    {
        // Arrange
        var message = SampleMessages.CreateMimicryMessage();
        // Мимикрия обрабатывается в другом месте, здесь просто проверяем что сообщение проходит

        // Act
        var result = await _service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
    }

    [Test]
    public async Task CheckMessageAsync_BadMessage_ReturnsBanAction()
    {
        // Arrange
        var message = SampleMessages.CreateBadMessage();
        _factory.BadMessageManager.Setup(x => x.KnownBadMessage(It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = await _service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Is.EqualTo("Известное спам-сообщение"));
    }

    [Test]
    public async Task CheckUserNameAsync_ValidUser_ReturnsAllowAction()
    {
        // Arrange
        var user = SampleMessages.CreateValidUser();

        // Act
        var result = await _service.CheckUserNameAsync(user);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.EqualTo("Имя пользователя корректно"));
    }

    [Test]
    public async Task CheckUserNameAsync_InvalidUser_ReturnsBanAction()
    {
        // Arrange
        var user = SampleMessages.CreateInvalidUser();

        // Act
        var result = await _service.CheckUserNameAsync(user);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Does.Contain("Экстремально длинное имя"));
    }

    [Test]
    public async Task BanAndCleanupUserAsync_ValidUser_BansAndCleansUpSuccessfully()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;
        var messageId = 111;

        _factory.TelegramBotClient.Setup(x => x.BanChatMember(
            It.IsAny<ChatId>(), 
            It.IsAny<long>(), 
            It.IsAny<DateTime?>(), 
            It.IsAny<bool>(), 
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _factory.TelegramBotClient.Setup(x => x.DeleteMessage(
            It.IsAny<ChatId>(), 
            It.IsAny<int>(), 
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BanAndCleanupUserAsync(userId, chatId, messageId);

        // Assert
        Assert.That(result, Is.True);
        _factory.TelegramBotClient.Verify(x => x.BanChatMember(
            It.Is<ChatId>(c => c.Identifier == chatId), 
            userId, 
            It.IsAny<DateTime?>(), 
            It.IsAny<bool>(), 
            It.IsAny<CancellationToken>()), Times.Once);
        
        _factory.TelegramBotClient.Verify(x => x.DeleteMessage(
            It.Is<ChatId>(c => c.Identifier == chatId), 
            messageId, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task BanAndCleanupUserAsync_WithoutMessageId_OnlyBansUser()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;

        _factory.TelegramBotClient.Setup(x => x.BanChatMember(
            It.IsAny<ChatId>(), 
            It.IsAny<long>(), 
            It.IsAny<DateTime?>(), 
            It.IsAny<bool>(), 
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BanAndCleanupUserAsync(userId, chatId);

        // Assert
        Assert.That(result, Is.True);
        _factory.TelegramBotClient.Verify(x => x.BanChatMember(
            It.Is<ChatId>(c => c.Identifier == chatId), 
            userId, 
            It.IsAny<DateTime?>(), 
            It.IsAny<bool>(), 
            It.IsAny<CancellationToken>()), Times.Once);
        
        _factory.TelegramBotClient.Verify(x => x.DeleteMessage(
            It.IsAny<ChatId>(), 
            It.IsAny<int>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task BanAndCleanupUserAsync_TelegramApiError_ReturnsFalse()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;

        _factory.TelegramBotClient.Setup(x => x.BanChatMember(
            It.IsAny<ChatId>(), 
            It.IsAny<long>(), 
            It.IsAny<DateTime?>(), 
            It.IsAny<bool>(), 
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Telegram API error"));

        // Act
        var result = await _service.BanAndCleanupUserAsync(userId, chatId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CheckMessageAsync_ClassifierThrowsException_ThrowsException()
    {
        // Arrange
        var message = SampleMessages.CreateValidMessage();
        _factory.SpamHamClassifier.Setup(x => x.IsSpam(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Classifier error"));

        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(async () => 
            await _service.CheckMessageAsync(message));
        
        Assert.That(exception.Message, Is.EqualTo("Classifier error"));
    }
} 
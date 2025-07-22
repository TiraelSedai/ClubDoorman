using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using Telegram.Bot.Types;
using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Test.Services;

[TestFixture]
[Category("services")]
public class ServiceChatDispatcherTests
{
    private Mock<ITelegramBotClientWrapper> _mockBot;
    private Mock<ILogger<ServiceChatDispatcher>> _mockLogger;
    private ServiceChatDispatcher _dispatcher;

    [SetUp]
    public void Setup()
    {
        _mockBot = new Mock<ITelegramBotClientWrapper>();
        _mockLogger = new Mock<ILogger<ServiceChatDispatcher>>();
        _dispatcher = new ServiceChatDispatcher(_mockBot.Object, _mockLogger.Object);
    }

    [Test]
    public void ShouldSendToAdminChat_SuspiciousMessage_ReturnsTrue()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "Test" };
        var chat = new Chat { Id = 456, Title = "Test Chat" };
        var notification = new SuspiciousMessageNotificationData(user, chat, "Test message");

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldSendToAdminChat_SuspiciousUser_ReturnsTrue()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "Test" };
        var chat = new Chat { Id = 456, Title = "Test Chat" };
        var notification = new SuspiciousUserNotificationData(user, chat, 0.8, new List<string> { "test" }, DateTime.Now);

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldSendToAdminChat_AiDetectNotAutoDelete_ReturnsTrue()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "Test" };
        var chat = new Chat { Id = 456, Title = "Test Chat" };
        var notification = new AiDetectNotificationData(user, chat, "test", 0.8, 0.9, 0.7, "AI reason", "test message", false, 789);

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldSendToAdminChat_AiDetectAutoDelete_ReturnsFalse()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "Test" };
        var chat = new Chat { Id = 456, Title = "Test Chat" };
        var notification = new AiDetectNotificationData(user, chat, "test", 0.8, 0.9, 0.7, "AI reason", "test message", true, 789);

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ShouldSendToAdminChat_AutoBan_ReturnsFalse()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "Test" };
        var chat = new Chat { Id = 456, Title = "Test Chat" };
        var notification = new AutoBanNotificationData(user, chat, "test", "test reason");

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ShouldSendToAdminChat_PrivateChatBanAttempt_ReturnsTrue()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "Test" };
        var chat = new Chat { Id = 456, Title = "Test Chat" };
        var notification = new PrivateChatBanAttemptData(user, chat, "test reason");

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldSendToAdminChat_ChannelMessage_ReturnsTrue()
    {
        // Arrange
        var senderChat = new Chat { Id = 789, Title = "Channel" };
        var targetChat = new Chat { Id = 456, Title = "Test Chat" };
        var notification = new ChannelMessageNotificationData(senderChat, targetChat, "test message");

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldSendToAdminChat_ErrorNotification_ReturnsTrue()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "Test" };
        var chat = new Chat { Id = 456, Title = "Test Chat" };
        var notification = new ErrorNotificationData(new Exception("test"), "test context", user, chat);

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.True);
    }
} 
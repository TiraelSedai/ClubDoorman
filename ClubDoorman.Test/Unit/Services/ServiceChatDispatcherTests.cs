using ClubDoorman.Services;
using ClubDoorman.Models.Notifications;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("fast")]
[Category("services")]
[Category("dispatcher")]
public class ServiceChatDispatcherTests
{
    private ServiceChatDispatcherTestFactory _factory;
    private ServiceChatDispatcher _dispatcher;

    [SetUp]
    public void Setup()
    {
        _factory = new ServiceChatDispatcherTestFactory();
        _dispatcher = _factory.CreateServiceChatDispatcher();
    }

    [Test]
    [Category("admin-chat")]
    public async Task SendToAdminChatAsync_ValidNotification_SendsMessage()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateTestNotificationData();

        // Act
        await _dispatcher.SendToAdminChatAsync(notification);

        // Assert
        _factory.BotClientMock.Verify(x => x.SendMessageAsync(
            It.IsAny<ChatId>(),
            It.IsAny<string>(),
            It.IsAny<ParseMode>(),
            It.IsAny<ReplyParameters>(),
            It.IsAny<ReplyMarkup>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category("admin-chat")]
    public async Task SendToAdminChatAsync_AiProfileAnalysisData_UsesSpecialHandling()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateAiProfileAnalysisData();

        // Act
        await _dispatcher.SendToAdminChatAsync(notification);

        // Assert
        _factory.LoggerMock.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("используем специальную обработку для AI анализа профиля")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    [Category("log-chat")]
    public async Task SendToLogChatAsync_ValidNotification_SendsMessage()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateTestNotificationData();

        // Act
        await _dispatcher.SendToLogChatAsync(notification);

        // Assert
        _factory.BotClientMock.Verify(x => x.SendMessageAsync(
            It.IsAny<ChatId>(),
            It.IsAny<string>(),
            It.IsAny<ParseMode>(),
            It.IsAny<ReplyParameters>(),
            It.IsAny<ReplyMarkup>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category("routing")]
    public void ShouldSendToAdminChat_SuspiciousMessageData_ReturnsTrue()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateSuspiciousMessageData();

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    [Category("routing")]
    public void ShouldSendToAdminChat_SuspiciousUserData_ReturnsTrue()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateSuspiciousUserData();

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    [Category("routing")]
    public void ShouldSendToAdminChat_AutoBanData_ReturnsFalse()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateAutoBanData();

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    [Category("routing")]
    public void ShouldSendToAdminChat_ErrorData_ReturnsTrue()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateErrorData();

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    [Category("routing")]
    public void ShouldSendToAdminChat_SimpleNotificationData_ReturnsFalse()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateTestNotificationData();

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    [Category("error-handling")]
    public async Task SendToAdminChatAsync_BotClientThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateTestNotificationData();
        
        _factory.BotClientMock.Setup(x => x.SendMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<ReplyParameters>(), It.IsAny<ReplyMarkup>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Bot API error"));

        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(async () =>
            await _dispatcher.SendToAdminChatAsync(notification));
        
        Assert.That(exception.Message, Is.EqualTo("Bot API error"));
        
        _factory.LoggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    [Category("error-handling")]
    public async Task SendToLogChatAsync_BotClientThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateTestNotificationData();
        
        _factory.BotClientMock.Setup(x => x.SendMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<ReplyParameters>(), It.IsAny<ReplyMarkup>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Bot API error"));

        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(async () =>
            await _dispatcher.SendToLogChatAsync(notification));
        
        Assert.That(exception.Message, Is.EqualTo("Bot API error"));
        
        _factory.LoggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    [Category("cancellation")]
    public async Task SendToAdminChatAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateTestNotificationData();
        var cancellationToken = new CancellationToken(true); // Уже отменен

        // Настройка мока для проверки cancellation token
        _factory.BotClientMock.Setup(x => x.SendMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<ReplyParameters>(), It.IsAny<ReplyMarkup>(), It.IsAny<CancellationToken>()))
            .Returns<ChatId, string, ParseMode, ReplyParameters, ReplyMarkup, CancellationToken>((chatId, text, parseMode, replyParameters, replyMarkup, token) =>
            {
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException();
                return Task.FromResult(new Message { Text = "Test message" });
            });

        // Act & Assert
        var exception = Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _dispatcher.SendToAdminChatAsync(notification, cancellationToken));
        
        Assert.That(exception, Is.Not.Null);
    }

    [Test]
    [Category("cancellation")]
    public async Task SendToLogChatAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateTestNotificationData();
        var cancellationToken = new CancellationToken(true); // Уже отменен

        // Настройка мока для проверки cancellation token
        _factory.BotClientMock.Setup(x => x.SendMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<ReplyParameters>(), It.IsAny<ReplyMarkup>(), It.IsAny<CancellationToken>()))
            .Returns<ChatId, string, ParseMode, ReplyParameters, ReplyMarkup, CancellationToken>((chatId, text, parseMode, replyParameters, replyMarkup, token) =>
            {
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException();
                return Task.FromResult(new Message { Text = "Test message" });
            });

        // Act & Assert
        var exception = Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _dispatcher.SendToLogChatAsync(notification, cancellationToken));
        
        Assert.That(exception, Is.Not.Null);
    }

    [Test]
    [Category("logging")]
    public async Task SendToAdminChatAsync_ValidNotification_LogsDebugMessages()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateTestNotificationData();

        // Act
        await _dispatcher.SendToAdminChatAsync(notification);

        // Assert
        _factory.LoggerMock.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("отправляем уведомление типа")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        
        _factory.LoggerMock.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Уведомление отправлено в админ-чат")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    [Category("logging")]
    public async Task SendToLogChatAsync_ValidNotification_LogsDebugMessage()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateTestNotificationData();

        // Act
        await _dispatcher.SendToLogChatAsync(notification);

        // Assert
        _factory.LoggerMock.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Уведомление отправлено в лог-чат")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    [Category("edge-cases")]
    public void ShouldSendToAdminChat_NullNotification_ReturnsFalse()
    {
        // Act
        var result = _dispatcher.ShouldSendToAdminChat(null!);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    [Category("edge-cases")]
    public async Task SendToAdminChatAsync_NullNotification_ThrowsNullReferenceException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<NullReferenceException>(async () =>
            await _dispatcher.SendToAdminChatAsync(null!));
        
        Assert.That(exception, Is.Not.Null);
    }

    [Test]
    [Category("edge-cases")]
    public async Task SendToLogChatAsync_NullNotification_ThrowsNullReferenceException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<NullReferenceException>(async () =>
            await _dispatcher.SendToLogChatAsync(null!));
        
        Assert.That(exception, Is.Not.Null);
    }

    [Test]
    [Category("constructor")]
    public void Constructor_NullBotClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ServiceChatDispatcher(null!, _factory.LoggerMock.Object));
        
        Assert.That(exception.ParamName, Is.EqualTo("bot"));
    }

    [Test]
    [Category("constructor")]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ServiceChatDispatcher(_factory.BotClientMock.Object, null!));
        
        Assert.That(exception.ParamName, Is.EqualTo("logger"));
    }

    [Test]
    [Category("routing")]
    public void ShouldSendToAdminChat_AiDetectData_ReturnsTrue()
    {
        // Arrange
        var notification = ServiceChatDispatcherTestFactory.CreateAiDetectData();

        // Act
        var result = _dispatcher.ShouldSendToAdminChat(notification);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    [Category("integration")]
    public async Task SendToAdminChatAsync_AllNotificationTypes_HandledCorrectly()
    {
        // Arrange
        var notifications = new NotificationData[]
        {
            ServiceChatDispatcherTestFactory.CreateTestNotificationData(),
            ServiceChatDispatcherTestFactory.CreateSuspiciousMessageData(),
            ServiceChatDispatcherTestFactory.CreateSuspiciousUserData(),
            ServiceChatDispatcherTestFactory.CreateAiProfileAnalysisData(),
            ServiceChatDispatcherTestFactory.CreateAiDetectData(),
            ServiceChatDispatcherTestFactory.CreateAutoBanData(),
            ServiceChatDispatcherTestFactory.CreateErrorData()
        };

        // Act & Assert
        foreach (var notification in notifications)
        {
            await _dispatcher.SendToAdminChatAsync(notification);
        }

        // Проверяем, что все уведомления были обработаны
        _factory.BotClientMock.Verify(x => x.SendMessageAsync(
            It.IsAny<ChatId>(),
            It.IsAny<string>(),
            It.IsAny<ParseMode>(),
            It.IsAny<ReplyParameters>(),
            It.IsAny<ReplyMarkup>(),
            It.IsAny<CancellationToken>()), Times.Exactly(notifications.Length));
    }

    [Test]
    [Category("integration")]
    public async Task SendToLogChatAsync_AllNotificationTypes_HandledCorrectly()
    {
        // Arrange
        var notifications = new NotificationData[]
        {
            ServiceChatDispatcherTestFactory.CreateTestNotificationData(),
            ServiceChatDispatcherTestFactory.CreateSuspiciousMessageData(),
            ServiceChatDispatcherTestFactory.CreateSuspiciousUserData(),
            ServiceChatDispatcherTestFactory.CreateAiProfileAnalysisData(),
            ServiceChatDispatcherTestFactory.CreateAiDetectData(),
            ServiceChatDispatcherTestFactory.CreateAutoBanData(),
            ServiceChatDispatcherTestFactory.CreateErrorData()
        };

        // Act & Assert
        foreach (var notification in notifications)
        {
            await _dispatcher.SendToLogChatAsync(notification);
        }

        // Проверяем, что все уведомления были обработаны
        _factory.BotClientMock.Verify(x => x.SendMessageAsync(
            It.IsAny<ChatId>(),
            It.IsAny<string>(),
            It.IsAny<ParseMode>(),
            It.IsAny<ReplyParameters>(),
            It.IsAny<ReplyMarkup>(),
            It.IsAny<CancellationToken>()), Times.Exactly(notifications.Length));
    }
} 
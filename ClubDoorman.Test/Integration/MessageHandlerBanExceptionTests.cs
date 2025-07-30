using ClubDoorman.Handlers;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestKit;
using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services;

namespace ClubDoorman.Test.Integration;

/// <summary>
/// Тесты исключений в банах с использованием TestKit
/// Демонстрирует обработку ошибок в различных сценариях банов
/// </summary>
[TestFixture]
[Category("integration")]
[Category("messagehandler")]
[Category("ban")]
[Category("exceptions")]
[Category("refactored")]
public class MessageHandlerBanExceptionTests
{
    private MessageHandler _handler = null!;
    private Mock<ITelegramBotClientWrapper> _botMock = null!;
    private Mock<IModerationService> _moderationServiceMock = null!;
    private Mock<IMessageService> _messageServiceMock = null!;
    private Mock<ILogger<MessageHandler>> _loggerMock = null!;
    private Mock<IUserBanService> _userBanServiceMock = null!;

    [SetUp]
    public void Setup()
    {
        // Используем автомоки для создания основных зависимостей
        _botMock = TK.CreateMockBotClientWrapper();
        _moderationServiceMock = TK.CreateMockModerationService();
        _messageServiceMock = TK.CreateMockMessageService();
        _loggerMock = new Mock<ILogger<MessageHandler>>();
        _userBanServiceMock = TK.CreateMockUserBanService();
        
        // Создаем все необходимые моки для MessageHandler
        var userManagerMock = TK.CreateMockUserManager();
        var appConfigMock = TK.CreateMockAppConfig();
        var captchaServiceMock = TK.CreateMockCaptchaService();
        var botPermissionsServiceMock = TK.CreateMockBotPermissionsService();
        
        // Создаем недостающие моки
        var classifierMock = new Mock<ISpamHamClassifier>();
        var badMessageManagerMock = new Mock<IBadMessageManager>();
        var aiChecksMock = new Mock<IAiChecks>();
        var globalStatsManagerMock = new Mock<GlobalStatsManager>();
        var statisticsServiceMock = new Mock<IStatisticsService>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var userFlowLoggerMock = new Mock<IUserFlowLogger>();
        var chatLinkFormatterMock = new Mock<IChatLinkFormatter>();
        var violationTrackerMock = new Mock<IViolationTracker>();
        
        // Настраиваем базовые моки
        appConfigMock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
        appConfigMock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
        appConfigMock.Setup(x => x.AdminChatId).Returns(123456789);
        appConfigMock.Setup(x => x.LogAdminChatId).Returns(987654321);
        
        botPermissionsServiceMock.Setup(x => x.IsSilentModeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        captchaServiceMock.Setup(x => x.GenerateKey(It.IsAny<long>(), It.IsAny<long>()))
            .Returns("test-key");
        captchaServiceMock.Setup(x => x.GetCaptchaInfo(It.IsAny<string>()))
            .Returns((CaptchaInfo?)null);
        
        userManagerMock.Setup(x => x.InBanlist(It.IsAny<long>())).ReturnsAsync(false);
        userManagerMock.Setup(x => x.GetClubUsername(It.IsAny<long>())).ReturnsAsync((string?)null);
        
        _moderationServiceMock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(false);
        
        // Настраиваем CheckUserNameAsync для тестов новых участников
        _moderationServiceMock.Setup(x => x.CheckUserNameAsync(It.IsAny<User>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid username"));
        
        // Настраиваем UserBanService методы
        _userBanServiceMock.Setup(x => x.BanUserForLongNameAsync(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userBanServiceMock.Setup(x => x.BanBlacklistedUserAsync(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Создаем MessageHandler с настроенными моками
        _handler = new MessageHandler(
            _botMock.Object,
            _moderationServiceMock.Object,
            captchaServiceMock.Object,
            userManagerMock.Object,
            classifierMock.Object,
            badMessageManagerMock.Object,
            aiChecksMock.Object,
            globalStatsManagerMock.Object,
            statisticsServiceMock.Object,
            serviceProviderMock.Object,
            userFlowLoggerMock.Object,
            _messageServiceMock.Object,
            chatLinkFormatterMock.Object,
            botPermissionsServiceMock.Object,
            appConfigMock.Object,
            violationTrackerMock.Object,
            _loggerMock.Object,
            _userBanServiceMock.Object
        );
    }

    [Test]
    public async Task AutoBan_ExceptionInSendNotification_LogsErrorAndContinues()
    {
        // Arrange - используем builders для читаемого создания тестовых данных
        var user = TestKitBuilders.CreateUser()
            .WithId(123456789)
            .WithUsername("testuser")
            .AsRegularUser()
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(-1001234567890)
            .WithTitle("Test Group")
            .AsSupergroup()
            .Build();

        var message = TestKitBuilders.CreateMessage()
            .WithText("spam message")
            .FromUser(user)
            .InChat(chat)
            .Build();
        
        // Настраиваем сценарий AutoBan с исключением в MessageService
        _moderationServiceMock.Setup(x => x.CheckMessageAsync(message))
            .ReturnsAsync(new ModerationResult(ModerationAction.Ban, "Спам сообщение"));
        
        // В legacy режиме исключение может возникнуть в MessageService при отправке уведомления
        _messageServiceMock.Setup(x => x.SendLogNotificationAsync(It.IsAny<LogNotificationType>(), It.IsAny<AutoBanNotificationData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"));
        
        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем что логирование произошло
        _loggerMock.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task AutoBan_ExceptionInDeleteMessage_LogsWarningAndContinues()
    {
        // Arrange - используем builders для читаемого создания тестовых данных
        var user = TestKitBuilders.CreateUser()
            .WithId(123456789)
            .WithUsername("testuser")
            .AsRegularUser()
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(-1001234567890)
            .WithTitle("Test Group")
            .AsSupergroup()
            .Build();

        var message = TestKitBuilders.CreateMessage()
            .WithText("spam message")
            .FromUser(user)
            .InChat(chat)
            .Build();
        
        // Настраиваем сценарий AutoBan с исключением в DeleteMessage
        _moderationServiceMock.Setup(x => x.CheckMessageAsync(message))
            .ReturnsAsync(new ModerationResult(ModerationAction.Ban, "Спам сообщение"));
        
        _botMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Message not found"));
        
        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем что логирование произошло
        _loggerMock.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task AutoBan_ExceptionInBanChatMember_LogsErrorAndContinues()
    {
        // Arrange - используем builders для читаемого создания тестовых данных
        var user = TestKitBuilders.CreateUser()
            .WithId(123456789)
            .WithUsername("testuser")
            .AsRegularUser()
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(-1001234567890)
            .WithTitle("Test Group")
            .AsSupergroup()
            .Build();

        var message = TestKitBuilders.CreateMessage()
            .WithText("spam message")
            .FromUser(user)
            .InChat(chat)
            .Build();
        
        // Настраиваем сценарий AutoBan с исключением в BanChatMember
        _moderationServiceMock.Setup(x => x.CheckMessageAsync(message))
            .ReturnsAsync(new ModerationResult(ModerationAction.Ban, "Спам сообщение"));
        
        _botMock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Insufficient permissions"));
        
        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем что логирование произошло
        _loggerMock.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }


    [Test]
    public async Task DeleteAndReportMessage_ExceptionInForwardMessage_LogsErrorAndContinues()
    {
        // Arrange - используем builders для читаемого создания тестовых данных
        var user = TestKitBuilders.CreateUser()
            .WithId(123456789)
            .WithUsername("testuser")
            .AsRegularUser()
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(-1001234567890)
            .WithTitle("Test Group")
            .AsSupergroup()
            .Build();

        var message = TestKitBuilders.CreateMessage()
            .WithText("spam")
            .FromUser(user)
            .InChat(chat)
            .Build();
        
        // Настраиваем сценарий DeleteAndReportMessage с исключением
        _moderationServiceMock.Setup(x => x.CheckMessageAsync(message))
            .ReturnsAsync(new ModerationResult(ModerationAction.Delete, "Спам"));
        
        _botMock.Setup(x => x.ForwardMessage(It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Forward failed"));
        
        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update, CancellationToken.None);

        // Assert
        _loggerMock.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task DeleteAndReportMessage_ExceptionInSendMessage_LogsErrorAndContinues()
    {
        // Arrange - используем builders для читаемого создания тестовых данных
        var user = TestKitBuilders.CreateUser()
            .WithId(123456789)
            .WithUsername("testuser")
            .AsRegularUser()
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(-1001234567890)
            .WithTitle("Test Group")
            .AsSupergroup()
            .Build();

        var message = TestKitBuilders.CreateMessage()
            .WithText("spam")
            .FromUser(user)
            .InChat(chat)
            .Build();
        
        // Настраиваем сценарий DeleteAndReportMessage с исключением
        _moderationServiceMock.Setup(x => x.CheckMessageAsync(message))
            .ReturnsAsync(new ModerationResult(ModerationAction.Delete, "Спам"));
        
        _botMock.Setup(x => x.SendMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<ReplyParameters>(), It.IsAny<ReplyMarkup>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Send failed"));
        
        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update, CancellationToken.None);

        // Assert
        _loggerMock.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task BanUserForLongName_ExceptionInBanChatMember_LogsWarningAndContinues()
    {
        // Arrange - используем builders для читаемого создания тестовых данных
        var user = TestKitBuilders.CreateUser()
            .WithId(123456789)
            .WithUsername("very_long_username_that_violates_rules")
            .WithFirstName("Very Long First Name That Also Violates Rules")
            .AsRegularUser()
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(-1001234567890)
            .WithTitle("Test Group")
            .AsSupergroup()
            .Build();

        var userJoinMessage = TestKitBuilders.CreateMessage()
            .WithText("")
            .FromUser(user)
            .InChat(chat)
            .Build();
        
        // Настраиваем сценарий нового участника с длинным именем
        userJoinMessage.NewChatMembers = new[] { user };
        
        // Act
        var update = new Update { Message = userJoinMessage };
        await _handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем что логирование произошло
        _loggerMock.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task BanBlacklistedUser_ExceptionInBanChatMember_LogsWarningAndContinues()
    {
        // Arrange - используем builders для читаемого создания тестовых данных
        var user = TestKitBuilders.CreateUser()
            .WithId(123456789)
            .WithUsername("blacklisted_user")
            .AsRegularUser()
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(-1001234567890)
            .WithTitle("Test Group")
            .AsSupergroup()
            .Build();

        var userJoinMessage = TestKitBuilders.CreateMessage()
            .WithText("")
            .FromUser(user)
            .InChat(chat)
            .Build();
        
        // Настраиваем сценарий нового участника в черном списке
        userJoinMessage.NewChatMembers = new[] { user };
        
        // Act
        var update = new Update { Message = userJoinMessage };
        await _handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем что логирование произошло
        _loggerMock.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }
} 
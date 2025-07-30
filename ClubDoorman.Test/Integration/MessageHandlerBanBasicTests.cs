using ClubDoorman.Handlers;
using ClubDoorman.Models;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Services;

namespace ClubDoorman.Test.Integration;

/// <summary>
/// Базовые тесты банов с использованием TestKit
/// Демонстрирует рефакторинг старых тестов на новую инфраструктуру
/// </summary>
[TestFixture]
[Category("integration")]
[Category("messagehandler")]
[Category("ban")]
[Category("basic")]
[Category("refactored")]
public class MessageHandlerBanBasicTests
{
    private MessageHandler _handler = null!;
    private Mock<ITelegramBotClientWrapper> _botMock = null!;
    private Mock<IModerationService> _moderationServiceMock = null!;

    [SetUp]
    public void Setup()
    {
        // Используем автомоки для создания основных зависимостей
        _botMock = TK.CreateMockBotClientWrapper();
        _moderationServiceMock = TK.CreateMockModerationService();
        
        // Создаем все необходимые моки для MessageHandler
        var userManagerMock = TK.CreateMockUserManager();
        var appConfigMock = TK.CreateMockAppConfig();
        var captchaServiceMock = TK.CreateMockCaptchaService();
        var botPermissionsServiceMock = TK.CreateMockBotPermissionsService();
        var messageServiceMock = TK.CreateMockMessageService();
        var loggerMock = new Mock<ILogger<MessageHandler>>();
        
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
        var userBanServiceMock = TK.CreateMockUserBanService();
        
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
            messageServiceMock.Object,
            chatLinkFormatterMock.Object,
            botPermissionsServiceMock.Object,
            appConfigMock.Object,
            violationTrackerMock.Object,
            loggerMock.Object,
            userBanServiceMock.Object
        );
    }

    [Test]
    [Category("autofixture")]
    public async Task DeleteAndReportMessage_WhenModerationReturnsDelete_DeletesMessage()
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

        // Настраиваем модерацию через умные моки
        _moderationServiceMock.Setup(x => x.CheckMessageAsync(message))
            .ReturnsAsync(new ModerationResult(ModerationAction.Delete, "ML решил что это спам"));

        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update, CancellationToken.None);

        // Assert
        _botMock.Verify(x => x.DeleteMessage(chat.Id, message.MessageId, It.IsAny<CancellationToken>()), Times.Once);
    }
} 
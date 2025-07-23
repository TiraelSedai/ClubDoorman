using ClubDoorman.Services;
using ClubDoorman.Models.Notifications;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Фабрика для создания ServiceChatDispatcher с моками для тестирования
/// </summary>
public class ServiceChatDispatcherTestFactory
{
    public Mock<ITelegramBotClientWrapper> BotClientMock { get; }
    public Mock<ILogger<ServiceChatDispatcher>> LoggerMock { get; }

    public ServiceChatDispatcherTestFactory()
    {
        BotClientMock = new Mock<ITelegramBotClientWrapper>();
        LoggerMock = new Mock<ILogger<ServiceChatDispatcher>>();

        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        // Настройка BotClient
        BotClientMock.Setup(x => x.SendMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<ReplyParameters>(), It.IsAny<ReplyMarkup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Text = "Test message" });

        // Настройка Logger
        LoggerMock.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Verifiable();
    }

    /// <summary>
    /// Создает экземпляр ServiceChatDispatcher с настроенными моками
    /// </summary>
    public ServiceChatDispatcher CreateServiceChatDispatcher()
    {
        return new ServiceChatDispatcher(BotClientMock.Object, LoggerMock.Object);
    }

    /// <summary>
    /// Создает тестовые данные пользователя
    /// </summary>
    public static User CreateTestUser(long userId = 12345)
    {
        return new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Username = "testuser"
        };
    }

    /// <summary>
    /// Создает тестовые данные чата
    /// </summary>
    public static Chat CreateTestChat(long chatId = 67890)
    {
        return new Chat
        {
            Id = chatId,
            Title = "Test Chat",
            Type = ChatType.Group
        };
    }

    /// <summary>
    /// Создает тестовые данные уведомления
    /// </summary>
    public static NotificationData CreateTestNotificationData(long userId = 12345)
    {
        return new SimpleNotificationData(CreateTestUser(userId), CreateTestChat(), "Test reason");
    }

    /// <summary>
    /// Создает тестовые данные подозрительного сообщения
    /// </summary>
    public static SuspiciousMessageNotificationData CreateSuspiciousMessageData(long userId = 12345)
    {
        return new SuspiciousMessageNotificationData(
            CreateTestUser(userId),
            CreateTestChat(),
            "Test suspicious message");
    }

    /// <summary>
    /// Создает тестовые данные подозрительного пользователя
    /// </summary>
    public static SuspiciousUserNotificationData CreateSuspiciousUserData(long userId = 12345)
    {
        return new SuspiciousUserNotificationData(
            CreateTestUser(userId),
            CreateTestChat(),
            0.75,
            new List<string> { "First message", "Second message" },
            DateTime.UtcNow);
    }

    /// <summary>
    /// Создает тестовые данные AI анализа профиля
    /// </summary>
    public static AiProfileAnalysisData CreateAiProfileAnalysisData(long userId = 12345)
    {
        return new AiProfileAnalysisData(
            CreateTestUser(userId),
            CreateTestChat(),
            0.95,
            "Test analysis",
            "Test name bio",
            "Test message");
    }

    /// <summary>
    /// Создает тестовые данные AI детекта
    /// </summary>
    public static AiDetectNotificationData CreateAiDetectData(long userId = 12345)
    {
        return new AiDetectNotificationData(
            CreateTestUser(userId),
            CreateTestChat(),
            "Test reason",
            0.90,
            0.85,
            0.80,
            "Test AI reason",
            "Test message",
            false,
            12345);
    }

    /// <summary>
    /// Создает тестовые данные автобана
    /// </summary>
    public static AutoBanNotificationData CreateAutoBanData(long userId = 12345)
    {
        return new AutoBanNotificationData(
            CreateTestUser(userId),
            CreateTestChat(),
            "Test ban type",
            "Test reason");
    }

    /// <summary>
    /// Создает тестовые данные ошибки
    /// </summary>
    public static ErrorNotificationData CreateErrorData(long userId = 12345)
    {
        return new ErrorNotificationData(
            new Exception("Test error"),
            "Test context",
            CreateTestUser(userId),
            CreateTestChat());
    }
} 
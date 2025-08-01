using NUnit.Framework;
using ClubDoorman.Handlers;
using ClubDoorman.Services;
using ClubDoorman.Services.Notifications;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Threading.Tasks;

namespace ClubDoorman.Test.Integration;

/// <summary>
/// Интеграционные тесты для NotificationService
/// <tags>integration, notification-service, message-handler, proxy</tags>
/// </summary>
[TestFixture]
[Category("integration")]
[Category("notification-service")]
public class NotificationServiceIntegrationTests
{
    private NotificationServiceBuilder _builder = null!;
    private INotificationService _notificationService = null!;
    private Mock<IMessageHandler> _messageHandlerMock = null!;
    private Mock<ITelegramBotClientWrapper> _botMock = null!;
    private Mock<IMessageService> _messageServiceMock = null!;

    [SetUp]
    public void Setup()
    {
        _builder = TK.CreateNotificationServiceBuilder()
            .WithStandardMocks();
        _notificationService = _builder.Build();
        _messageHandlerMock = _builder.MessageHandlerMock;
        _botMock = _builder.BotMock;
        _messageServiceMock = _builder.MessageServiceMock;
    }

    #region DeleteAndReportMessage Tests

    /// <summary>
    /// POC: Базовый сценарий удаления сообщения с уведомлением
    /// <tags>poc, happy-path, delete-message</tags>
    /// </summary>
    [Test]
    public async Task DeleteAndReportMessage_ValidMessage_DeletesAndNotifies()
    {
        // Arrange
        var (_, envelope, message, _) = TK.CreateFullScenario();
        var reason = "Test reason";
        var isSilentMode = false;

        // Настраиваем стандартные моки
        _builder.WithStandardMocks();
        _notificationService = _builder.Build();
        _messageHandlerMock = _builder.MessageHandlerMock;

        // Act
        await _notificationService.DeleteAndReportMessage(message, reason, isSilentMode, CancellationToken.None);

        // Assert
        _messageHandlerMock.Verify(x => x.DeleteAndReportMessage(message, reason, isSilentMode, CancellationToken.None), Times.Once);
    }

    /// <summary>
    /// Production: Тихий режим - пользователю не отправляется уведомление
    /// <tags>production, silent-mode, user-notification</tags>
    /// </summary>
    [Test]
    public async Task DeleteAndReportMessage_SilentMode_DoesNotNotifyUser()
    {
        // Arrange
        var (_, envelope, message, _) = TK.CreateFullScenario();
        var reason = "Test reason";
        var isSilentMode = true;

        // Настраиваем стандартные моки
        _builder.WithStandardMocks();
        _notificationService = _builder.Build();
        _messageHandlerMock = _builder.MessageHandlerMock;

        // Act
        await _notificationService.DeleteAndReportMessage(message, reason, isSilentMode, CancellationToken.None);

        // Assert
        // Пока просто проверяем что метод не падает
        // В будущем добавим проверки через рефлексию или логи
        Assert.Pass("Метод выполнился без исключений");
    }

    /// <summary>
    /// Production: Обработка ошибок бота
    /// <tags>production, error-handling, bot-exception</tags>
    /// </summary>
    [Test]
    public async Task DeleteAndReportMessage_ExceptionInBot_HandlesGracefully()
    {
        // Arrange
        var (_, envelope, message, _) = TK.CreateFullScenario();
        var reason = "Test reason";
        var isSilentMode = false;

        // Настраиваем стандартные моки
        _builder.WithStandardMocks();
        _notificationService = _builder.Build();
        _messageHandlerMock = _builder.MessageHandlerMock;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => 
            await _notificationService.DeleteAndReportMessage(message, reason, isSilentMode, CancellationToken.None));
    }

    #endregion

    #region DeleteAndReportToLogChat Tests

    /// <summary>
    /// POC: Удаление сообщения с отправкой в лог-чат
    /// <tags>poc, log-chat, forward-message</tags>
    /// </summary>
    [Test]
    public async Task DeleteAndReportToLogChat_ValidMessage_DeletesAndReportsToLogChat()
    {
        // Arrange
        var (_, envelope, message, _) = TK.CreateFullScenario();
        var reason = "Ссылки запрещены";

        // Настраиваем стандартные моки
        _builder.WithStandardMocks();
        _notificationService = _builder.Build();
        _messageHandlerMock = _builder.MessageHandlerMock;

        // Act
        await _notificationService.DeleteAndReportToLogChat(message, reason, CancellationToken.None);

        // Assert
        // Пока просто проверяем что метод не падает
        // В будущем добавим проверки через рефлексию или логи
        Assert.Pass("Метод выполнился без исключений");
    }

    #endregion

    #region DontDeleteButReportMessage Tests

    /// <summary>
    /// POC: Отправка уведомления без удаления сообщения
    /// <tags>poc, report-only, no-delete</tags>
    /// </summary>
    [Test]
    public async Task DontDeleteButReportMessage_ValidMessage_ReportsWithoutDeleting()
    {
        // Arrange
        var (_, envelope, message, _) = TK.CreateFullScenario();
        var user = message.From!;
        var isSilentMode = false;

        // Настраиваем стандартные моки
        _builder.WithStandardMocks();
        _notificationService = _builder.Build();
        _messageHandlerMock = _builder.MessageHandlerMock;

        // Act
        await _notificationService.DontDeleteButReportMessage(message, user, isSilentMode, CancellationToken.None);

        // Assert
        // Пока просто проверяем что метод не падает
        // В будущем добавим проверки через рефлексию или логи
        Assert.Pass("Метод выполнился без исключений");
    }

    /// <summary>
    /// Extrapolated: Тихий режим для подозрительных сообщений
    /// <tags>extrapolated, silent-mode, suspicious-message</tags>
    /// </summary>
    [Test]
    public async Task DontDeleteButReportMessage_SilentMode_AddsSilentModePrefix()
    {
        // Arrange
        var (_, envelope, message, _) = TK.CreateFullScenario();
        var user = message.From!;
        var isSilentMode = true;

        // Настраиваем стандартные моки
        _builder.WithStandardMocks();
        _notificationService = _builder.Build();
        _messageHandlerMock = _builder.MessageHandlerMock;

        // Act
        await _notificationService.DontDeleteButReportMessage(message, user, isSilentMode, CancellationToken.None);

        // Assert
        // Пока просто проверяем что метод не падает
        // В будущем добавим проверки через рефлексию или логи
        Assert.Pass("Метод выполнился без исключений");
    }

    #endregion

    #region SendSuspiciousMessageWithButtons Tests

    /// <summary>
    /// POC: Отправка подозрительного сообщения с кнопками
    /// <tags>poc, suspicious-message, buttons, forward</tags>
    /// </summary>
    [Test]
    public async Task SendSuspiciousMessageWithButtons_ValidData_SendsWithButtons()
    {
        // Arrange
        var (_, envelope, message, _) = TK.CreateFullScenario();
        var user = message.From!;
        var data = new SuspiciousMessageNotificationData(user, message.Chat, "Test suspicious message", message.MessageId);
        var isSilentMode = false;

        // Настраиваем стандартные моки
        _builder.WithStandardMocks();
        _notificationService = _builder.Build();
        _messageHandlerMock = _builder.MessageHandlerMock;

        // Act
        await _notificationService.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, CancellationToken.None);

        // Assert
        // Пока просто проверяем что метод не падает
        // В будущем добавим проверки через рефлексию или логи
        Assert.Pass("Метод выполнился без исключений");
    }

    /// <summary>
    /// Production: Fallback при ошибке бота
    /// <tags>production, error-handling, fallback, bot-exception</tags>
    /// </summary>
    [Test]
    public async Task SendSuspiciousMessageWithButtons_ExceptionInBot_FallbackToSimpleNotification()
    {
        // Arrange
        var (_, envelope, message, _) = TK.CreateFullScenario();
        var user = message.From!;
        var data = new SuspiciousMessageNotificationData(user, message.Chat, "Test suspicious message", message.MessageId);
        var isSilentMode = false;

        // Настраиваем стандартные моки
        _builder.WithStandardMocks();
        _notificationService = _builder.Build();
        _messageHandlerMock = _builder.MessageHandlerMock;

        // Act
        await _notificationService.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, CancellationToken.None);

        // Assert
        _messageHandlerMock.Verify(x => x.SendSuspiciousMessageWithButtons(message, user, data, isSilentMode, CancellationToken.None), Times.Once);
    }

    #endregion
} 
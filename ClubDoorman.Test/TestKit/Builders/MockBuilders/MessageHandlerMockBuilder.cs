using ClubDoorman.Handlers;
using ClubDoorman.Models.Notifications;
using Telegram.Bot.Types;
using Moq;

namespace ClubDoorman.Test.TestKit.Builders.MockBuilders;

/// <summary>
/// Билдер для мока MessageHandler
/// <tags>builders, message-handler, mocks, fluent-api</tags>
/// </summary>
public class MessageHandlerMockBuilder
{
    private readonly Mock<MessageHandler> _mock = new();

    /// <summary>
    /// Настраивает мок для удаления сообщений
    /// <tags>builders, message-handler, delete-messages, fluent-api</tags>
    /// </summary>
    public MessageHandlerMockBuilder ThatDeletesMessages()
    {
        _mock.Setup(x => x.DeleteAndReportMessage(
                It.IsAny<Message>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        return this;
    }

    /// <summary>
    /// Настраивает мок для отправки в лог-чат
    /// <tags>builders, message-handler, report-to-log-chat, fluent-api</tags>
    /// </summary>
    public MessageHandlerMockBuilder ThatReportsToLogChat()
    {
        _mock.Setup(x => x.DeleteAndReportToLogChat(
                It.IsAny<Message>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        return this;
    }

    /// <summary>
    /// Настраивает мок для отправки уведомления без удаления
    /// <tags>builders, message-handler, report-without-deleting, fluent-api</tags>
    /// </summary>
    public MessageHandlerMockBuilder ThatReportsWithoutDeleting()
    {
        _mock.Setup(x => x.DontDeleteButReportMessage(
                It.IsAny<Message>(),
                It.IsAny<Telegram.Bot.Types.User>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        return this;
    }

    /// <summary>
    /// Настраивает мок для отправки подозрительного сообщения с кнопками
    /// <tags>builders, message-handler, send-suspicious-message-with-buttons, fluent-api</tags>
    /// </summary>
    public MessageHandlerMockBuilder ThatSendsSuspiciousMessageWithButtons()
    {
        _mock.Setup(x => x.SendSuspiciousMessageWithButtons(
                It.IsAny<Message>(),
                It.IsAny<Telegram.Bot.Types.User>(),
                It.IsAny<SuspiciousMessageNotificationData>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        return this;
    }

    /// <summary>
    /// Настраивает мок для выброса исключения
    /// <tags>builders, message-handler, throw-exception, fluent-api</tags>
    /// </summary>
    public MessageHandlerMockBuilder ThatThrowsException()
    {
        _mock.Setup(x => x.DeleteAndReportMessage(
                It.IsAny<Message>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("MessageHandler error"));
        
        return this;
    }

    /// <summary>
    /// Строит мок
    /// <tags>builders, message-handler, build, fluent-api</tags>
    /// </summary>
    public Mock<MessageHandler> Build() => _mock;

    /// <summary>
    /// Строит объект мока
    /// <tags>builders, message-handler, build-object, fluent-api</tags>
    /// </summary>
    public MessageHandler BuildObject() => Build().Object;
} 
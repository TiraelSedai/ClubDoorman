using ClubDoorman.Services;
using ClubDoorman.Models.Notifications;
using Telegram.Bot.Types;
using Moq;

namespace ClubDoorman.Test.TestKit.Builders.MockBuilders;

/// <summary>
/// Билдер для мока IMessageService
/// <tags>builders, message-service, mocks, fluent-api</tags>
/// </summary>
public class MessageServiceMockBuilder
{
    private readonly Mock<IMessageService> _mock = new();

    /// <summary>
    /// Настраивает мок для отправки уведомления пользователю
    /// <tags>builders, message-service, send-user-notification, fluent-api</tags>
    /// </summary>
    public MessageServiceMockBuilder ThatSendsUserNotification()
    {
        _mock.Setup(x => x.SendUserNotificationAsync(
                It.IsAny<User>(),
                It.IsAny<Chat>(),
                It.IsAny<UserNotificationType>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        return this;
    }

    /// <summary>
    /// Настраивает мок для отправки уведомления админу
    /// <tags>builders, message-service, send-admin-notification, fluent-api</tags>
    /// </summary>
    public MessageServiceMockBuilder ThatSendsAdminNotification()
    {
        _mock.Setup(x => x.SendAdminNotificationAsync(
                It.IsAny<AdminNotificationType>(),
                It.IsAny<NotificationData>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        return this;
    }

    /// <summary>
    /// Настраивает мок для отправки уведомления пользователю с ответом
    /// <tags>builders, message-service, send-user-notification-with-reply, fluent-api</tags>
    /// </summary>
    public MessageServiceMockBuilder ThatSendsUserNotificationWithReply()
    {
        _mock.Setup(x => x.SendUserNotificationWithReplyAsync(
                It.IsAny<User>(),
                It.IsAny<Chat>(),
                It.IsAny<UserNotificationType>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Text = "User notification with reply" });
        
        return this;
    }

    /// <summary>
    /// Настраивает мок для выброса исключения
    /// <tags>builders, message-service, throw-exception, fluent-api</tags>
    /// </summary>
    public MessageServiceMockBuilder ThatThrowsException()
    {
        _mock.Setup(x => x.SendUserNotificationAsync(
                It.IsAny<User>(),
                It.IsAny<Chat>(),
                It.IsAny<UserNotificationType>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Message service error"));
        
        _mock.Setup(x => x.SendAdminNotificationAsync(
                It.IsAny<AdminNotificationType>(),
                It.IsAny<NotificationData>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Message service error"));
        
        return this;
    }

    /// <summary>
    /// Строит мок
    /// <tags>builders, message-service, build, fluent-api</tags>
    /// </summary>
    public Mock<IMessageService> Build() => _mock;

    /// <summary>
    /// Строит объект мока
    /// <tags>builders, message-service, build-object, fluent-api</tags>
    /// </summary>
    public IMessageService BuildObject() => Build().Object;
} 
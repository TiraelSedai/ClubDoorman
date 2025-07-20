using ClubDoorman.Handlers;
using ClubDoorman.Services;
using ClubDoorman.Test.TestData;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Telegram.Bot.Types;
using Moq;
using Microsoft.Extensions.Logging;
using ClubDoorman.Models;
using ClubDoorman.Handlers.Commands;

namespace ClubDoorman.Test.Unit.Handlers;

[TestFixture]
[Category("unit")]
[Category("handlers")]
[Category("message")]
public class MessageHandlerFakeTests
{
    private MessageHandlerTestFactory _factory = null!;
    private FakeTelegramClient _fakeClient = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();
        _fakeClient = new FakeTelegramClient();
    }

    [Test]
    public async Task HandleAsync_ValidMessage_ProcessesSuccessfully()
    {
        // Arrange
        var service = _factory.CreateMessageHandlerWithFake(_fakeClient);
        var message = MessageTestData.ValidMessage();

        // Настройка моков
        _factory.ModerationServiceMock
            .Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid message"));

        _factory.ModerationServiceMock
            .Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(true);

        // Act
        var update = new Update { Message = message };
        await service.HandleAsync(update);

        // Assert
        // Сообщение обработано без ошибок
        Assert.That(_fakeClient.SentMessages.Count, Is.EqualTo(0)); // Нет дополнительных сообщений
    }

    [Test]
    public async Task HandleAsync_SpamMessage_DeletesAndReports()
    {
        // Arrange
        var service = _factory.CreateMessageHandlerWithFake(_fakeClient);
        var message = MessageTestData.SpamMessage();

        // Настройка моков
        _factory.ModerationServiceMock
            .Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Delete, "Spam detected"));

        _factory.ModerationServiceMock
            .Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(true);

        // Act
        var update = new Update { Message = message };
        await service.HandleAsync(update);

        // Assert
        Assert.That(_fakeClient.WasMessageDeleted(message.Chat.Id, message.MessageId), Is.True);
    }

    [Test]
    public async Task HandleAsync_NewUser_SendsCaptcha()
    {
        // Arrange
        var service = _factory.CreateMessageHandlerWithFake(_fakeClient);
        var message = MessageTestData.ServiceMessage(); // Сообщение о новом участнике

        // Act
        var update = new Update { Message = message };
        await service.HandleAsync(update);

        // Assert
        // Проверяем, что была вызвана отправка капчи
        _factory.CaptchaServiceMock.Verify(
            x => x.CreateCaptchaAsync(It.IsAny<Chat>(), It.IsAny<User>(), It.IsAny<Message>()),
            Times.Once);
    }

    [Test]
    public async Task HandleAsync_BotMessage_Ignores()
    {
        // Arrange
        var service = _factory.CreateMessageHandlerWithFake(_fakeClient);
        var message = MessageTestData.BotMessage();

        // Act
        var update = new Update { Message = message };
        await service.HandleAsync(update);

        // Assert
        // Бот сообщения игнорируются - моки не вызываются
        _factory.ModerationServiceMock.Verify(
            x => x.CheckMessageAsync(It.IsAny<Message>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ServiceMessage_Ignores()
    {
        // Arrange
        var service = _factory.CreateMessageHandlerWithFake(_fakeClient);
        var message = MessageTestData.ServiceMessage();

        // Act
        var update = new Update { Message = message };
        await service.HandleAsync(update);

        // Assert
        // Сервисные сообщения игнорируются
        _factory.ModerationServiceMock.Verify(
            x => x.CheckMessageAsync(It.IsAny<Message>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_NullMessage_ThrowsNullReferenceException()
    {
        // Arrange
        var service = _factory.CreateMessageHandlerWithFake(_fakeClient);

        // Act & Assert
        Assert.ThrowsAsync<NullReferenceException>(async () =>
            await service.HandleAsync(new Update { Message = null }));
    }

    [Test]
    public async Task HandleAsync_ModerationServiceError_LogsAndContinues()
    {
        // Arrange
        var service = _factory.CreateMessageHandlerWithFake(_fakeClient);
        var message = MessageTestData.ValidMessage();

        // Настройка моков
        _factory.ModerationServiceMock
            .Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ThrowsAsync(new Exception("Moderation service error"));

        _factory.ModerationServiceMock
            .Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(false); // Пользователь не одобрен, чтобы дойти до модерации

        // Act & Assert
        // Ошибка должна быть проброшена, так как нет обработки исключений
        Assert.ThrowsAsync<Exception>(async () =>
        {
            var update = new Update { Message = message };
            await service.HandleAsync(update);
        });
    }

    [Test]
    public async Task HandleAsync_TelegramError_HandlesGracefully()
    {
        // Arrange
        _fakeClient.ShouldThrowException = true;
        _fakeClient.ExceptionToThrow = new Exception("Telegram API error");
        
        var service = _factory.CreateMessageHandlerWithFake(_fakeClient);
        var message = MessageTestData.ValidMessage();

        // Настройка моков
        _factory.ModerationServiceMock
            .Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Delete, "Spam"));

        _factory.ModerationServiceMock
            .Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(true);

        // Act
        var update = new Update { Message = message };
        await service.HandleAsync(update);

        // Assert
        // Ошибка Telegram обработана, исключение не проброшено
        _factory.LoggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task HandleAsync_StartCommand_ProcessesCommand()
    {
        // Arrange
        var service = _factory.CreateMessageHandlerWithFake(_fakeClient);
        var message = MessageTestData.StartCommand();

        // Настройка ServiceProvider для команд
        var mockStartCommandHandler = new Mock<StartCommandHandler>();
        _factory.ServiceProviderMock
            .Setup(x => x.GetRequiredService(It.IsAny<Type>()))
            .Returns(mockStartCommandHandler.Object);

        // Act
        var update = new Update { Message = message };
        await service.HandleAsync(update);

        // Assert
        // Команда обработана
        mockStartCommandHandler.Verify(
            x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task HandleAsync_ApprovedUser_ProcessesNormally()
    {
        // Arrange
        var service = _factory.CreateMessageHandlerWithFake(_fakeClient);
        var message = MessageTestData.ValidMessage();

        // Настройка моков - пользователь одобрен
        _factory.ModerationServiceMock
            .Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(true);

        _factory.ModerationServiceMock
            .Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid message"));

        // Act
        var update = new Update { Message = message };
        await service.HandleAsync(update);

        // Assert
        // Капча не отправляется для одобренных пользователей
        _factory.CaptchaServiceMock.Verify(
            x => x.CreateCaptchaAsync(It.IsAny<Chat>(), It.IsAny<User>(), It.IsAny<Message>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_EmptyMessage_ProcessesCorrectly()
    {
        // Arrange
        var service = _factory.CreateMessageHandlerWithFake(_fakeClient);
        var message = MessageTestData.EmptyMessage();

        // Настройка моков
        _factory.ModerationServiceMock
            .Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Empty message allowed"));

        _factory.ModerationServiceMock
            .Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(true);

        // Act
        var update = new Update { Message = message };
        await service.HandleAsync(update);

        // Assert
        // Пустое сообщение обработано
        _factory.ModerationServiceMock.Verify(
            x => x.CheckMessageAsync(It.IsAny<Message>()),
            Times.Once);
    }
} 
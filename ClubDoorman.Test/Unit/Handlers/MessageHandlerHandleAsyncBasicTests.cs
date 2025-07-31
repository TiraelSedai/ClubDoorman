using ClubDoorman.Handlers;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Test.TestKit;
using NUnit.Framework;
using Moq;
using System;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Базовые тесты для метода HandleAsync в MessageHandler
/// <tags>unit, handlers, handle-async, golden-master</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("handlers")]
[Category("handle-async")]
[Category("golden-master")]
public class MessageHandlerHandleAsyncBasicTests
{
    private MessageHandlerTestFactory _factory = null!;
    private MessageHandler _messageHandler = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();
        
        // Настройка базовых моков для предотвращения NullReferenceException
        _factory.WithModerationServiceSetup(mock =>
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new Models.ModerationResult(Models.ModerationAction.Allow, "Test"));
            mock.Setup(x => x.CheckUserNameAsync(It.IsAny<User>()))
                .ReturnsAsync(new Models.ModerationResult(Models.ModerationAction.Allow, "Test name"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });
        
        _factory.WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.Approved(It.IsAny<long>(), It.IsAny<long?>()))
                .Returns(true);
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>()))
                .ReturnsAsync((string?)null);
            mock.Setup(x => x.InBanlist(It.IsAny<long>()))
                .ReturnsAsync(false);
        });

        _messageHandler = _factory.CreateMessageHandler();
    }

    /// <summary>
    /// Тест для HandleAsync с null Update
    /// Проверяет, что метод выбрасывает ArgumentNullException для null Update
    /// <tags>golden-master, production, handle-async, null-update, error-handling</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_NullUpdate_ThrowsArgumentNullException()
    {
        // Arrange: Передаем null
        Update? update = null;

        // Act & Assert: Проверяем исключение
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _messageHandler.HandleAsync(update));
        
        Assert.That(exception.ParamName, Is.EqualTo("update"), 
            "Параметр исключения должен быть 'update'");
    }

    /// <summary>
    /// Тест для HandleAsync с валидным текстовым сообщением
    /// Проверяет, что метод обрабатывает обычное текстовое сообщение и вызывает модерацию
    /// <tags>golden-master, production, handle-async, valid-message, moderation</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidTextMessage_CallsModerationAndLogs()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var update = new Update { Message = message };

        // Act: Вызываем метод
        await _messageHandler.HandleAsync(update);

        // Assert: Проверяем вызовы модерации - используем конкретный объект
        _factory.ModerationServiceMock.Verify(
            x => x.CheckMessageAsync(message),
            Times.Once,
            "Должна вызваться проверка сообщения");

        // CheckUserNameAsync вызывается только для новых участников, не для обычных сообщений
        _factory.ModerationServiceMock.Verify(
            x => x.CheckUserNameAsync(It.IsAny<User>()),
            Times.Never,
            "CheckUserNameAsync не должен вызываться для обычных сообщений");

        // Проверяем логирование - используем реальные сообщения из логов
        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MessageHandler получил сообщение")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться получение сообщения");
    }

    /// <summary>
    /// Тест для HandleAsync с отредактированным сообщением
    /// Проверяет, что метод обрабатывает отредактированное сообщение
    /// <tags>golden-master, production, handle-async, edited-message</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_EditedMessage_CallsModerationAndLogs()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.EditDate = DateTime.UtcNow;
        var update = new Update { EditedMessage = message };

        // Act: Вызываем метод
        await _messageHandler.HandleAsync(update);

        // Assert: Проверяем вызовы модерации - используем конкретный объект
        _factory.ModerationServiceMock.Verify(
            x => x.CheckMessageAsync(message),
            Times.Once,
            "Должна вызваться проверка отредактированного сообщения");

        // CheckUserNameAsync вызывается только для новых участников, не для обычных сообщений
        _factory.ModerationServiceMock.Verify(
            x => x.CheckUserNameAsync(It.IsAny<User>()),
            Times.Never,
            "CheckUserNameAsync не должен вызываться для обычных сообщений");

        // Проверяем логирование - используем реальные сообщения из логов
        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MessageHandler получил сообщение")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться получение отредактированного сообщения");
    }

    /// <summary>
    /// Тест для HandleAsync с конкретными проверками для убийства мутантов
    /// Проверяет конкретные значения и состояния объектов
    /// <tags>golden-master, production, handle-async, mutation-killing</tags>
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidMessage_VerifiesSpecificCalls()
    {
        // Arrange: Создаем конкретные объекты для точных проверок
        var user = new User { Id = 12345, FirstName = "Test", Username = "testuser" };
        var chat = new Chat { Id = -1001234567890, Type = ChatType.Supergroup, Title = "Test Chat" };
        var message = new Message 
        { 
            Date = DateTime.UtcNow,
            From = user,
            Chat = chat,
            Text = "Hello world"
        };
        var update = new Update { Message = message };

        // Act: Вызываем метод
        await _messageHandler.HandleAsync(update);

        // Assert: Проверяем конкретные вызовы с точными параметрами
        _factory.ModerationServiceMock.Verify(
            x => x.CheckMessageAsync(It.Is<Message>(m => 
                m.Text == "Hello world" && 
                m.From!.Id == 12345)),
            Times.Once,
            "Должна вызваться проверка сообщения с точными параметрами");

        // Проверяем, что пользователь проверяется по блэклисту
        _factory.UserManagerMock.Verify(
            x => x.InBanlist(12345),
            Times.Once,
            "Должна вызваться проверка пользователя по блэклисту");

        // Проверяем, что AI анализ запускается
        _factory.AiChecksMock.Verify(
            x => x.GetAttentionBaitProbability(It.Is<User>(u => u.Id == 12345), It.Is<string>(s => s == "Hello world"), It.IsAny<Func<string, Task>>()),
            Times.Once,
            "Должен запуститься AI анализ профиля");
    }
} 
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
/// Тесты для метода CanHandle в MessageHandler
/// <tags>unit, handlers, can-handle, golden-master</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("handlers")]
[Category("can-handle")]
[Category("golden-master")]
public class MessageHandlerCanHandleTests
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
    /// Тест для CanHandle с валидным сообщением
    /// Проверяет, что метод возвращает true для обычного текстового сообщения
    /// <tags>golden-master, production, can-handle, valid-message</tags>
    /// </summary>
    [Test]
    public void CanHandle_ValidMessage_ReturnsTrue()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        var update = new Update { Message = message };

        // Act: Вызываем метод
        var result = _messageHandler.CanHandle(update);

        // Assert: Проверяем результат
        Assert.That(result, Is.True, "CanHandle должен возвращать true для валидного сообщения");
    }

    /// <summary>
    /// Тест для CanHandle с отредактированным сообщением
    /// Проверяет, что метод возвращает true для отредактированного сообщения
    /// <tags>golden-master, production, can-handle, edited-message</tags>
    /// </summary>
    [Test]
    public void CanHandle_EditedMessage_ReturnsTrue()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
        message.EditDate = DateTime.UtcNow;
        var update = new Update { EditedMessage = message };

        // Act: Вызываем метод
        var result = _messageHandler.CanHandle(update);

        // Assert: Проверяем результат
        Assert.That(result, Is.True, "CanHandle должен возвращать true для отредактированного сообщения");
    }

    /// <summary>
    /// Тест для CanHandle с null Update
    /// Проверяет, что метод возвращает false для null Update
    /// <tags>golden-master, production, can-handle, null-update</tags>
    /// </summary>
    [Test]
    public void CanHandle_NullUpdate_ReturnsFalse()
    {
        // Arrange: Передаем null
        Update? update = null;

        // Act: Вызываем метод
        var result = _messageHandler.CanHandle(update);

        // Assert: Проверяем результат
        Assert.That(result, Is.False, "CanHandle должен возвращать false для null Update");
    }

    /// <summary>
    /// Тест для CanHandle с Update без сообщений
    /// Проверяет, что метод возвращает false для Update без Message и EditedMessage
    /// <tags>golden-master, production, can-handle, no-message</tags>
    /// </summary>
    [Test]
    public void CanHandle_UpdateWithoutMessage_ReturnsFalse()
    {
        // Arrange: Создаем Update без сообщений
        var update = new Update { Message = null, EditedMessage = null };

        // Act: Вызываем метод
        var result = _messageHandler.CanHandle(update);

        // Assert: Проверяем результат
        Assert.That(result, Is.False, "CanHandle должен возвращать false для Update без сообщений");
    }

    /// <summary>
    /// Тест для CanHandle с Update содержащим CallbackQuery
    /// Проверяет, что метод возвращает false для Update с CallbackQuery
    /// <tags>golden-master, production, can-handle, callback-query</tags>
    /// </summary>
    [Test]
    public void CanHandle_UpdateWithCallbackQuery_ReturnsFalse()
    {
        // Arrange: Создаем Update с CallbackQuery
        var update = new Update 
        { 
            Message = null, 
            EditedMessage = null,
            CallbackQuery = new CallbackQuery { Id = "test" }
        };

        // Act: Вызываем метод
        var result = _messageHandler.CanHandle(update);

        // Assert: Проверяем результат
        Assert.That(result, Is.False, "CanHandle должен возвращать false для Update с CallbackQuery");
    }

    /// <summary>
    /// Тест для CanHandle с сообщением без текста
    /// Проверяет, что метод возвращает true для сообщения без текста (медиа)
    /// <tags>golden-master, production, can-handle, media-message</tags>
    /// </summary>
    [Test]
    public void CanHandle_MessageWithoutText_ReturnsTrue()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message) = TK.Specialized.Messages.NoTextScenario();
        var update = new Update { Message = message };

        // Act: Вызываем метод
        var result = _messageHandler.CanHandle(update);

        // Assert: Проверяем результат
        Assert.That(result, Is.True, "CanHandle должен возвращать true для сообщения без текста");
    }

    /// <summary>
    /// Тест для CanHandle с сообщением с подписью
    /// Проверяет, что метод возвращает true для сообщения с подписью
    /// <tags>golden-master, production, can-handle, caption-message</tags>
    /// </summary>
    [Test]
    public void CanHandle_MessageWithCaption_ReturnsTrue()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message) = TK.Specialized.Messages.CaptionOnlyScenario();
        var update = new Update { Message = message };

        // Act: Вызываем метод
        var result = _messageHandler.CanHandle(update);

        // Assert: Проверяем результат
        Assert.That(result, Is.True, "CanHandle должен возвращать true для сообщения с подписью");
    }
} 
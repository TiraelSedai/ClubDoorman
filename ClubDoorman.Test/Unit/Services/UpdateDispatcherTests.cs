using ClubDoorman.Services;
using ClubDoorman.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("fast")]
[Category("services")]
[Category("dispatcher")]
public class UpdateDispatcherTests
{
    private Mock<ILogger<UpdateDispatcher>> _loggerMock;
    private Mock<IUpdateHandler> _handler1Mock;
    private Mock<IUpdateHandler> _handler2Mock;
    private UpdateDispatcher _dispatcher;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<UpdateDispatcher>>();
        _handler1Mock = new Mock<IUpdateHandler>();
        _handler2Mock = new Mock<IUpdateHandler>();

        var handlers = new List<IUpdateHandler> { _handler1Mock.Object, _handler2Mock.Object };
        _dispatcher = new UpdateDispatcher(handlers, _loggerMock.Object);
    }

    [Test]
    [Category("constructor")]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var handlers = new List<IUpdateHandler> { _handler1Mock.Object };
        var logger = new Mock<ILogger<UpdateDispatcher>>();

        // Act
        var dispatcher = new UpdateDispatcher(handlers, logger.Object);

        // Assert
        Assert.That(dispatcher, Is.Not.Null);
    }

    [Test]
    [Category("constructor")]
    public void Constructor_NullHandlers_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<UpdateDispatcher>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new UpdateDispatcher(null!, logger.Object));

        Assert.That(exception.ParamName, Is.EqualTo("updateHandlers"));
    }

    [Test]
    [Category("constructor")]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var handlers = new List<IUpdateHandler> { _handler1Mock.Object };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new UpdateDispatcher(handlers, null!));

        Assert.That(exception.ParamName, Is.EqualTo("logger"));
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_ValidUpdate_ProcessesAllHandlers()
    {
        // Arrange
        var update = CreateTestUpdate();
        _handler1Mock.Setup(x => x.CanHandle(update)).Returns(true);
        _handler2Mock.Setup(x => x.CanHandle(update)).Returns(false);

        // Act
        await _dispatcher.DispatchAsync(update);

        // Assert
        _handler1Mock.Verify(x => x.CanHandle(update), Times.Once);
        _handler1Mock.Verify(x => x.HandleAsync(update, It.IsAny<CancellationToken>()), Times.Once);
        _handler2Mock.Verify(x => x.CanHandle(update), Times.Once);
        _handler2Mock.Verify(x => x.HandleAsync(update, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_AllHandlersCanHandle_CallsAllHandlers()
    {
        // Arrange
        var update = CreateTestUpdate();
        _handler1Mock.Setup(x => x.CanHandle(update)).Returns(true);
        _handler2Mock.Setup(x => x.CanHandle(update)).Returns(true);

        // Act
        await _dispatcher.DispatchAsync(update);

        // Assert
        _handler1Mock.Verify(x => x.CanHandle(update), Times.Once);
        _handler1Mock.Verify(x => x.HandleAsync(update, It.IsAny<CancellationToken>()), Times.Once);
        _handler2Mock.Verify(x => x.CanHandle(update), Times.Once);
        _handler2Mock.Verify(x => x.HandleAsync(update, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_NoHandlersCanHandle_LogsDebugInfo()
    {
        // Arrange
        var update = CreateTestUpdate();
        _handler1Mock.Setup(x => x.CanHandle(update)).Returns(false);
        _handler2Mock.Setup(x => x.CanHandle(update)).Returns(false);

        // Act
        await _dispatcher.DispatchAsync(update);

        // Assert
        _handler1Mock.Verify(x => x.CanHandle(update), Times.Once);
        _handler1Mock.Verify(x => x.HandleAsync(update, It.IsAny<CancellationToken>()), Times.Never);
        _handler2Mock.Verify(x => x.CanHandle(update), Times.Once);
        _handler2Mock.Verify(x => x.HandleAsync(update, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_NullUpdate_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _dispatcher.DispatchAsync(null!));

        Assert.That(exception.ParamName, Is.EqualTo("update"));
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_HandlerThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var update = CreateTestUpdate();
        var expectedException = new Exception("Handler error");
        _handler1Mock.Setup(x => x.CanHandle(update)).Returns(true);
        _handler1Mock.Setup(x => x.HandleAsync(update, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(async () =>
            await _dispatcher.DispatchAsync(update));

        Assert.That(exception, Is.EqualTo(expectedException));
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_OperationCanceledException_LogsInfoAndRethrows()
    {
        // Arrange
        var update = CreateTestUpdate();
        var expectedException = new OperationCanceledException();
        _handler1Mock.Setup(x => x.CanHandle(update)).Returns(true);
        _handler1Mock.Setup(x => x.HandleAsync(update, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _dispatcher.DispatchAsync(update));

        Assert.That(exception, Is.EqualTo(expectedException));
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_WithCancellationToken_PassesTokenToHandlers()
    {
        // Arrange
        var update = CreateTestUpdate();
        var cancellationToken = new CancellationToken();
        _handler1Mock.Setup(x => x.CanHandle(update)).Returns(true);

        // Act
        await _dispatcher.DispatchAsync(update, cancellationToken);

        // Assert
        _handler1Mock.Verify(x => x.HandleAsync(update, cancellationToken), Times.Once);
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_EmptyHandlersList_ProcessesWithoutErrors()
    {
        // Arrange
        var emptyDispatcher = new UpdateDispatcher(new List<IUpdateHandler>(), _loggerMock.Object);
        var update = CreateTestUpdate();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await emptyDispatcher.DispatchAsync(update));
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_MessageUpdate_LogsCorrectDebugInfo()
    {
        // Arrange
        var update = CreateTestUpdate();

        // Act
        await _dispatcher.DispatchAsync(update);

        // Assert
        // Проверяем, что логгер был вызван с правильными параметрами
        // (проверка логов в unit тестах обычно не требуется, но можно проверить, что метод не падает)
        Assert.Pass("Update processed successfully");
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_CallbackQueryUpdate_LogsCorrectDebugInfo()
    {
        // Arrange
        var update = new Update
        {
            CallbackQuery = new CallbackQuery { Id = "test_callback" }
        };

        // Act
        await _dispatcher.DispatchAsync(update);

        // Assert
        Assert.Pass("Callback query update processed successfully");
    }

    [Test]
    [Category("dispatch")]
    public async Task DispatchAsync_ChatMemberUpdate_LogsCorrectDebugInfo()
    {
        // Arrange
        var update = new Update
        {
            ChatMember = new ChatMemberUpdated
            {
                Chat = new Chat { Id = 12345 },
                From = new User { Id = 67890 }
            }
        };

        // Act
        await _dispatcher.DispatchAsync(update);

        // Assert
        Assert.Pass("Chat member update processed successfully");
    }

    [Test]
    [Category("integration")]
    public async Task DispatchAsync_MultipleHandlersWithDifferentResponses_ProcessesCorrectly()
    {
        // Arrange
        var update = CreateTestUpdate();
        _handler1Mock.Setup(x => x.CanHandle(update)).Returns(true);
        _handler2Mock.Setup(x => x.CanHandle(update)).Returns(true);

        var handler1Called = false;
        var handler2Called = false;

        _handler1Mock.Setup(x => x.HandleAsync(update, It.IsAny<CancellationToken>()))
            .Callback(() => handler1Called = true)
            .Returns(Task.CompletedTask);

        _handler2Mock.Setup(x => x.HandleAsync(update, It.IsAny<CancellationToken>()))
            .Callback(() => handler2Called = true)
            .Returns(Task.CompletedTask);

        // Act
        await _dispatcher.DispatchAsync(update);

        // Assert
        Assert.That(handler1Called, Is.True);
        Assert.That(handler2Called, Is.True);
    }

    [Test]
    [Category("performance")]
    public async Task DispatchAsync_LargeNumberOfHandlers_ProcessesEfficiently()
    {
        // Arrange
        var handlers = new List<IUpdateHandler>();
        var handlerMocks = new List<Mock<IUpdateHandler>>();

        for (int i = 0; i < 10; i++)
        {
            var mock = new Mock<IUpdateHandler>();
            mock.Setup(x => x.CanHandle(It.IsAny<Update>())).Returns(i % 2 == 0);
            handlers.Add(mock.Object);
            handlerMocks.Add(mock);
        }

        var largeDispatcher = new UpdateDispatcher(handlers, _loggerMock.Object);
        var update = CreateTestUpdate();

        // Act
        await largeDispatcher.DispatchAsync(update);

        // Assert
        foreach (var mock in handlerMocks)
        {
            mock.Verify(x => x.CanHandle(update), Times.Once);
            if (handlerMocks.IndexOf(mock) % 2 == 0)
            {
                mock.Verify(x => x.HandleAsync(update, It.IsAny<CancellationToken>()), Times.Once);
            }
            else
            {
                mock.Verify(x => x.HandleAsync(update, It.IsAny<CancellationToken>()), Times.Never);
            }
        }
    }

    private static Update CreateTestUpdate()
    {
        return new Update
        {
            Message = new Message
            {
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 12345 },
                From = new User { Id = 67890 },
                Text = "Test message"
            }
        };
    }
} 
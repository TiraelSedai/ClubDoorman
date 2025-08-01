using ClubDoorman.Handlers;
using ClubDoorman.Services;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Services;

/// <summary>
/// Тесты для CommandProcessingService
/// <tags>unit, services, command-processing, proxy</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("services")]
[Category("command-processing")]
public class CommandProcessingServiceTests
{
    private Mock<IMessageHandler> _messageHandlerMock = null!;
    private Mock<ILogger<CommandProcessingService>> _loggerMock = null!;
    private CommandProcessingService _service = null!;

    [SetUp]
    public void Setup()
    {
        _messageHandlerMock = new Mock<IMessageHandler>();
        _loggerMock = new Mock<ILogger<CommandProcessingService>>();
        _service = new CommandProcessingService(_messageHandlerMock.Object, _loggerMock.Object);
    }

    /// <summary>
    /// POC: Проверка проксирования вызова HandleCommandAsync
    /// <tags>poc, proxy, command-processing</tags>
    /// </summary>
    [Test]
    public async Task HandleCommandAsync_ValidMessage_ProxiesToMessageHandler()
    {
        // Arrange
        var message = TK.CreateMessage();
        var cancellationToken = CancellationToken.None;

        // Act
        await _service.HandleCommandAsync(message, cancellationToken);

        // Assert
        _messageHandlerMock.Verify(x => x.HandleCommandAsync(message, cancellationToken), Times.Once);
    }
} 
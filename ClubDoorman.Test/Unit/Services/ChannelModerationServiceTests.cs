using ClubDoorman.Handlers;
using ClubDoorman.Services;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Services;

/// <summary>
/// Тесты для ChannelModerationService
/// <tags>unit, services, channel-moderation, proxy</tags>
/// </summary>
[TestFixture]
[Category("unit")]
[Category("services")]
[Category("channel-moderation")]
public class ChannelModerationServiceTests
{
    private Mock<IMessageHandler> _messageHandlerMock = null!;
    private Mock<ILogger<ChannelModerationService>> _loggerMock = null!;
    private ChannelModerationService _service = null!;

    [SetUp]
    public void Setup()
    {
        _messageHandlerMock = new Mock<IMessageHandler>();
        _loggerMock = new Mock<ILogger<ChannelModerationService>>();
        _service = new ChannelModerationService(_messageHandlerMock.Object, _loggerMock.Object);
    }

    /// <summary>
    /// POC: Проверка проксирования вызова HandleChannelMessageAsync
    /// <tags>poc, proxy, channel-moderation</tags>
    /// </summary>
    [Test]
    public async Task HandleChannelMessageAsync_ValidMessage_ProxiesToMessageHandler()
    {
        // Arrange
        var message = TK.CreateMessage();
        var cancellationToken = CancellationToken.None;

        // Act
        await _service.HandleChannelMessageAsync(message, cancellationToken);

        // Assert
        _messageHandlerMock.Verify(x => x.HandleChannelMessageAsync(message, cancellationToken), Times.Once);
    }
} 
using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("fast")]
[Category("critical")]
[Category("uses:bot-permissions")]
public class BotPermissionsServiceTests : TestBase
{
    private readonly Mock<ITelegramBotClientWrapper> _mockBot;
    private readonly Mock<ILogger<BotPermissionsService>> _mockLogger;
    private readonly BotPermissionsService _service;

    public BotPermissionsServiceTests()
    {
        _mockBot = new Mock<ITelegramBotClientWrapper>();
        _mockLogger = new Mock<ILogger<BotPermissionsService>>();
        _service = new BotPermissionsService(_mockBot.Object, _mockLogger.Object);
    }

    [Test]
    public void Constructor_WithNullBot_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new BotPermissionsService(null!, _mockLogger.Object));
        Assert.That(exception.ParamName, Is.EqualTo("bot"));
    }

    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new BotPermissionsService(_mockBot.Object, null!));
        Assert.That(exception.ParamName, Is.EqualTo("logger"));
    }

    [Test]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var service = new BotPermissionsService(_mockBot.Object, _mockLogger.Object);

        // Assert
        Assert.That(service, Is.Not.Null);
    }

    [TestCase(ChatMemberStatus.Administrator, true)]
    [TestCase(ChatMemberStatus.Member, false)]
    [TestCase(ChatMemberStatus.Left, false)]
    [TestCase(ChatMemberStatus.Kicked, false)]
    [TestCase(ChatMemberStatus.Creator, false)]
    public async Task IsBotAdminAsync_WithDifferentStatuses_ReturnsExpectedResult(
        ChatMemberStatus status, bool expectedResult)
    {
        // Arrange
        var chatId = 1000L + (long)status; // Уникальный chatId для каждого статуса
        ChatMember chatMember = status switch
        {
            ChatMemberStatus.Administrator => new ChatMemberAdministrator(),
            ChatMemberStatus.Creator => new ChatMemberOwner(),
            _ => new ChatMemberMember()
        };
        
        _mockBot.Setup(x => x.BotId).Returns(456L);
        _mockBot.Setup(x => x.GetChatMember(chatId, 456L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatMember);

        // Act
        var result = await _service.IsBotAdminAsync(chatId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResult));
        _mockBot.Verify(x => x.GetChatMember(chatId, 456L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task IsBotAdminAsync_WhenExceptionOccurs_ReturnsFalseAndLogsWarning()
    {
        // Arrange
        var chatId = 2000L;
        var exception = new Exception("Test exception");
        
        _mockBot.Setup(x => x.BotId).Returns(456L);
        _mockBot.Setup(x => x.GetChatMember(chatId, 456L, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _service.IsBotAdminAsync(chatId);

        // Assert
        Assert.That(result, Is.False);
        // Проверяем, что было залогировано предупреждение о неудачной проверке прав администратора
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Не удалось проверить права администратора")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [TestCase(3000L, true)] // Обычный чат, не админ
    [TestCase(3001L, true)] // Обычный чат, не админ
    public async Task IsSilentModeAsync_ForRegularChats_ReturnsExpectedResult(long chatId, bool expectedResult)
    {
        // Arrange
        var chat = new Chat { Id = chatId, Type = ChatType.Group };
        var chatMember = new ChatMemberMember();
        
        _mockBot.Setup(x => x.GetChat(chatId, It.IsAny<CancellationToken>())).ReturnsAsync(chat);
        _mockBot.Setup(x => x.BotId).Returns(456L);
        _mockBot.Setup(x => x.GetChatMember(chatId, 456L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatMember);

        // Act
        var result = await _service.IsSilentModeAsync(chatId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [Test]
    public async Task IsSilentModeAsync_ForAdminChat_ReturnsFalse()
    {
        // Arrange
        var adminChatId = Config.AdminChatId;

        // Act
        var result = await _service.IsSilentModeAsync(adminChatId);

        // Assert
        Assert.That(result, Is.False);
        _mockBot.Verify(x => x.GetChat(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task IsSilentModeAsync_ForLogAdminChat_ReturnsFalse()
    {
        // Arrange
        var logAdminChatId = Config.LogAdminChatId;

        // Act
        var result = await _service.IsSilentModeAsync(logAdminChatId);

        // Assert
        Assert.That(result, Is.False);
        _mockBot.Verify(x => x.GetChat(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task IsSilentModeAsync_ForPrivateChat_ReturnsFalse()
    {
        // Arrange
        var chatId = 4000L;
        var chat = new Chat { Id = chatId, Type = ChatType.Private };
        
        _mockBot.Setup(x => x.GetChat(chatId, It.IsAny<CancellationToken>())).ReturnsAsync(chat);

        // Act
        var result = await _service.IsSilentModeAsync(chatId);

        // Assert
        Assert.That(result, Is.False);
        _mockBot.Verify(x => x.GetChat(chatId, It.IsAny<CancellationToken>()), Times.Once);
        // Для приватных чатов GetChatMember не должен вызываться для этого конкретного chatId
        _mockBot.Verify(x => x.GetChatMember(chatId, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task IsSilentModeAsync_WhenChatGetFails_ReturnsFalseAndLogsWarning()
    {
        // Arrange
        var chatId = 5000L;
        var exception = new Exception("Test exception");
        
        _mockBot.Setup(x => x.GetChat(chatId, It.IsAny<CancellationToken>())).ThrowsAsync(exception);

        // Act
        var result = await _service.IsSilentModeAsync(chatId);

        // Assert
        Assert.That(result, Is.False);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Не удалось получить информацию о чате")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task IsSilentModeAsync_WhenBotIsAdmin_ReturnsFalse()
    {
        // Arrange
        var chatId = 6000L;
        var chat = new Chat { Id = chatId, Type = ChatType.Group };
        var chatMember = new ChatMemberAdministrator();
        
        _mockBot.Setup(x => x.GetChat(chatId, It.IsAny<CancellationToken>())).ReturnsAsync(chat);
        _mockBot.Setup(x => x.BotId).Returns(456L);
        _mockBot.Setup(x => x.GetChatMember(chatId, 456L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatMember);

        // Act
        var result = await _service.IsSilentModeAsync(chatId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsSilentModeAsync_WhenBotIsNotAdmin_ReturnsTrue()
    {
        // Arrange
        var chatId = 7000L;
        var chat = new Chat { Id = chatId, Type = ChatType.Group };
        var chatMember = new ChatMemberMember();
        
        _mockBot.Setup(x => x.GetChat(chatId, It.IsAny<CancellationToken>())).ReturnsAsync(chat);
        _mockBot.Setup(x => x.BotId).Returns(456L);
        _mockBot.Setup(x => x.GetChatMember(chatId, 456L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatMember);

        // Act
        var result = await _service.IsSilentModeAsync(chatId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task GetBotChatMemberAsync_WhenNotCached_ReturnsFromApiAndCaches()
    {
        // Arrange
        var chatId = 8000L;
        var botId = 456L;
        var chatMember = new ChatMemberAdministrator();
        
        _mockBot.Setup(x => x.BotId).Returns(botId);
        _mockBot.Setup(x => x.GetChatMember(chatId, botId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatMember);

        // Act
        var result = await _service.GetBotChatMemberAsync(chatId);

        // Assert
        Assert.That(result, Is.EqualTo(chatMember));
        _mockBot.Verify(x => x.GetChatMember(chatId, botId, It.IsAny<CancellationToken>()), Times.Once);
        // Проверяем, что было залогировано сообщение о получении информации о правах бота
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Получена информация о правах бота")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task GetBotChatMemberAsync_WhenCached_ReturnsFromCache()
    {
        // Arrange
        var chatId = 9000L;
        var botId = 456L;
        var chatMember = new ChatMemberAdministrator();
        
        _mockBot.Setup(x => x.BotId).Returns(botId);
        _mockBot.Setup(x => x.GetChatMember(chatId, botId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatMember);

        // Act - первый вызов для кэширования
        await _service.GetBotChatMemberAsync(chatId);
        
        // Второй вызов должен использовать кэш
        var result = await _service.GetBotChatMemberAsync(chatId);

        // Assert
        Assert.That(result, Is.EqualTo(chatMember));
        _mockBot.Verify(x => x.GetChatMember(chatId, botId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetBotChatMemberAsync_WithCancellationToken_PassesTokenToApi()
    {
        // Arrange
        var chatId = 10000L;
        var botId = 456L;
        var chatMember = new ChatMemberAdministrator();
        var cancellationToken = new CancellationToken();
        
        _mockBot.Setup(x => x.BotId).Returns(botId);
        _mockBot.Setup(x => x.GetChatMember(chatId, botId, cancellationToken))
            .ReturnsAsync(chatMember);

        // Act
        var result = await _service.GetBotChatMemberAsync(chatId, cancellationToken);

        // Assert
        Assert.That(result, Is.EqualTo(chatMember));
        _mockBot.Verify(x => x.GetChatMember(chatId, botId, cancellationToken), Times.Once);
    }
} 
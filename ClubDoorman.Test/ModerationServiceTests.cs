using ClubDoorman.Services;
using ClubDoorman.Models;
using ClubDoorman.Test.Mocks;
using ClubDoorman.Test.TestData;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.Test;

/// <summary>
/// Тесты для ModerationService - основной фокус TDD
/// </summary>
[TestFixture]
[Category("moderation")]
public class ModerationServiceTests
{
    private ModerationService _moderationService = null!;
    private Mock<ITelegramBotClient> _mockTelegramClient = null!;
    private Mock<IUserManager> _mockUserManager = null!;
    private Mock<ILogger<ModerationService>> _mockLogger = null!;

    [SetUp]
    public void Setup()
    {
        _mockTelegramClient = MockTelegram.CreateMockBotClient();
        _mockUserManager = new Mock<IUserManager>();
        _mockLogger = new Mock<ILogger<ModerationService>>();
        
        _moderationService = new ModerationService(
            _mockTelegramClient.Object,
            _mockUserManager.Object,
            _mockLogger.Object
        );
    }

    [Test]
    [Category("message_moderation")]
    public void CheckMessageAsync_ValidMessage_ReturnsAllowAction()
    {
        // Arrange
        var message = MockTelegram.CreateTestMessage(SampleMessages.Valid.SimpleText);
        
        // Act
        var result = _moderationService.CheckMessageAsync(message).Result;
        
        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.EqualTo("Сообщение прошло проверку"));
    }

    [Test]
    [Category("message_moderation")]
    public void CheckMessageAsync_SpamMessage_ReturnsBanAction()
    {
        // Arrange
        var message = MockTelegram.CreateTestMessage(SampleMessages.Spam.SimpleSpam);
        
        // Act
        var result = _moderationService.CheckMessageAsync(message).Result;
        
        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Is.EqualTo("Обнаружен спам"));
    }

    [Test]
    [Category("message_moderation")]
    public void CheckMessageAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _moderationService.CheckMessageAsync(null!)
        );
        
        Assert.That(exception.ParamName, Is.EqualTo("message"));
    }

    [Test]
    [Category("message_moderation")]
    public void CheckMessageAsync_EmptyMessage_ReturnsAllowAction()
    {
        // Arrange
        var message = MockTelegram.CreateTestMessage(SampleMessages.EdgeCases.Empty);
        
        // Act
        var result = _moderationService.CheckMessageAsync(message).Result;
        
        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
    }

    [Test]
    [Category("username_moderation")]
    public void CheckUserNameAsync_ValidUsername_ReturnsAllowAction()
    {
        // Arrange
        var user = MockTelegram.CreateTestUser(username: "valid_user");
        
        // Act
        var result = _moderationService.CheckUserNameAsync(user).Result;
        
        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
    }

    [Test]
    [Category("username_moderation")]
    public void CheckUserNameAsync_SuspiciousUsername_ReturnsWarnAction()
    {
        // Arrange
        var user = MockTelegram.CreateTestUser(username: "buy_cheap_products");
        
        // Act
        var result = _moderationService.CheckUserNameAsync(user).Result;
        
        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Warn));
        Assert.That(result.Reason, Is.EqualTo("Подозрительное имя пользователя"));
    }

    [Test]
    [Category("username_moderation")]
    public void CheckUserNameAsync_NullUser_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _moderationService.CheckUserNameAsync(null!)
        );
        
        Assert.That(exception.ParamName, Is.EqualTo("user"));
    }

    [Test]
    [Category("ban_cleanup")]
    public void BanAndCleanupUserAsync_ValidUser_BansAndCleansUp()
    {
        // Arrange
        var user = MockTelegram.CreateTestUser();
        var chat = MockTelegram.CreateTestChat();
        
        _mockTelegramClient.Setup(x => x.BanChatMemberAsync(
            It.IsAny<ChatId>(), 
            It.IsAny<long>(), 
            It.IsAny<DateTime>(), 
            It.IsAny<bool>(), 
            It.IsAny<bool>(), 
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(true);
        
        // Act
        var result = _moderationService.BanAndCleanupUserAsync(user, chat).Result;
        
        // Assert
        Assert.That(result, Is.True);
        _mockTelegramClient.Verify(x => x.BanChatMemberAsync(
            It.IsAny<ChatId>(), 
            user.Id, 
            It.IsAny<DateTime>(), 
            It.IsAny<bool>(), 
            It.IsAny<bool>(), 
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Test]
    [Category("ban_cleanup")]
    public void BanAndCleanupUserAsync_NullUser_ThrowsArgumentNullException()
    {
        // Arrange
        var chat = MockTelegram.CreateTestChat();
        
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _moderationService.BanAndCleanupUserAsync(null!, chat)
        );
        
        Assert.That(exception.ParamName, Is.EqualTo("user"));
    }

    [Test]
    [Category("ban_cleanup")]
    public void BanAndCleanupUserAsync_NullChat_ThrowsArgumentNullException()
    {
        // Arrange
        var user = MockTelegram.CreateTestUser();
        
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _moderationService.BanAndCleanupUserAsync(user, null!)
        );
        
        Assert.That(exception.ParamName, Is.EqualTo("chat"));
    }

    [Test]
    [Category("integration")]
    public void FullModerationFlow_SpamUser_GetsBanned()
    {
        // Arrange
        var user = MockTelegram.CreateTestUser(username: "spam_bot");
        var chat = MockTelegram.CreateTestChat();
        var message = MockTelegram.CreateTestMessage(SampleMessages.Spam.SimpleSpam);
        message.From = user;
        message.Chat = chat;
        
        _mockTelegramClient.Setup(x => x.BanChatMemberAsync(
            It.IsAny<ChatId>(), 
            It.IsAny<long>(), 
            It.IsAny<DateTime>(), 
            It.IsAny<bool>(), 
            It.IsAny<bool>(), 
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(true);
        
        // Act
        var messageResult = _moderationService.CheckMessageAsync(message).Result;
        var usernameResult = _moderationService.CheckUserNameAsync(user).Result;
        var banResult = _moderationService.BanAndCleanupUserAsync(user, chat).Result;
        
        // Assert
        Assert.That(messageResult.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(usernameResult.Action, Is.EqualTo(ModerationAction.Warn));
        Assert.That(banResult, Is.True);
    }
} 
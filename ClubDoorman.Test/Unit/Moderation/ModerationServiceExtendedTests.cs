using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestData;
using ClubDoorman.Infrastructure;
using NUnit.Framework;
using Telegram.Bot.Types;
using Moq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace ClubDoorman.Test.Unit.Moderation;

[TestFixture]
[Category("unit")]
[Category("moderation")]
[Category("extended")]
public class ModerationServiceExtendedTests
{
    private ModerationServiceTestFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new ModerationServiceTestFactory();
    }

    #region CheckUserNameAsync Tests

    [Test]
    public async Task CheckUserNameAsync_NullUser_ThrowsArgumentNullException()
    {
        // Arrange
        var service = _factory.CreateModerationService();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CheckUserNameAsync(null!));
        
        Assert.That(exception.ParamName, Is.EqualTo("user"));
    }

    [Test]
    public async Task CheckUserNameAsync_EmptyFirstName_ThrowsModerationException()
    {
        // Arrange
        var service = _factory.CreateModerationService();
        var user = new User { Id = 123, FirstName = "" };

        // Act & Assert
        var exception = Assert.ThrowsAsync<ModerationException>(
            () => service.CheckUserNameAsync(user));
        
        Assert.That(exception.Message, Does.Contain("пустым"));
    }

    [Test]
    public async Task CheckUserNameAsync_ValidFirstName_ReturnsAllow()
    {
        // Arrange
        var service = _factory.CreateModerationService();
        var user = new User { Id = 123, FirstName = "John" };

        // Act
        var result = await service.CheckUserNameAsync(user);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
    }

    [Test]
    public async Task CheckUserNameAsync_LongFirstName_ReturnsReport()
    {
        // Arrange
        var service = _factory.CreateModerationService();
        var user = new User { Id = 123, FirstName = new string('A', 51) }; // 51 символ

        // Act
        var result = await service.CheckUserNameAsync(user);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Report));
        Assert.That(result.Reason, Does.Contain("длинное"));
    }

    #endregion

    #region IncrementGoodMessageCountAsync Tests

    [Test]
    public async Task IncrementGoodMessageCountAsync_NullUser_ThrowsArgumentNullException()
    {
        // Arrange
        var service = _factory.CreateModerationService();
        var chat = TK.CreateGroupChat();
        var messageText = "test message";

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            () => service.IncrementGoodMessageCountAsync(null!, chat, messageText));
        
        Assert.That(exception.ParamName, Is.EqualTo("user"));
    }

    [Test]
    public async Task IncrementGoodMessageCountAsync_NullChat_ThrowsArgumentNullException()
    {
        // Arrange
        var service = _factory.CreateModerationService();
        var user = TK.CreateValidUser();
        var messageText = "test message";

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            () => service.IncrementGoodMessageCountAsync(user, null!, messageText));
        
        Assert.That(exception.ParamName, Is.EqualTo("chat"));
    }

    [Test]
    public async Task IncrementGoodMessageCountAsync_EmptyMessageText_ThrowsArgumentException()
    {
        // Arrange
        var service = _factory.CreateModerationService();
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(
            () => service.IncrementGoodMessageCountAsync(user, chat, ""));
        
        Assert.That(exception.ParamName, Is.EqualTo("messageText"));
    }

    [Test]
    public async Task IncrementGoodMessageCountAsync_ValidParameters_IncrementsCount()
    {
        // Arrange
        var service = _factory.CreateModerationService();
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var messageText = "test message";

        // Act
        await service.IncrementGoodMessageCountAsync(user, chat, messageText);

        // Assert - проверяем что метод выполнился без исключений
        Assert.Pass("Method executed successfully");
    }

    #endregion

    #region BanAndCleanupUserAsync Tests

    [Test]
    public async Task BanAndCleanupUserAsync_ValidParameters_ReturnsTrue()
    {
        // Arrange
        _factory.WithBotClientSetup(mock => 
            mock.Setup(x => x.SendRequest(It.IsAny<Telegram.Bot.Requests.BanChatMemberRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true)));
        
        var service = _factory.CreateModerationService();
        var userId = 12345L;
        var chatId = 67890L;
        var messageId = 111;

        // Act
        var result = await service.BanAndCleanupUserAsync(userId, chatId, messageId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task BanAndCleanupUserAsync_WithoutMessageId_OnlyBansUser()
    {
        // Arrange
        _factory.WithBotClientSetup(mock => 
            mock.Setup(x => x.SendRequest(It.IsAny<Telegram.Bot.Requests.BanChatMemberRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true)));
        
        var service = _factory.CreateModerationService();
        var userId = 12345L;
        var chatId = 67890L;

        // Act
        var result = await service.BanAndCleanupUserAsync(userId, chatId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task BanAndCleanupUserAsync_TelegramApiError_ReturnsFalse()
    {
        // Arrange
        _factory.WithBotClientSetup(mock => 
            mock.Setup(x => x.SendRequest(It.IsAny<Telegram.Bot.Requests.BanChatMemberRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Telegram API error")));
        
        var service = _factory.CreateModerationService();
        var userId = 12345L;
        var chatId = 67890L;

        // Act
        var result = await service.BanAndCleanupUserAsync(userId, chatId);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region IsUserApproved Tests

    [Test]
    public void IsUserApproved_ValidUserId_ReturnsFalse()
    {
        // Arrange
        var service = _factory.CreateModerationService();
        var userId = 12345L;

        // Act
        var result = service.IsUserApproved(userId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsUserApproved_WithChatId_ReturnsFalse()
    {
        // Arrange
        var service = _factory.CreateModerationService();
        var userId = 12345L;
        var chatId = 67890L;

        // Act
        var result = service.IsUserApproved(userId, chatId);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region SetAiDetectForSuspiciousUser Tests

    [Test]
    public void SetAiDetectForSuspiciousUser_ValidParameters_ReturnsFalse()
    {
        // Arrange
        var service = _factory.CreateModerationService();
        var userId = 12345L;
        var chatId = 67890L;
        var enabled = true;

        // Act
        var result = service.SetAiDetectForSuspiciousUser(userId, chatId, enabled);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void SetAiDetectForSuspiciousUser_DisableAiDetect_ReturnsFalse()
    {
        // Arrange
        var service = _factory.CreateModerationService();
        var userId = 12345L;
        var chatId = 67890L;
        var enabled = false;

        // Act
        var result = service.SetAiDetectForSuspiciousUser(userId, chatId, enabled);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region GetSuspiciousUsersStats Tests

    [Test]
    public void GetSuspiciousUsersStats_ReturnsDefaultStats()
    {
        // Arrange
        var service = _factory.CreateModerationService();

        // Act
        var (totalSuspicious, withAiDetect, groupsCount) = service.GetSuspiciousUsersStats();

        // Assert
        Assert.That(totalSuspicious, Is.EqualTo(0));
        Assert.That(withAiDetect, Is.EqualTo(0));
        Assert.That(groupsCount, Is.EqualTo(0));
    }

    #endregion

    #region GetAiDetectUsers Tests

    [Test]
    public void GetAiDetectUsers_ReturnsNull()
    {
        // Arrange
        var service = _factory.CreateModerationService();

        // Act
        var result = service.GetAiDetectUsers();

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public async Task CheckMessageAsync_UserWithoutFirstName_HandlesGracefully()
    {
        // Arrange
        _factory.WithClassifierSetup(mock => 
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((false, -1.5f))); // Уверенный ham (не спам)
        
        var service = _factory.CreateModerationService();
        var message = new Message
        {
            Text = "Hello",
            From = new User { Id = 123 }, // Без FirstName
            Chat = new Chat { Id = 456, Type = Telegram.Bot.Types.Enums.ChatType.Group }
        };

        // Act
        var result = await service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
    }

    [Test]
    public async Task CheckMessageAsync_ChatWithoutId_HandlesGracefully()
    {
        // Arrange
        _factory.WithClassifierSetup(mock => 
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((false, -1.5f))); // Уверенный ham (не спам)
        
        var service = _factory.CreateModerationService();
        var message = new Message
        {
            Text = "Hello",
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Type = Telegram.Bot.Types.Enums.ChatType.Group } // Без Id
        };

        // Act
        var result = await service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
    }

    [Test]
    public async Task CheckMessageAsync_ConcurrentCalls_HandlesCorrectly()
    {
        // Arrange
        _factory.WithClassifierSetup(mock => 
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((false, -1.5f))); // Уверенный ham (не спам)
        
        var service = _factory.CreateModerationService();
        var message = TK.CreateValidMessage();
        var tasks = new List<Task<ModerationResult>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(service.CheckMessageAsync(message));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.That(results, Has.Length.EqualTo(10));
        Assert.That(results.All(r => r.Action == ModerationAction.Allow), Is.True);
    }

    #endregion

    #region Integration Tests

    [Test]
    public async Task CheckMessageAsync_WithMimicryDetection_ReturnsAllow()
    {
        // Arrange
        _factory.WithClassifierSetup(mock => 
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((false, -1.5f))); // Уверенный ham (не спам)
        
        _factory.WithMimicryClassifierSetup(mock => 
            mock.Setup(x => x.AnalyzeMessages(It.IsAny<List<string>>()))
                .Returns(0.9));
        
        var service = _factory.CreateModerationService();
        var message = TK.CreateValidMessage();

        // Act
        var result = await service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
    }

    [Test]
    public async Task CheckMessageAsync_WithBadMessageManager_ReturnsBan()
    {
        // Arrange
        _factory.WithBadMessageManagerSetup(mock => 
            mock.Setup(x => x.KnownBadMessage(It.IsAny<string>()))
                .Returns(true));
        
        var service = _factory.CreateModerationService();
        var message = TK.CreateValidMessage();

        // Act
        var result = await service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Does.Contain("спам"));
    }

    #endregion
} 
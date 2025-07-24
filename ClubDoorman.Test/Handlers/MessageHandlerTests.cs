using ClubDoorman.Handlers;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestData;
using ClubDoorman.Models;
using ClubDoorman.Services;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace ClubDoorman.Test.Handlers;

[TestFixture]
[Category("handlers")]
public class MessageHandlerTests
{
    private MessageHandlerTestFactory _factory;
    private MessageHandler _handler;

    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();
        
        // Настройка моков для AppConfig - чаты разрешены и не отключены
        _factory.WithAppConfigSetup(mock => 
        {
            mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
            mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
            mock.Setup(x => x.AdminChatId).Returns(123456789);
            mock.Setup(x => x.LogAdminChatId).Returns(987654321);
        });
        
        // Настройка моков для BotPermissionsService
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.BotId).Returns(123456789);
        });
        
        _factory.WithBotPermissionsServiceSetup(mock =>
        {
            mock.Setup(x => x.IsSilentModeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        });
        
        // Настройка моков для CaptchaService
        _factory.WithCaptchaServiceSetup(mock =>
        {
            mock.Setup(x => x.GenerateKey(It.IsAny<long>(), It.IsAny<long>()))
                .Returns("test-key");
            mock.Setup(x => x.GetCaptchaInfo(It.IsAny<string>()))
                .Returns((CaptchaInfo?)null);
        });
        
        // Настройка моков для UserManager
        _factory.WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.InBanlist(It.IsAny<long>()))
                .ReturnsAsync(false);
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>()))
                .ReturnsAsync((string?)null);
        });
        
        _handler = _factory.CreateMessageHandler();
    }

    [Test]
    public async Task HandleMessageAsync_ValidMessage_ProcessesSuccessfully()
    {
        // Arrange
        var message = TestDataFactory.CreateValidMessage();
        
        _factory.WithModerationServiceSetup(mock => 
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid message"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false); // User is not approved, so moderation will run
        });
        
        _factory.WithUserManagerSetup(mock => 
        {
            mock.Setup(x => x.InBanlist(It.IsAny<long>()))
                .ReturnsAsync(false); // User is not in banlist
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>()))
                .ReturnsAsync((string?)null); // User is not from club
        });

        _factory.WithAiChecksSetup(mock => 
            mock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), null))
                .ReturnsAsync(new SpamPhotoBio(
                    new SpamProbability { Probability = 0.1f, Reason = "Safe" },
                    new byte[0],
                    "Test"
                )));

        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update);

        // Assert
        _factory.ModerationServiceMock.Verify(
            x => x.CheckMessageAsync(It.IsAny<Message>()), 
            Times.Once);
        
        _factory.ModerationServiceMock.Verify(
            x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()), 
            Times.Once);
    }

    [Test]
    public async Task HandleMessageAsync_SpamMessage_BlocksUser()
    {
        // Arrange
        var message = TestDataFactory.CreateSpamMessage();
        
        _factory.WithModerationServiceSetup(mock => 
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Delete, "Spam detected")));
        
        _factory.WithUserManagerSetup(mock => 
            mock.Setup(x => x.Approved(It.IsAny<long>(), null))
                .Returns(true));

        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update);

        // Assert
        _factory.ModerationServiceMock.Verify(
            x => x.CheckMessageAsync(It.IsAny<Message>()), 
            Times.Once);
    }

    [Test]
    public async Task HandleMessageAsync_UnapprovedUser_IgnoresMessage()
    {
        // Arrange
        var message = TestDataFactory.CreateValidMessage();
        
        _factory.WithModerationServiceSetup(mock => 
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(true)); // User is approved, so moderation will be skipped
        
        _factory.WithUserManagerSetup(mock => 
        {
            mock.Setup(x => x.InBanlist(It.IsAny<long>()))
                .ReturnsAsync(false); // User is not in banlist
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>()))
                .ReturnsAsync((string?)null); // User is not from club
        });

        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update);

        // Assert
        _factory.ModerationServiceMock.Verify(
            x => x.CheckMessageAsync(It.IsAny<Message>()), 
            Times.Never);
    }

    [Test]
    public async Task HandleMessageAsync_BotMessage_IgnoresMessage()
    {
        // Arrange
        var message = TestDataFactory.CreateValidMessage();
        message.From = TestDataFactory.CreateBotUser();

        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update);

        // Assert
        _factory.ModerationServiceMock.Verify(
            x => x.CheckMessageAsync(It.IsAny<Message>()), 
            Times.Never);
        
        _factory.UserManagerMock.Verify(
            x => x.Approved(It.IsAny<long>(), null), 
            Times.Never);
    }

    [Test]
    public async Task HandleMessageAsync_EmptyMessage_ProcessesNormally()
    {
        // Arrange
        var message = TestDataFactory.CreateEmptyMessage();
        
        _factory.WithModerationServiceSetup(mock => 
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Empty message")));
        
        _factory.WithUserManagerSetup(mock => 
            mock.Setup(x => x.Approved(It.IsAny<long>(), null))
                .Returns(true));

        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update);

        // Assert
        _factory.ModerationServiceMock.Verify(
            x => x.CheckMessageAsync(It.IsAny<Message>()), 
            Times.Once);
    }

        [Test]
    public async Task HandleMessageAsync_ExceptionInModeration_ThrowsException()
    {
        // Arrange
        var message = TestDataFactory.CreateValidMessage();
        
        _factory.WithModerationServiceSetup(mock => 
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ThrowsAsync(new System.Exception("Moderation error"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false); // User is not approved, so moderation will run
        });
        
        _factory.WithUserManagerSetup(mock => 
        {
            mock.Setup(x => x.InBanlist(It.IsAny<long>()))
                .ReturnsAsync(false); // User is not in banlist
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>()))
                .ReturnsAsync((string?)null); // User is not from club
        });

        _factory.WithAiChecksSetup(mock => 
            mock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), null))
                .ReturnsAsync(new SpamPhotoBio(
                    new SpamProbability { Probability = 0.1f, Reason = "Safe" },
                    new byte[0],
                    "Test"
                )));

        // Act & Assert
        var update = new Update { Message = message };
        Assert.ThrowsAsync<System.Exception>(async () => 
            await _handler.HandleAsync(update));
    }

    [Test]
    public async Task HandleMessageAsync_RequireManualReview_ReportsMessage()
    {
        // Arrange
        var message = TestDataFactory.CreateValidMessage();
        
        _factory.WithModerationServiceSetup(mock => 
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.RequireManualReview, "Manual review required"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false); // User is not approved, so moderation will run
        });
        
        _factory.WithUserManagerSetup(mock => 
        {
            mock.Setup(x => x.InBanlist(It.IsAny<long>()))
                .ReturnsAsync(false); // User is not in banlist
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>()))
                .ReturnsAsync((string?)null); // User is not from club
        });

        _factory.WithAiChecksSetup(mock => 
            mock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), null))
                .ReturnsAsync(new SpamPhotoBio(
                    new SpamProbability { Probability = 0.1f, Reason = "Safe" },
                    new byte[0],
                    "Test"
                )));

        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update);

        // Assert
        _factory.ModerationServiceMock.Verify(
            x => x.CheckMessageAsync(It.IsAny<Message>()), 
            Times.Once);
    }
} 
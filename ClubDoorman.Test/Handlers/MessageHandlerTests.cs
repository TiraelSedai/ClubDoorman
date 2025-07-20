using ClubDoorman.Handlers;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestData;
using ClubDoorman.Models;
using ClubDoorman.Services;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using System.Threading.Tasks;

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
        _handler = _factory.CreateMessageHandler();
    }

    [Test]
    public async Task HandleMessageAsync_ValidMessage_ProcessesSuccessfully()
    {
        // Arrange
        var message = TestDataFactory.CreateValidMessage();
        
        _factory.WithModerationServiceSetup(mock => 
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid message")));
        
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
        
        _factory.UserManagerMock.Verify(
            x => x.Approved(It.IsAny<long>(), null), 
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
        
        _factory.WithUserManagerSetup(mock => 
            mock.Setup(x => x.Approved(It.IsAny<long>(), null))
                .Returns(false));

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
    public async Task HandleMessageAsync_ExceptionInModeration_LogsError()
    {
        // Arrange
        var message = TestDataFactory.CreateValidMessage();
        
        _factory.WithModerationServiceSetup(mock => 
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ThrowsAsync(new System.Exception("Moderation error")));
        
        _factory.WithUserManagerSetup(mock => 
            mock.Setup(x => x.Approved(It.IsAny<long>(), null))
                .Returns(true));

        // Act & Assert
        var update = new Update { Message = message };
        Assert.DoesNotThrowAsync(async () => 
            await _handler.HandleAsync(update));
        
        _factory.LoggerMock.Verify(
            x => x.Log(
                It.IsAny<Microsoft.Extensions.Logging.LogLevel>(),
                It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task HandleMessageAsync_CaptchaRequired_ShowsCaptcha()
    {
        // Arrange
        var message = TestDataFactory.CreateValidMessage();
        
        _factory.WithModerationServiceSetup(mock => 
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.RequireManualReview, "Captcha required")));
        
        _factory.WithUserManagerSetup(mock => 
            mock.Setup(x => x.Approved(It.IsAny<long>(), null))
                .Returns(true));

        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update);

        // Assert
        _factory.CaptchaServiceMock.Verify(
            x => x.CreateCaptchaAsync(It.IsAny<Chat>(), It.IsAny<User>(), It.IsAny<Message>()), 
            Times.Once);
    }
} 
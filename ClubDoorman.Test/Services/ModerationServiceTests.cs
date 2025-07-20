using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestData;
using ClubDoorman.Models;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using System.Threading.Tasks;

namespace ClubDoorman.Test.Services;

[TestFixture]
[Category("services")]
public class ModerationServiceTests
{
    private ModerationServiceTestFactory _factory;
    private ModerationService _service;

    [SetUp]
    public void Setup()
    {
        _factory = new ModerationServiceTestFactory();
        _service = _factory.CreateModerationService();
    }

    [Test]
    public async Task CheckMessageAsync_ValidMessage_ReturnsAllow()
    {
        // Arrange
        var message = TestDataFactory.CreateValidMessage();
        
        _factory.WithClassifierSetup(mock => 
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((false, 0.2f)));
        
        _factory.WithMimicryClassifierSetup(mock => 
            mock.Setup(x => x.AnalyzeMessages(It.IsAny<List<string>>()))
                .Returns(0.1));

        // Act
        var result = await _service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.Not.Empty);
    }

    [Test]
    public async Task CheckMessageAsync_SpamMessage_ReturnsDelete()
    {
        // Arrange
        var message = TestDataFactory.CreateSpamMessage();
        
        _factory.WithClassifierSetup(mock => 
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((true, 0.8f)));
        
        _factory.WithMimicryClassifierSetup(mock => 
            mock.Setup(x => x.AnalyzeMessages(It.IsAny<List<string>>()))
                .Returns(0.1));

        // Act
        var result = await _service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
        Assert.That(result.Reason, Is.Not.Empty);
    }

    [Test]
    public async Task CheckMessageAsync_EmptyMessage_ReturnsReport()
    {
        // Arrange
        var message = TestDataFactory.CreateEmptyMessage();
        
        _factory.WithClassifierSetup(mock => 
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((false, 0.2f)));
        
        _factory.WithMimicryClassifierSetup(mock => 
            mock.Setup(x => x.AnalyzeMessages(It.IsAny<List<string>>()))
                .Returns(0.1));

        // Act
        var result = await _service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Report));
        Assert.That(result.Reason, Contains.Substring("Медиа без подписи"));
    }

    [Test]
    public async Task CheckMessageAsync_BothClassifiersReturnSpam_ReturnsDelete()
    {
        // Arrange
        var message = TestDataFactory.CreateSpamMessage();
        
        _factory.WithClassifierSetup(mock => 
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((true, 0.8f)));
        
        _factory.WithMimicryClassifierSetup(mock => 
            mock.Setup(x => x.AnalyzeMessages(It.IsAny<List<string>>()))
                .Returns(0.1));

        // Act
        var result = await _service.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
        Assert.That(result.Reason, Contains.Substring("спам"));
    }

    [Test]
    public async Task CheckMessageAsync_ExceptionInClassifier_ThrowsException()
    {
        // Arrange
        var message = TestDataFactory.CreateValidMessage();
        
        _factory.WithClassifierSetup(mock => 
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ThrowsAsync(new System.Exception("Test exception")));
        
        _factory.WithMimicryClassifierSetup(mock => 
            mock.Setup(x => x.AnalyzeMessages(It.IsAny<List<string>>()))
                .Returns(0.1));

        // Act & Assert
        var exception = Assert.ThrowsAsync<System.Exception>(async () => 
            await _service.CheckMessageAsync(message));
        
        Assert.That(exception.Message, Is.EqualTo("Test exception"));
    }

    [Test]
    public async Task CheckMessageAsync_BotUser_ReturnsDelete()
    {
        // Arrange
        var message = TestDataFactory.CreateValidMessage();
        message.From = TestDataFactory.CreateBotUser();
        
        _factory.WithClassifierSetup(mock => 
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((true, 0.8f)));
        
        _factory.WithMimicryClassifierSetup(mock => 
            mock.Setup(x => x.AnalyzeMessages(It.IsAny<List<string>>()))
                .Returns(0.1));

        // Act
        var result = await _service.CheckMessageAsync(message);

        // Assert
        // Bot users are handled in MessageHandler, not ModerationService
        // The ModerationService will process them normally
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
        Assert.That(result.Reason, Contains.Substring("спам"));
    }
} 
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
public class ModerationServiceSimpleTests
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
    public void CreateModerationService_WithFactory_ReturnsValidInstance()
    {
        // Arrange & Act
        var service = _factory.CreateModerationService();

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<ModerationService>());
    }

    [Test]
    public void Factory_ProvidesAllMocks()
    {
        // Assert
        Assert.That(_factory.ClassifierMock, Is.Not.Null);
        Assert.That(_factory.MimicryClassifierMock, Is.Not.Null);
        Assert.That(_factory.BadMessageManagerMock, Is.Not.Null);
        Assert.That(_factory.UserManagerMock, Is.Not.Null);
        Assert.That(_factory.AiChecksMock, Is.Not.Null);
        Assert.That(_factory.SuspiciousUsersStorageMock, Is.Not.Null);
        Assert.That(_factory.LoggerMock, Is.Not.Null);
        Assert.That(_factory.FakeTelegramClient, Is.Not.Null);
    }

    [Test]
    public void Factory_WithFluentApi_ConfiguresMocks()
    {
        // Arrange
        var expectedResult = new ModerationResult(ModerationAction.Allow, "Test");

        // Act
        _factory.WithClassifierSetup(mock => 
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((false, 0.2f)));

        // Assert
        Assert.That(_factory.ClassifierMock, Is.Not.Null);
    }

    [Test]
    public void TestDataFactory_CreatesValidMessage()
    {
        // Act
        var message = TestDataFactory.CreateValidMessage();

        // Assert
        Assert.That(message, Is.Not.Null);
        Assert.That(message.MessageId, Is.EqualTo(0)); // MessageId is read-only, defaults to 0
        Assert.That(message.Text, Is.EqualTo("Hello, this is a valid message!"));
        Assert.That(message.From, Is.Not.Null);
        Assert.That(message.Chat, Is.Not.Null);
    }

    [Test]
    public void TestDataFactory_CreatesSpamMessage()
    {
        // Act
        var message = TestDataFactory.CreateSpamMessage();

        // Assert
        Assert.That(message, Is.Not.Null);
        Assert.That(message.MessageId, Is.EqualTo(0)); // MessageId is read-only, defaults to 0
        Assert.That(message.Text, Is.EqualTo("BUY NOW!!! AMAZING OFFER!!! CLICK HERE!!!"));
    }

    [Test]
    public void TestDataFactory_CreatesValidUser()
    {
        // Act
        var user = TestDataFactory.CreateValidUser();

        // Assert
        Assert.That(user, Is.Not.Null);
        Assert.That(user.Id, Is.EqualTo(123456789));
        Assert.That(user.FirstName, Is.EqualTo("Test"));
        Assert.That(user.LastName, Is.EqualTo("User"));
        Assert.That(user.Username, Is.EqualTo("testuser"));
        Assert.That(user.IsBot, Is.False);
    }

    [Test]
    public void TestDataFactory_CreatesBotUser()
    {
        // Act
        var user = TestDataFactory.CreateBotUser();

        // Assert
        Assert.That(user, Is.Not.Null);
        Assert.That(user.Id, Is.EqualTo(987654321));
        Assert.That(user.FirstName, Is.EqualTo("TestBot"));
        Assert.That(user.Username, Is.EqualTo("testbot"));
        Assert.That(user.IsBot, Is.True);
    }

    [Test]
    public void TestDataFactory_CreatesGroupChat()
    {
        // Act
        var chat = TestDataFactory.CreateGroupChat();

        // Assert
        Assert.That(chat, Is.Not.Null);
        Assert.That(chat.Id, Is.EqualTo(-1001234567890));
        Assert.That(chat.Type, Is.EqualTo(Telegram.Bot.Types.Enums.ChatType.Group));
        Assert.That(chat.Title, Is.EqualTo("Test Group"));
        Assert.That(chat.Username, Is.EqualTo("testgroup"));
    }
} 
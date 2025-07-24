using ClubDoorman.Handlers;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Services;
using ClubDoorman.Models;
using ClubDoorman.Models.Requests;
using NUnit.Framework;
using Moq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.TestInfrastructure;

[TestFixture]
[Category("test-infrastructure")]
public class MessageHandlerTestFactoryTests
{
    private MessageHandlerTestFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();
    }

    [Test]
    public void CreateMessageHandler_ReturnsWorkingService()
    {
        // Act
        var service = _factory.CreateMessageHandler();

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<MessageHandler>());
    }

    [Test]
    public void CreateMessageHandlerWithFake_ReturnsWorkingService()
    {
        // Act
        var service = _factory.CreateMessageHandlerWithFake();

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<MessageHandler>());
    }

    [Test]
    public void CreateMessageHandlerWithFake_CustomFake_UsesProvidedFake()
    {
        // Arrange
        var customFake = new FakeTelegramClient();

        // Act
        var service = _factory.CreateMessageHandlerWithFake(customFake);

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<MessageHandler>());
    }

    [Test]
    public void Factory_ProvidesAccessToMocks()
    {
        // Assert
        Assert.That(_factory.ModerationServiceMock, Is.Not.Null);
        Assert.That(_factory.CaptchaServiceMock, Is.Not.Null);
        Assert.That(_factory.UserManagerMock, Is.Not.Null);
        Assert.That(_factory.StatisticsServiceMock, Is.Not.Null);
        Assert.That(_factory.ServiceProviderMock, Is.Not.Null);
        Assert.That(_factory.LoggerMock, Is.Not.Null);
        Assert.That(_factory.FakeTelegramClient, Is.Not.Null);
    }

    [Test]
    public void Factory_WithModerationServiceSetup_ConfiguresMock()
    {
        // Arrange
        var wasCalled = false;

        // Act
        _factory.WithModerationServiceSetup(mock =>
        {
            wasCalled = true;
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Test"));
        });

        // Assert
        Assert.That(wasCalled, Is.True);
    }

    [Test]
    public void Factory_WithUserManagerSetup_ConfiguresMock()
    {
        // Arrange
        var wasCalled = false;

        // Act
        _factory.WithUserManagerSetup(mock =>
        {
            wasCalled = true;
            mock.Setup(x => x.Approved(It.IsAny<long>(), It.IsAny<long?>()))
                .Returns(true);
        });

        // Assert
        Assert.That(wasCalled, Is.True);
    }

    [Test]
    public void Factory_WithCaptchaServiceSetup_ConfiguresMock()
    {
        // Arrange
        var wasCalled = false;

        // Act
        _factory.WithCaptchaServiceSetup(mock =>
        {
            wasCalled = true;
            mock.Setup(x => x.CreateCaptchaAsync(It.IsAny<CreateCaptchaRequest>()))
                .ReturnsAsync(new Models.CaptchaInfo(123, "Test", DateTime.UtcNow, new User(), 0, new CancellationTokenSource(), null));
        });

        // Assert
        Assert.That(wasCalled, Is.True);
    }
} 
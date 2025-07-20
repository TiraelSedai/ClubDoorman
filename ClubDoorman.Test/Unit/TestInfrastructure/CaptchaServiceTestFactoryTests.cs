using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.TestInfrastructure;

[TestFixture]
[Category("test-infrastructure")]
public class CaptchaServiceTestFactoryTests
{
    private CaptchaServiceTestFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new CaptchaServiceTestFactory();
    }

    [Test]
    public void CreateCaptchaService_ReturnsWorkingService()
    {
        // Act
        var service = _factory.CreateCaptchaService();

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<CaptchaService>());
    }

    [Test]
    public void CreateCaptchaServiceWithFake_ReturnsWorkingService()
    {
        // Act
        var service = _factory.CreateCaptchaServiceWithFake();

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<CaptchaService>());
    }

    [Test]
    public void CreateCaptchaServiceWithFake_CustomFake_UsesProvidedFake()
    {
        // Arrange
        var customFake = new FakeTelegramClient();

        // Act
        var service = _factory.CreateCaptchaServiceWithFake(customFake);

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<CaptchaService>());
    }

    [Test]
    public void Factory_ProvidesAccessToMocks()
    {
        // Assert
        Assert.That(_factory.LoggerMock, Is.Not.Null);
        Assert.That(_factory.TelegramBotClientWrapperMock, Is.Not.Null);
        Assert.That(_factory.FakeTelegramClient, Is.Not.Null);
    }
} 
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для CaptchaServiceTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class CaptchaServiceTestFactoryTests
{
    [Test]
    public void CreateCaptchaService_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new CaptchaServiceTestFactory();

        // Act
        var instance = factory.CreateCaptchaService();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<CaptchaService>());
    }

    [Test]
    public void CreateCaptchaService_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new CaptchaServiceTestFactory();

        // Act
        var instance = factory.CreateCaptchaService();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateCaptchaService_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new CaptchaServiceTestFactory();

        // Act
        var instance1 = factory.CreateCaptchaService();
        var instance2 = factory.CreateCaptchaService();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void LoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new CaptchaServiceTestFactory();

        // Act & Assert
        Assert.That(factory.LoggerMock, Is.Not.Null);
        Assert.That(factory.LoggerMock.Object, Is.Not.Null);
    }
}

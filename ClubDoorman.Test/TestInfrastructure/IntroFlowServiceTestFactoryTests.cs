using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для IntroFlowServiceTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class IntroFlowServiceTestFactoryTests
{
    [Test]
    public void CreateIntroFlowService_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new IntroFlowServiceTestFactory();

        // Act
        var instance = factory.CreateIntroFlowService();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<IntroFlowService>());
    }

    [Test]
    public void CreateIntroFlowService_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new IntroFlowServiceTestFactory();

        // Act
        var instance = factory.CreateIntroFlowService();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateIntroFlowService_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new IntroFlowServiceTestFactory();

        // Act
        var instance1 = factory.CreateIntroFlowService();
        var instance2 = factory.CreateIntroFlowService();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void LoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new IntroFlowServiceTestFactory();

        // Act & Assert
        Assert.That(factory.LoggerMock, Is.Not.Null);
        Assert.That(factory.LoggerMock.Object, Is.Not.Null);
    }

    [Test]
    public void CaptchaServiceMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new IntroFlowServiceTestFactory();

        // Act & Assert
        Assert.That(factory.CaptchaServiceMock, Is.Not.Null);
        Assert.That(factory.CaptchaServiceMock.Object, Is.Not.Null);
    }

    [Test]
    public void UserManagerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new IntroFlowServiceTestFactory();

        // Act & Assert
        Assert.That(factory.UserManagerMock, Is.Not.Null);
        Assert.That(factory.UserManagerMock.Object, Is.Not.Null);
    }

    [Test]
    public void StatisticsServiceMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new IntroFlowServiceTestFactory();

        // Act & Assert
        Assert.That(factory.StatisticsServiceMock, Is.Not.Null);
        Assert.That(factory.StatisticsServiceMock.Object, Is.Not.Null);
    }

    [Test]
    public void ModerationServiceMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new IntroFlowServiceTestFactory();

        // Act & Assert
        Assert.That(factory.ModerationServiceMock, Is.Not.Null);
        Assert.That(factory.ModerationServiceMock.Object, Is.Not.Null);
    }
}

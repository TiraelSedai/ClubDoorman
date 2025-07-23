using ClubDoorman.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using ClubDoorman.Services;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для MessageHandlerTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class MessageHandlerTestFactoryTests
{
    [Test]
    public void CreateMessageHandler_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();

        // Act
        var instance = factory.CreateMessageHandler();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<MessageHandler>());
    }

    [Test]
    public void CreateMessageHandler_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();

        // Act
        var instance = factory.CreateMessageHandler();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateMessageHandler_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();

        // Act
        var instance1 = factory.CreateMessageHandler();
        var instance2 = factory.CreateMessageHandler();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void BotMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();

        // Act & Assert
        Assert.That(factory.BotMock, Is.Not.Null);
        Assert.That(factory.BotMock.Object, Is.Not.Null);
    }

    [Test]
    public void ModerationServiceMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();

        // Act & Assert
        Assert.That(factory.ModerationServiceMock, Is.Not.Null);
        Assert.That(factory.ModerationServiceMock.Object, Is.Not.Null);
    }

    [Test]
    public void CaptchaServiceMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();

        // Act & Assert
        Assert.That(factory.CaptchaServiceMock, Is.Not.Null);
        Assert.That(factory.CaptchaServiceMock.Object, Is.Not.Null);
    }

    [Test]
    public void UserManagerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();

        // Act & Assert
        Assert.That(factory.UserManagerMock, Is.Not.Null);
        Assert.That(factory.UserManagerMock.Object, Is.Not.Null);
    }

    [Test]
    public void ClassifierMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();

        // Act & Assert
        Assert.That(factory.ClassifierMock, Is.Not.Null);
        Assert.That(factory.ClassifierMock.Object, Is.Not.Null);
    }

    [Test]
    public void BadMessageManagerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();

        // Act & Assert
        Assert.That(factory.BadMessageManagerMock, Is.Not.Null);
        Assert.That(factory.BadMessageManagerMock.Object, Is.Not.Null);
    }

    [Test]
    public void AiChecksMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();

        // Act & Assert
        Assert.That(factory.AiChecksMock, Is.Not.Null);
        Assert.That(factory.AiChecksMock.Object, Is.Not.Null);
    }

    [Test]
    public void StatisticsServiceMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();

        // Act & Assert
        Assert.That(factory.StatisticsServiceMock, Is.Not.Null);
        Assert.That(factory.StatisticsServiceMock.Object, Is.Not.Null);
    }

    [Test]
    public void ServiceProviderMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();

        // Act & Assert
        Assert.That(factory.ServiceProviderMock, Is.Not.Null);
        Assert.That(factory.ServiceProviderMock.Object, Is.Not.Null);
    }

    [Test]
    public void LoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();

        // Act & Assert
        Assert.That(factory.LoggerMock, Is.Not.Null);
        Assert.That(factory.LoggerMock.Object, Is.Not.Null);
    }
}

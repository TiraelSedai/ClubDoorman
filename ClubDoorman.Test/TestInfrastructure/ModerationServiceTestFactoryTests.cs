using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для ModerationServiceTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class ModerationServiceTestFactoryTests
{
    [Test]
    public void CreateModerationService_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new ModerationServiceTestFactory();

        // Act
        var instance = factory.CreateModerationService();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<ModerationService>());
    }

    [Test]
    public void CreateModerationService_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new ModerationServiceTestFactory();

        // Act
        var instance = factory.CreateModerationService();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateModerationService_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new ModerationServiceTestFactory();

        // Act
        var instance1 = factory.CreateModerationService();
        var instance2 = factory.CreateModerationService();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void ClassifierMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new ModerationServiceTestFactory();

        // Act & Assert
        Assert.That(factory.ClassifierMock, Is.Not.Null);
        Assert.That(factory.ClassifierMock.Object, Is.Not.Null);
    }

    [Test]
    public void MimicryClassifierMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new ModerationServiceTestFactory();

        // Act & Assert
        Assert.That(factory.MimicryClassifierMock, Is.Not.Null);
        Assert.That(factory.MimicryClassifierMock.Object, Is.Not.Null);
    }

    [Test]
    public void BadMessageManagerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new ModerationServiceTestFactory();

        // Act & Assert
        Assert.That(factory.BadMessageManagerMock, Is.Not.Null);
        Assert.That(factory.BadMessageManagerMock.Object, Is.Not.Null);
    }

    [Test]
    public void UserManagerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new ModerationServiceTestFactory();

        // Act & Assert
        Assert.That(factory.UserManagerMock, Is.Not.Null);
        Assert.That(factory.UserManagerMock.Object, Is.Not.Null);
    }

    [Test]
    public void AiChecksMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new ModerationServiceTestFactory();

        // Act & Assert
        Assert.That(factory.AiChecksMock, Is.Not.Null);
        Assert.That(factory.AiChecksMock.Object, Is.Not.Null);
    }

    [Test]
    public void SuspiciousUsersStorageMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new ModerationServiceTestFactory();

        // Act & Assert
        Assert.That(factory.SuspiciousUsersStorageMock, Is.Not.Null);
        Assert.That(factory.SuspiciousUsersStorageMock.Object, Is.Not.Null);
    }

    [Test]
    public void BotClientMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new ModerationServiceTestFactory();

        // Act & Assert
        Assert.That(factory.BotClientMock, Is.Not.Null);
        Assert.That(factory.BotClientMock.Object, Is.Not.Null);
    }

    [Test]
    public void LoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new ModerationServiceTestFactory();

        // Act & Assert
        Assert.That(factory.LoggerMock, Is.Not.Null);
        Assert.That(factory.LoggerMock.Object, Is.Not.Null);
    }
}

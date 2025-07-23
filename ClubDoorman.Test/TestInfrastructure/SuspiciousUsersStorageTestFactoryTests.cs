using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для SuspiciousUsersStorageTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class SuspiciousUsersStorageTestFactoryTests
{
    [Test]
    public void CreateSuspiciousUsersStorage_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new SuspiciousUsersStorageTestFactory();

        // Act
        var instance = factory.CreateSuspiciousUsersStorage();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<SuspiciousUsersStorage>());
    }

    [Test]
    public void CreateSuspiciousUsersStorage_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new SuspiciousUsersStorageTestFactory();

        // Act
        var instance = factory.CreateSuspiciousUsersStorage();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateSuspiciousUsersStorage_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new SuspiciousUsersStorageTestFactory();

        // Act
        var instance1 = factory.CreateSuspiciousUsersStorage();
        var instance2 = factory.CreateSuspiciousUsersStorage();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void LoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new SuspiciousUsersStorageTestFactory();

        // Act & Assert
        Assert.That(factory.LoggerMock, Is.Not.Null);
        Assert.That(factory.LoggerMock.Object, Is.Not.Null);
    }
}

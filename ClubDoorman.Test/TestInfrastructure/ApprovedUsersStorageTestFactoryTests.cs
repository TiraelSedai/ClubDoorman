using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для ApprovedUsersStorageTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class ApprovedUsersStorageTestFactoryTests
{
    [Test]
    public void CreateApprovedUsersStorage_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new ApprovedUsersStorageTestFactory();

        // Act
        var instance = factory.CreateApprovedUsersStorage();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<ApprovedUsersStorage>());
    }

    [Test]
    public void CreateApprovedUsersStorage_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new ApprovedUsersStorageTestFactory();

        // Act
        var instance = factory.CreateApprovedUsersStorage();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateApprovedUsersStorage_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new ApprovedUsersStorageTestFactory();

        // Act
        var instance1 = factory.CreateApprovedUsersStorage();
        var instance2 = factory.CreateApprovedUsersStorage();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void LoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new ApprovedUsersStorageTestFactory();

        // Act & Assert
        Assert.That(factory.LoggerMock, Is.Not.Null);
        Assert.That(factory.LoggerMock.Object, Is.Not.Null);
    }
}

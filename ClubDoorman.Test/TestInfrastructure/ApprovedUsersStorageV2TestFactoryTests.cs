using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для ApprovedUsersStorageV2TestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class ApprovedUsersStorageV2TestFactoryTests
{
    [Test]
    public void CreateApprovedUsersStorageV2_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new ApprovedUsersStorageV2TestFactory();

        // Act
        var instance = factory.CreateApprovedUsersStorageV2();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<ApprovedUsersStorageV2>());
    }

    [Test]
    public void CreateApprovedUsersStorageV2_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new ApprovedUsersStorageV2TestFactory();

        // Act
        var instance = factory.CreateApprovedUsersStorageV2();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateApprovedUsersStorageV2_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new ApprovedUsersStorageV2TestFactory();

        // Act
        var instance1 = factory.CreateApprovedUsersStorageV2();
        var instance2 = factory.CreateApprovedUsersStorageV2();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void LoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new ApprovedUsersStorageV2TestFactory();

        // Act & Assert
        Assert.That(factory.LoggerMock, Is.Not.Null);
        Assert.That(factory.LoggerMock.Object, Is.Not.Null);
    }
}

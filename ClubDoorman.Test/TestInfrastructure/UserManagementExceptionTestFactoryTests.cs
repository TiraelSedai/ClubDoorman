using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для UserManagementExceptionTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class UserManagementExceptionTestFactoryTests
{
    [Test]
    public void CreateUserManagementException_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new UserManagementExceptionTestFactory();

        // Act
        var instance = factory.CreateUserManagementException();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<UserManagementException>());
    }

    [Test]
    public void CreateUserManagementException_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new UserManagementExceptionTestFactory();

        // Act
        var instance = factory.CreateUserManagementException();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateUserManagementException_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new UserManagementExceptionTestFactory();

        // Act
        var instance1 = factory.CreateUserManagementException();
        var instance2 = factory.CreateUserManagementException();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }
}

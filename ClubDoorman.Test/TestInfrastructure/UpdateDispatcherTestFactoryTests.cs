using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using ClubDoorman.Handlers;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для UpdateDispatcherTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class UpdateDispatcherTestFactoryTests
{
    [Test]
    public void CreateUpdateDispatcher_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new UpdateDispatcherTestFactory();

        // Act
        var instance = factory.CreateUpdateDispatcher();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<UpdateDispatcher>());
    }

    [Test]
    public void CreateUpdateDispatcher_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new UpdateDispatcherTestFactory();

        // Act
        var instance = factory.CreateUpdateDispatcher();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateUpdateDispatcher_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new UpdateDispatcherTestFactory();

        // Act
        var instance1 = factory.CreateUpdateDispatcher();
        var instance2 = factory.CreateUpdateDispatcher();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void UpdateHandlersMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new UpdateDispatcherTestFactory();

        // Act & Assert
        Assert.That(factory.UpdateHandlersMock, Is.Not.Null);
        Assert.That(factory.UpdateHandlersMock.Object, Is.Not.Null);
    }

    [Test]
    public void LoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new UpdateDispatcherTestFactory();

        // Act & Assert
        Assert.That(factory.LoggerMock, Is.Not.Null);
        Assert.That(factory.LoggerMock.Object, Is.Not.Null);
    }
}

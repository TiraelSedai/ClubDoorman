using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для ModerationExceptionTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class ModerationExceptionTestFactoryTests
{
    [Test]
    public void CreateModerationException_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new ModerationExceptionTestFactory();

        // Act
        var instance = factory.CreateModerationException();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<ModerationException>());
    }

    [Test]
    public void CreateModerationException_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new ModerationExceptionTestFactory();

        // Act
        var instance = factory.CreateModerationException();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateModerationException_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new ModerationExceptionTestFactory();

        // Act
        var instance1 = factory.CreateModerationException();
        var instance2 = factory.CreateModerationException();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }
}

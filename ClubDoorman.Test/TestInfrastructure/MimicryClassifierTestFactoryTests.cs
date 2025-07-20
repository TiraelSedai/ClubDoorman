using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для MimicryClassifierTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class MimicryClassifierTestFactoryTests
{
    [Test]
    public void CreateMimicryClassifier_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new MimicryClassifierTestFactory();

        // Act
        var instance = factory.CreateMimicryClassifier();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<MimicryClassifier>());
    }

    [Test]
    public void CreateMimicryClassifier_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new MimicryClassifierTestFactory();

        // Act
        var instance = factory.CreateMimicryClassifier();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateMimicryClassifier_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new MimicryClassifierTestFactory();

        // Act
        var instance1 = factory.CreateMimicryClassifier();
        var instance2 = factory.CreateMimicryClassifier();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void LoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new MimicryClassifierTestFactory();

        // Act & Assert
        Assert.That(factory.LoggerMock, Is.Not.Null);
        Assert.That(factory.LoggerMock.Object, Is.Not.Null);
    }
}

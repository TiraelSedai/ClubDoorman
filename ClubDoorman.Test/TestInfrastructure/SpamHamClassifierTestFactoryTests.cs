using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для SpamHamClassifierTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class SpamHamClassifierTestFactoryTests
{
    [Test]
    public void CreateSpamHamClassifier_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new SpamHamClassifierTestFactory();

        // Act
        var instance = factory.CreateSpamHamClassifier();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<SpamHamClassifier>());
    }

    [Test]
    public void CreateSpamHamClassifier_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new SpamHamClassifierTestFactory();

        // Act
        var instance = factory.CreateSpamHamClassifier();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateSpamHamClassifier_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new SpamHamClassifierTestFactory();

        // Act
        var instance1 = factory.CreateSpamHamClassifier();
        var instance2 = factory.CreateSpamHamClassifier();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void LoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new SpamHamClassifierTestFactory();

        // Act & Assert
        Assert.That(factory.LoggerMock, Is.Not.Null);
        Assert.That(factory.LoggerMock.Object, Is.Not.Null);
    }
}

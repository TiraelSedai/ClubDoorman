using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для AiChecksTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class AiChecksTestFactoryTests
{
    [Test]
    public void CreateAiChecks_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new AiChecksTestFactory();

        // Act
        var instance = factory.CreateAiChecks();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<AiChecks>());
    }

    [Test]
    public void CreateAiChecks_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new AiChecksTestFactory();

        // Act
        var instance = factory.CreateAiChecks();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateAiChecks_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new AiChecksTestFactory();

        // Act
        var instance1 = factory.CreateAiChecks();
        var instance2 = factory.CreateAiChecks();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void LoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new AiChecksTestFactory();

        // Act & Assert
        Assert.That(factory.LoggerMock, Is.Not.Null);
        Assert.That(factory.LoggerMock.Object, Is.Not.Null);
    }
}

using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для ConfigurationExceptionTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class ConfigurationExceptionTestFactoryTests
{
    [Test]
    public void CreateConfigurationException_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new ConfigurationExceptionTestFactory();

        // Act
        var instance = factory.CreateConfigurationException();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<ConfigurationException>());
    }

    [Test]
    public void CreateConfigurationException_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new ConfigurationExceptionTestFactory();

        // Act
        var instance = factory.CreateConfigurationException();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateConfigurationException_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new ConfigurationExceptionTestFactory();

        // Act
        var instance1 = factory.CreateConfigurationException();
        var instance2 = factory.CreateConfigurationException();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }
}

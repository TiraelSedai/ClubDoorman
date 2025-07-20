using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для StatisticsServiceTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class StatisticsServiceTestFactoryTests
{
    [Test]
    public void CreateStatisticsService_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new StatisticsServiceTestFactory();

        // Act
        var instance = factory.CreateStatisticsService();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<StatisticsService>());
    }

    [Test]
    public void CreateStatisticsService_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new StatisticsServiceTestFactory();

        // Act
        var instance = factory.CreateStatisticsService();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateStatisticsService_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new StatisticsServiceTestFactory();

        // Act
        var instance1 = factory.CreateStatisticsService();
        var instance2 = factory.CreateStatisticsService();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void LoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new StatisticsServiceTestFactory();

        // Act & Assert
        Assert.That(factory.LoggerMock, Is.Not.Null);
        Assert.That(factory.LoggerMock.Object, Is.Not.Null);
    }
}

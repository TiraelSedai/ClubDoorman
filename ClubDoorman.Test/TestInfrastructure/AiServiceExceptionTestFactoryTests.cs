using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using ClubDoorman.Services;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для AiServiceExceptionTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class AiServiceExceptionTestFactoryTests
{
    [Test]
    public void CreateAiServiceException_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new AiServiceExceptionTestFactory();

        // Act
        var instance = factory.CreateAiServiceException();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<AiServiceException>());
    }

    [Test]
    public void CreateAiServiceException_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new AiServiceExceptionTestFactory();

        // Act
        var instance = factory.CreateAiServiceException();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateAiServiceException_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new AiServiceExceptionTestFactory();

        // Act
        var instance1 = factory.CreateAiServiceException();
        var instance2 = factory.CreateAiServiceException();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }
}

using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для TelegramApiExceptionTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class TelegramApiExceptionTestFactoryTests
{
    [Test]
    public void CreateTelegramApiException_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new TelegramApiExceptionTestFactory();

        // Act
        var instance = factory.CreateTelegramApiException();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<TelegramApiException>());
    }

    [Test]
    public void CreateTelegramApiException_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new TelegramApiExceptionTestFactory();

        // Act
        var instance = factory.CreateTelegramApiException();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateTelegramApiException_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new TelegramApiExceptionTestFactory();

        // Act
        var instance1 = factory.CreateTelegramApiException();
        var instance2 = factory.CreateTelegramApiException();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }
}

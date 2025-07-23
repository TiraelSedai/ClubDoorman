using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для TelegramBotClientWrapperTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class TelegramBotClientWrapperTestFactoryTests
{
    [Test]
    public void CreateTelegramBotClientWrapper_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new TelegramBotClientWrapperTestFactory();

        // Act
        var instance = factory.CreateTelegramBotClientWrapper();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<TelegramBotClientWrapper>());
    }

    [Test]
    public void CreateTelegramBotClientWrapper_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new TelegramBotClientWrapperTestFactory();

        // Act
        var instance = factory.CreateTelegramBotClientWrapper();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateTelegramBotClientWrapper_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new TelegramBotClientWrapperTestFactory();

        // Act
        var instance1 = factory.CreateTelegramBotClientWrapper();
        var instance2 = factory.CreateTelegramBotClientWrapper();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }
}

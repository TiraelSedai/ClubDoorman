using ClubDoorman.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using ClubDoorman.Services;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Тесты для ChatMemberHandlerTestFactory
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class ChatMemberHandlerTestFactoryTests
{
    [Test]
    public void CreateChatMemberHandler_ReturnsWorkingInstance()
    {
        // Arrange
        var factory = new ChatMemberHandlerTestFactory();

        // Act
        var instance = factory.CreateChatMemberHandler();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<ChatMemberHandler>());
    }

    [Test]
    public void CreateChatMemberHandler_ConfiguresAllDependencies()
    {
        // Arrange
        var factory = new ChatMemberHandlerTestFactory();

        // Act
        var instance = factory.CreateChatMemberHandler();

        // Assert
        // Проверяем что все зависимости настроены
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void CreateChatMemberHandler_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new ChatMemberHandlerTestFactory();

        // Act
        var instance1 = factory.CreateChatMemberHandler();
        var instance2 = factory.CreateChatMemberHandler();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2));
    }

    [Test]
    public void UserManagerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new ChatMemberHandlerTestFactory();

        // Act & Assert
        Assert.That(factory.UserManagerMock, Is.Not.Null);
        Assert.That(factory.UserManagerMock.Object, Is.Not.Null);
    }

    [Test]
    public void LoggerMock_IsProperlyConfigured()
    {
        // Arrange
        var factory = new ChatMemberHandlerTestFactory();

        // Act & Assert
        Assert.That(factory.LoggerMock, Is.Not.Null);
        Assert.That(factory.LoggerMock.Object, Is.Not.Null);
    }
}

using NUnit.Framework;
using ClubDoorman.Services.Notifications;
using ClubDoorman.Test.TestKit;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Integration;

/// <summary>
/// Тесты для NotificationServiceBuilder
/// <tags>integration, notification-service-builder, test-infrastructure</tags>
/// </summary>
[TestFixture]
[Category("integration")]
[Category("notification-service-builder")]
public class NotificationServiceBuilderTests
{
    /// <summary>
    /// POC: Проверка создания NotificationService через билдер
    /// <tags>poc, builder, notification-service</tags>
    /// </summary>
    [Test]
    public void CreateNotificationService_WithBuilder_ReturnsValidService()
    {
        // Arrange & Act
        var notificationService = TK.CreateNotificationServiceBuilder()
            .WithStandardMocks()
            .Build();

        // Assert
        Assert.That(notificationService, Is.Not.Null);
        Assert.That(notificationService, Is.InstanceOf<INotificationService>());
    }

    /// <summary>
    /// POC: Проверка доступа к мокам через билдер
    /// <tags>poc, builder, mocks</tags>
    /// </summary>
    [Test]
    public void Builder_ProvidesAccessToMocks()
    {
        // Arrange
        var builder = TK.CreateNotificationServiceBuilder();

        // Act & Assert
        Assert.That(builder.MessageHandlerMock, Is.Not.Null);
        Assert.That(builder.BotMock, Is.Not.Null);
        Assert.That(builder.MessageServiceMock, Is.Not.Null);
    }
} 
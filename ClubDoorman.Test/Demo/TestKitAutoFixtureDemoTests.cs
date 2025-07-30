using ClubDoorman.Handlers;
using ClubDoorman.Services;
using ClubDoorman.Models;
using NUnit.Framework;
using ClubDoorman.Test.TestKit;

namespace ClubDoorman.Test;

/// <summary>
/// Демонстрационные тесты для показа возможностей AutoFixture
/// </summary>
[TestFixture]
[Category("demo")]
[Category("autofixture")]
public class TestKitAutoFixtureDemoTests
{
    [Test]
    public void AutoFixture_CreateMessageHandler_WorksCorrectly()
    {
        // Arrange & Act
        var handler = TestKitAutoFixture.CreateMessageHandler();

        // Assert
        Assert.That(handler, Is.Not.Null);
        Assert.That(handler, Is.InstanceOf<MessageHandler>());
    }

    [Test]
    public void AutoFixture_CreateModerationService_WorksCorrectly()
    {
        // Arrange & Act
        var service = TestKitAutoFixture.CreateModerationService();

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<ModerationService>());
    }

    [Test]
    public void AutoFixture_CreateCaptchaService_WorksCorrectly()
    {
        // Arrange & Act
        var service = TestKitAutoFixture.CreateCaptchaService();

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<CaptchaService>());
    }

    [Test]
    public void AutoFixture_CreateUserManager_WorksCorrectly()
    {
        // Arrange & Act
        var manager = TestKitAutoFixture.CreateUserManager();

        // Assert
        Assert.That(manager, Is.Not.Null);
        Assert.That(manager, Is.InstanceOf<IUserManager>());
    }

    [Test]
    public void AutoFixture_CreateUpdate_WorksCorrectly()
    {
        // Arrange & Act
        var update = TestKitAutoFixture.CreateUpdate();

        // Assert
        Assert.That(update, Is.Not.Null);
        Assert.That(update, Is.InstanceOf<Telegram.Bot.Types.Update>());
    }

    [Test]
    public void AutoFixture_CreateMessageUpdate_WorksCorrectly()
    {
        // Arrange & Act
        var update = TestKitAutoFixture.CreateMessageUpdate();

        // Assert
        Assert.That(update, Is.Not.Null);
        Assert.That(update.Message, Is.Not.Null);
        Assert.That(update.Message.Text, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void AutoFixture_CreateCallbackQueryUpdate_WorksCorrectly()
    {
        // Arrange & Act
        var update = TestKitAutoFixture.CreateCallbackQueryUpdate();

        // Assert
        Assert.That(update, Is.Not.Null);
        Assert.That(update.CallbackQuery, Is.Not.Null);
        Assert.That(update.CallbackQuery.Data, Is.Not.Null);
    }

    [Test]
    public void AutoFixture_CreateManyMessages_WorksCorrectly()
    {
        // Arrange & Act
        var messages = TestKitAutoFixture.CreateManyMessages(5);

        // Assert
        Assert.That(messages, Is.Not.Null);
        Assert.That(messages.Count(), Is.EqualTo(5));
        Assert.That(messages, Has.All.Not.Null);
        Assert.That(messages, Has.All.Not.Null);
        Assert.That(messages.All(m => m.Text != null && m.Text.Length > 0), Is.True);
    }

    [Test]
    public void AutoFixture_CreateManyUsers_WorksCorrectly()
    {
        // Arrange & Act
        var users = TestKitAutoFixture.CreateManyUsers(3);

        // Assert
        Assert.That(users, Is.Not.Null);
        Assert.That(users.Count(), Is.EqualTo(3));
        Assert.That(users, Has.All.Not.Null);
        Assert.That(users, Has.All.Not.Null);
        Assert.That(users.All(u => u.FirstName != null && u.FirstName.Length > 0), Is.True);
    }

    [Test]
    public void AutoFixture_CreateManySpamMessages_WorksCorrectly()
    {
        // Arrange & Act
        var spamMessages = TestKitAutoFixture.CreateManySpamMessages(4);

        // Assert
        Assert.That(spamMessages, Is.Not.Null);
        Assert.That(spamMessages.Count(), Is.EqualTo(4));
        Assert.That(spamMessages, Has.All.Not.Null);
        Assert.That(spamMessages, Has.All.Not.Null);
        Assert.That(spamMessages.All(m => m.Text != null && m.Text.Length > 0), Is.True);
        
        // Проверяем, что это действительно спам
        foreach (var message in spamMessages)
        {
            Assert.That(message.Text, Does.Contain("🔥").Or.Contain("💰").Or.Contain("🎁").Or.Contain("⚡").Or.Contain("💎").Or.Contain("🚀").Or.Contain("📱"));
        }
    }

    [Test]
    public void AutoFixture_CreateWithFixture_WorksCorrectly()
    {
        // Arrange & Act
        var (handler, fixture) = TestKitAutoFixture.CreateWithFixture<MessageHandler>();

        // Assert
        Assert.That(handler, Is.Not.Null);
        Assert.That(fixture, Is.Not.Null);
        Assert.That(handler, Is.InstanceOf<MessageHandler>());
    }

    [Test]
    public void AutoFixture_CreateGeneric_WorksCorrectly()
    {
        // Arrange & Act
        var user = TestKitAutoFixture.Create<Telegram.Bot.Types.User>();
        var chat = TestKitAutoFixture.Create<Telegram.Bot.Types.Chat>();
        var message = TestKitAutoFixture.Create<Telegram.Bot.Types.Message>();

        // Assert
        Assert.That(user, Is.Not.Null);
        Assert.That(chat, Is.Not.Null);
        Assert.That(message, Is.Not.Null);
        
        Assert.That(user.FirstName, Is.Not.Null.And.Not.Empty);
        Assert.That(chat.Title, Is.Not.Null.And.Not.Empty);
        Assert.That(message.Text, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void AutoFixture_CreateManyGeneric_WorksCorrectly()
    {
        // Arrange & Act
        var users = TestKitAutoFixture.CreateMany<Telegram.Bot.Types.User>(3);
        var chats = TestKitAutoFixture.CreateMany<Telegram.Bot.Types.Chat>(2);

        // Assert
        Assert.That(users.Count(), Is.EqualTo(3));
        Assert.That(chats.Count(), Is.EqualTo(2));
        
        Assert.That(users, Has.All.Not.Null);
        Assert.That(chats, Has.All.Not.Null);
    }

    [Test]
    public void AutoFixture_IntegrationWithBogus_WorksCorrectly()
    {
        // Arrange & Act
        var handler = TestKitAutoFixture.CreateMessageHandler();
        var update = TestKitAutoFixture.CreateMessageUpdate();

        // Assert
        Assert.That(handler, Is.Not.Null);
        Assert.That(update, Is.Not.Null);
        Assert.That(update.Message, Is.Not.Null);
        
        // Проверяем, что AutoFixture использует Bogus для генерации данных
        Assert.That(update.Message.From, Is.Not.Null);
        Assert.That(update.Message.From.FirstName, Is.Not.Null.And.Not.Empty);
        Assert.That(update.Message.Chat, Is.Not.Null);
        Assert.That(update.Message.Chat.Title, Is.Not.Null.And.Not.Empty);
    }
} 
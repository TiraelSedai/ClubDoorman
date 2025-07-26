using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Services
{
    [TestFixture]
    public class MessageTemplatesHtmlTests
    {
        private MessageTemplates _templates;

        [SetUp]
        public void Setup()
        {
            _templates = new MessageTemplates();
        }

        [Test]
        public void ModerationWarning_ShouldContainHtmlTags()
        {
            // Arrange
            var user = new User { Id = 12345, FirstName = "Test", LastName = "User" };
            var chat = new Chat { Id = -100123456789, Title = "Test Chat" };
            var data = new SimpleNotificationData(user, chat, "test reason");

            // Act
            var template = _templates.GetUserTemplate(UserNotificationType.ModerationWarning);
            var result = _templates.FormatNotificationTemplate(template, data);

            // Assert
            Assert.That(result, Contains.Substring("<b>новичок</b>"));
            Assert.That(result, Contains.Substring("<b>Первые 3 сообщения</b>"));
            Assert.That(result, Contains.Substring("<b>стоп-слова</b>"));
            Assert.That(result, Contains.Substring("<a href=\"tg://user?id=12345\">Test User</a>"));
            
            // Проверяем, что нет лишних слешей
            Assert.That(result, Does.Not.Contain("\\."));
            Assert.That(result, Does.Not.Contain("\\-"));
            
            // Проверяем, что нет Markdown синтаксиса
            Assert.That(result, Does.Not.Contain("*новичок*"));
            Assert.That(result, Does.Not.Contain("*Первые 3 сообщения*"));
        }

        [Test]
        public void UserMention_ShouldBeHtmlLink()
        {
            // Arrange
            var user = new User { Id = 12345, FirstName = "Test", LastName = "User" };
            var chat = new Chat { Id = -100123456789, Title = "Test Chat" };
            var data = new SimpleNotificationData(user, chat, "test reason");

            // Act
            var template = "Test message with {UserMention}";
            var result = _templates.FormatNotificationTemplate(template, data);

            // Assert
            Assert.That(result, Is.EqualTo("Test message with <a href=\"tg://user?id=12345\">Test User</a>"));
        }

        [Test]
        public void SuspiciousUserTemplate_ShouldUseHtmlCodeTags()
        {
            // Arrange
            var user = new User { Id = 12345, FirstName = "Test", LastName = "User" };
            var chat = new Chat { Id = -100123456789, Title = "Test Chat" };
            var messages = new List<string> { "Test message 1", "Test message 2" };
            var data = new SuspiciousUserNotificationData(user, chat, 0.5, messages, DateTime.UtcNow);

            // Act
            var template = _templates.GetAdminTemplate(AdminNotificationType.SuspiciousUser);
            var result = _templates.FormatNotificationTemplate(template, data);

            // Assert
            Assert.That(result, Contains.Substring("<code>Test message 1</code>"));
            Assert.That(result, Does.Not.Contain("`Test message 1`"));
        }
    }
} 
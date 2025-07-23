using ClubDoorman.Handlers;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using NUnit.Framework;
using Moq;
using System;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Handlers;

[TestFixture]
[Category("unit")]
[Category("handlers")]
[Category("ai-profile-analysis")]
public class MessageHandlerAiProfileAnalysisTests
{
    private MessageHandlerTestFactory _factory = null!;
    private MessageHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();
        
        // Настройка базовых моков
        _factory.WithModerationServiceSetup(mock =>
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new Models.ModerationResult(Models.ModerationAction.Allow, "Test"));
            mock.Setup(x => x.CheckUserNameAsync(It.IsAny<User>()))
                .ReturnsAsync(new Models.ModerationResult(Models.ModerationAction.Allow, "Test name"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });
        
        _factory.WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.Approved(It.IsAny<long>(), It.IsAny<long?>()))
                .Returns(false); // Пользователь НЕ одобрен - нужен AI анализ
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>()))
                .ReturnsAsync((string?)null);
            mock.Setup(x => x.InBanlist(It.IsAny<long>()))
                .ReturnsAsync(false);
        });
        
        _factory.WithCaptchaServiceSetup(mock =>
        {
            mock.Setup(x => x.GenerateKey(It.IsAny<long>(), It.IsAny<long>()))
                .Returns("test_key");
            mock.Setup(x => x.GetCaptchaInfo(It.IsAny<string>()))
                .Returns((Models.CaptchaInfo?)null);
        });
        
        _factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.DeleteMessage(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mock.Setup(x => x.RestrictChatMember(
                It.IsAny<long>(), 
                It.IsAny<long>(), 
                It.IsAny<ChatPermissions>(), 
                It.IsAny<DateTime?>(), 
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        });
        
        _factory.WithMessageServiceSetup(mock =>
        {
            mock.Setup(x => x.SendAiProfileAnalysisAsync(It.IsAny<AiProfileAnalysisData>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        });
        
        _handler = _factory.CreateMessageHandler();
    }

    [Test]
    [TestCase(0.5, "Низкая вероятность спама")]
    [TestCase(0.6, "Средне-низкая вероятность спама")]
    [TestCase(0.74, "Почти средняя вероятность спама")]
    public async Task PerformAiProfileAnalysis_LowProbability_NoActionsPerformed(double probability, string reason)
    {
        // Arrange
        var user = CreateTestUser();
        var chat = CreateTestChat();
        var message = CreateTestMessage("Тестовое сообщение", user, chat);
        
        _factory.WithAiChecksSetup(mock =>
        {
            mock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), null))
                .ReturnsAsync(new SpamPhotoBio(
                    new SpamProbability { Probability = (float)probability, Reason = reason },
                    new byte[0],
                    "Test User"
                ));
        });

        // Act
        await _handler.HandleAsync(CreateUpdate(message), CancellationToken.None);

        // Assert
        // Проверяем что никаких действий не было предпринято
        _factory.BotMock.Verify(x => x.DeleteMessage(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), 
            Times.Never, "Сообщение не должно удаляться при низкой вероятности");
            
        _factory.BotMock.Verify(x => x.RestrictChatMember(
            It.IsAny<long>(), It.IsAny<long>(), It.IsAny<ChatPermissions>(), 
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), 
            Times.Never, "Пользователь не должен ограничиваться при низкой вероятности");
            
        _factory.MessageServiceMock.Verify(x => x.SendAiProfileAnalysisAsync(It.IsAny<AiProfileAnalysisData>(), It.IsAny<CancellationToken>()), 
            Times.Never, "Уведомление не должно отправляться при низкой вероятности");
    }

    [Test]
    [TestCase(0.75, "Средняя вероятность спама")]
    [TestCase(0.8, "Высокая-средняя вероятность спама")]
    [TestCase(0.89, "Почти высокая вероятность спама")]
    public async Task PerformAiProfileAnalysis_MediumProbability_OnlyReadOnlyApplied(double probability, string reason)
    {
        // Arrange
        var user = CreateTestUser();
        var chat = CreateTestChat();
        var message = CreateTestMessage("Тестовое сообщение", user, chat);
        
        _factory.WithAiChecksSetup(mock =>
        {
            mock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), null))
                .ReturnsAsync(new SpamPhotoBio(
                    new SpamProbability { Probability = (float)probability, Reason = reason },
                    new byte[0],
                    "Test User"
                ));
        });

        // Act
        await _handler.HandleAsync(CreateUpdate(message), CancellationToken.None);

        // Assert
        // Проверяем что сообщение НЕ удалено
        _factory.BotMock.Verify(x => x.DeleteMessage(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), 
            Times.Never, "Сообщение НЕ должно удаляться при средней вероятности");
            
        // Проверяем что пользователь ограничен (read-only)
        _factory.BotMock.Verify(x => x.RestrictChatMember(
            It.Is<long>(chatId => chatId == chat.Id),
            It.Is<long>(userId => userId == user.Id),
            It.Is<ChatPermissions>(perms => perms.CanSendMessages == false),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), 
            Times.Once, "Пользователь должен быть ограничен при средней вероятности");
            
        // Проверяем что отправлено уведомление с правильным автоматическим действием
        _factory.MessageServiceMock.Verify(x => x.SendAiProfileAnalysisAsync(
            It.Is<AiProfileAnalysisData>(data => 
                data.SpamProbability == probability &&
                data.Reason == reason &&
                data.AutomaticAction == "🔇 Read-Only на 10 минут (сообщение оставлено)"),
            It.IsAny<CancellationToken>()), 
            Times.Once, "Должно быть отправлено уведомление с правильным описанием действия");
    }

    [Test]
    [TestCase(0.9, "Высокая вероятность спама")]
    [TestCase(0.95, "Очень высокая вероятность спама")]
    [TestCase(1.0, "Максимальная вероятность спама")]
    public async Task PerformAiProfileAnalysis_HighProbability_MessageDeletedAndReadOnlyApplied(double probability, string reason)
    {
        // Arrange
        var user = CreateTestUser();
        var chat = CreateTestChat();
        var message = CreateTestMessage("Спам сообщение", user, chat);
        
        _factory.WithAiChecksSetup(mock =>
        {
            mock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), null))
                .ReturnsAsync(new SpamPhotoBio(
                    new SpamProbability { Probability = (float)probability, Reason = reason },
                    new byte[100], // Имитируем фото профиля
                    "Spam User"
                ));
        });

        // Act
        await _handler.HandleAsync(CreateUpdate(message), CancellationToken.None);

        // Assert
        // Проверяем что сообщение удалено
        _factory.BotMock.Verify(x => x.DeleteMessage(
            It.Is<long>(chatId => chatId == chat.Id),
            It.Is<int>(msgId => msgId == message.MessageId),
            It.IsAny<CancellationToken>()), 
            Times.Once, "Сообщение должно быть удалено при высокой вероятности");
            
        // Проверяем что пользователь ограничен (read-only)
        _factory.BotMock.Verify(x => x.RestrictChatMember(
            It.Is<long>(chatId => chatId == chat.Id),
            It.Is<long>(userId => userId == user.Id),
            It.Is<ChatPermissions>(perms => perms.CanSendMessages == false),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), 
            Times.Once, "Пользователь должен быть ограничен при высокой вероятности");
            
        // Проверяем что отправлено уведомление с правильным автоматическим действием
        _factory.MessageServiceMock.Verify(x => x.SendAiProfileAnalysisAsync(
            It.Is<AiProfileAnalysisData>(data => 
                data.SpamProbability == probability &&
                data.Reason == reason &&
                data.AutomaticAction == "🗑️ Сообщение удалено + 🔇 Read-Only на 10 минут"),
            It.IsAny<CancellationToken>()), 
            Times.Once, "Должно быть отправлено уведомление с правильным описанием действия");
    }

    [Test]
    public async Task PerformAiProfileAnalysis_ExactThresholdValues_BehaviorCorrect()
    {
        // Arrange - тестируем точные пороговые значения
        var user = CreateTestUser();
        var chat = CreateTestChat();
        var message = CreateTestMessage("Тестовое сообщение", user, chat);

        // Test: точно 0.75 - должно ограничивать, но не удалять
        _factory.WithAiChecksSetup(mock =>
        {
            mock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), null))
                .ReturnsAsync(new SpamPhotoBio(
                    new SpamProbability { Probability = (float)Consts.LlmLowProbability, Reason = "Точно пороговое значение" },
                    new byte[0],
                    "Test User"
                ));
        });

        // Act
        await _handler.HandleAsync(CreateUpdate(message), CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(x => x.DeleteMessage(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), 
            Times.Never, "При точно LlmLowProbability сообщение НЕ должно удаляться");
        _factory.BotMock.Verify(x => x.RestrictChatMember(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<ChatPermissions>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), 
            Times.Once, "При точно LlmLowProbability должно применяться ограничение");

        // Reset для следующего теста
        _factory.BotMock.Reset();
        _factory.MessageServiceMock.Reset();

        // Test: точно 0.9 - должно удалять И ограничивать
        _factory.WithAiChecksSetup(mock =>
        {
            mock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), null))
                .ReturnsAsync(new SpamPhotoBio(
                    new SpamProbability { Probability = (float)Consts.LlmHighProbability, Reason = "Точно высокое пороговое значение" },
                    new byte[0],
                    "Test User"
                ));
        });

        // Act
        await _handler.HandleAsync(CreateUpdate(message), CancellationToken.None);

        // Assert
        _factory.BotMock.Verify(x => x.DeleteMessage(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), 
            Times.Once, "При точно LlmHighProbability сообщение должно удаляться");
        _factory.BotMock.Verify(x => x.RestrictChatMember(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<ChatPermissions>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), 
            Times.Once, "При точно LlmHighProbability должно применяться ограничение");
    }

    #region Helper Methods

    private static User CreateTestUser(long id = 12345)
    {
        return new User
        {
            Id = id,
            FirstName = "Test",
            LastName = "User",
            Username = "testuser"
        };
    }

    private static Chat CreateTestChat(long id = -100123456789)
    {
        return new Chat
        {
            Id = id,
            Type = ChatType.Group,
            Title = "Test Chat"
        };
    }

    private static Message CreateTestMessage(string text, User user, Chat chat)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = chat,
            From = user,
            Text = text
        };
    }

    private static Update CreateUpdate(Message message)
    {
        return new Update
        {
            Id = 1,
            Message = message
        };
    }

    #endregion
} 
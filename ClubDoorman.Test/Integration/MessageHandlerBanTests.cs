using ClubDoorman.Handlers;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.TestData;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Test.TestData;
using ClubDoorman.Services;
using ClubDoorman.Test.TestKit;

namespace ClubDoorman.Test.Integration;

[TestFixture]
[Category("integration")]
[Category("messagehandler")]
[Category("ban")]
public class MessageHandlerBanTests
{
    private MessageHandlerTestFactory _factory = null!;
    private FakeTelegramClient _fakeClient = null!;
    private MessageHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();
        _fakeClient = FakeTelegramClientFactory.Create();
        
        // Настройка базовых моков для всех тестов
        _factory.WithAppConfigSetup(mock => 
        {
            mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
            mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
            mock.Setup(x => x.AdminChatId).Returns(123456789);
            mock.Setup(x => x.LogAdminChatId).Returns(987654321);
        });
        
        _factory.WithBotPermissionsServiceSetup(mock =>
        {
            mock.Setup(x => x.IsSilentModeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        });
        
        _factory.WithCaptchaServiceSetup(mock =>
        {
            mock.Setup(x => x.GenerateKey(It.IsAny<long>(), It.IsAny<long>()))
                .Returns("test-key");
            mock.Setup(x => x.GetCaptchaInfo(It.IsAny<string>()))
                .Returns((CaptchaInfo?)null);
        });
        
        _factory.WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.InBanlist(It.IsAny<long>())).ReturnsAsync(false);
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>())).ReturnsAsync((string?)null);
        });
        
        _factory.WithModerationServiceSetup(mock => 
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid message"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });
    }

    [Test]
    public async Task BanUserForLongName_PrivateChat_LogsWarningAndSendsAdminNotification()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var userJoinMessage = TK.CreateNewUserJoinMessage(user.Id);
        userJoinMessage.Chat = chat;

        // Используем стандартную настройку из инфраструктуры
        factory.SetupLongNameBanTestScenario(user);
        
        // Создаем MessageHandler с реальным UserBanService после настройки всех моков
        var handler = factory.CreateMessageHandlerWithRealUserBanService();

        // Act
        var update = new Update { Message = userJoinMessage };
        await handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем legacy поведение (прямые вызовы бота)
        factory.BotMock.Verify(
            x => x.BanChatMember(
                It.IsAny<ChatId>(),
                It.IsAny<long>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        factory.BotMock.Verify(
            x => x.DeleteMessage(
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task BanUserForLongName_GroupChat_BansUserAndSendsNotification()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var userJoinMessage = TK.CreateNewUserJoinMessage(user.Id);
        userJoinMessage.Chat = chat;

        // Используем стандартную настройку из инфраструктуры
        factory.SetupLongNameBanTestScenario(user);
        
        // Создаем MessageHandler после настройки всех моков
        var handler = factory.CreateMessageHandlerWithRealUserBanService();

        // Act
        var update = new Update { Message = userJoinMessage };
        await handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем legacy поведение (прямые вызовы бота)
        factory.BotMock.Verify(
            x => x.BanChatMember(
                It.IsAny<ChatId>(),
                It.IsAny<long>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        factory.BotMock.Verify(
            x => x.DeleteMessage(
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task BanBlacklistedUser_WhenUserInBlacklist_BansUser()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var userJoinMessage = TK.CreateNewUserJoinMessage(user.Id);
        userJoinMessage.Chat = chat;

        // Используем стандартную настройку из инфраструктуры
        factory.SetupBlacklistUserTestScenario(user);
        
        // Создаем MessageHandler после настройки всех моков
        var handler = factory.CreateMessageHandlerWithRealUserBanService();

        // Act
        var update = new Update { Message = userJoinMessage };
        await handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем legacy поведение (прямые вызовы бота)
        factory.BotMock.Verify(
            x => x.BanChatMember(
                It.IsAny<ChatId>(),
                It.IsAny<long>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        factory.BotMock.Verify(
            x => x.DeleteMessage(
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AutoBan_WhenUserViolatesRules_BansUser()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.From = user;
        message.Chat = chat;

        var envelope = new MessageEnvelope(
            MessageId: 459,
            ChatId: chat.Id,
            UserId: user.Id,
            Text: "Spam message"
        );

        _fakeClient.RegisterMessageEnvelope(envelope);

        // Настройка моков для сценария автобана
        _factory.SetupAutoBanScenario(user, "Спам сообщение");
        
        // Создаем MessageHandler после настройки моков
        _handler = _factory.CreateMessageHandlerWithRealUserBanService();

        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем legacy поведение (прямые вызовы бота)
        _factory.BotMock.Verify(
            x => x.BanChatMember(
                It.IsAny<ChatId>(),
                It.IsAny<long>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        _factory.BotMock.Verify(
            x => x.DeleteMessage(
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AutoBanChannel_WhenChannelSendsMessage_BansChannel()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();
        var channel = TK.CreateChannel();
        // Создаем чат с уникальным ID, чтобы избежать конфликтов с настройками
        var chat = new Chat
        {
            Id = -1001999999999, // Уникальный ID, который точно не настроен как announcement
            Type = ChatType.Group,
            Title = "Test Group for Channel Ban",
            Username = "testgroupchannel"
        };
        var message = TK.CreateChannelMessage(channel.Id, chat.Id, "Channel message");
        message.SenderChat = channel;
        message.Chat = chat;

        // Используем стандартную настройку из инфраструктуры
        factory.SetupChannelTestScenario(chat);
        
        // Настраиваем UserBanService для вызова AutoBanChannel
        // ДОЛЖНО быть ПОСЛЕ SetupChannelTestScenario, чтобы не перезаписаться
        factory.UserBanServiceMock.Setup(x => x.AutoBanChannelAsync(
            It.IsAny<Message>(), 
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Config.ChannelAutoBan - статическое свойство, по умолчанию true
        // Создаем MessageHandler после настройки всех моков
        var handler = factory.CreateMessageHandlerWithRealUserBanService();

        // Act
        var update = new Update { Message = message };
        await handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем legacy поведение (прямые вызовы бота)
        factory.BotMock.Verify(
            x => x.BanChatSenderChat(
                It.IsAny<ChatId>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        factory.BotMock.Verify(
            x => x.DeleteMessage(
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task HandleBlacklistBan_WhenUserInBlacklist_BansUser()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.From = user;
        message.Chat = chat;

        var envelope = new MessageEnvelope(
            MessageId: 461,
            ChatId: chat.Id,
            UserId: user.Id,
            Text: "User message"
        );

        _fakeClient.RegisterMessageEnvelope(envelope);

        // Настройка моков для сценария обработки бана из блэклиста
        _factory.SetupBlacklistBanHandlingScenario(user, "Пользователь в блэклисте");
        
        // Создаем MessageHandler после настройки моков
        _handler = _factory.CreateMessageHandlerWithRealUserBanService();

        // Act
        var update = new Update { Message = message };
        await _handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем legacy поведение (прямые вызовы бота)
        _factory.BotMock.Verify(
            x => x.BanChatMember(
                It.IsAny<ChatId>(),
                It.IsAny<long>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        _factory.BotMock.Verify(
            x => x.DeleteMessage(
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category("ban")]
    [Category("moderation")]
    public async Task WhenModerationReturnsBan_ShouldCallAutoBanAsync()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateTextMessage(user.Id, chat.Id, "spam message");
        message.Chat = chat;

        // Используем новую настройку для сценария бана по модерации
        factory.SetupModerationBanScenario("Спам сообщение");
        
        // Создаем MessageHandler после настройки всех моков
        var handler = factory.CreateMessageHandlerWithRealUserBanService();

        // Act
        var update = new Update { Message = message };
        await handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем legacy поведение (прямые вызовы бота)
        factory.BotMock.Verify(
            x => x.BanChatMember(
                It.IsAny<ChatId>(),
                It.IsAny<long>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        factory.BotMock.Verify(
            x => x.DeleteMessage(
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category("ban")]
    [Category("moderation")]
    public async Task WhenModerationReturnsBan_ShouldLogUserBanned()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateTextMessage(user.Id, chat.Id, "spam message");
        message.Chat = chat;

        // Используем новую настройку для сценария бана по модерации
        factory.SetupModerationBanScenario("Спам сообщение");
        
        // Создаем MessageHandler после настройки всех моков
        var handler = factory.CreateMessageHandlerWithRealUserBanService();

        // Act
        var update = new Update { Message = message };
        await handler.HandleAsync(update, CancellationToken.None);

        // Assert
        factory.UserFlowLoggerMock.Verify(
            x => x.LogUserBanned(It.IsAny<User>(), It.IsAny<Chat>(), "Спам сообщение"),
            Times.Once);
    }

    [Test]
    [Category("ban")]
    [Category("ai")]
    [Ignore("AI/ML functionality not available in upstream momai version")]
    public async Task WhenAiConfirmsMlSuspicion_ShouldCallAutoBanAsync()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateTextMessage(user.Id, chat.Id, "suspicious message");
        message.Chat = chat;

        // Используем настройку для AI подтверждения ML подозрения
        factory.SetupAiMlBanScenario(probability: 0.85, reason: "ML подозрение подтверждено");
        
        // Создаем MessageHandler после настройки всех моков
        var handler = factory.CreateMessageHandlerWithRealUserBanService();

        // Act
        var update = new Update { Message = message };
        await handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем legacy поведение (прямые вызовы бота)
        factory.BotMock.Verify(
            x => x.BanChatMember(
                It.IsAny<ChatId>(),
                It.IsAny<long>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        factory.BotMock.Verify(
            x => x.DeleteMessage(
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category("ban")]
    [Category("ai")]
    public async Task WhenAiRejectsMlSuspicion_ShouldNotCallAutoBanAsync()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateTextMessage(user.Id, chat.Id, "suspicious message");
        message.Chat = chat;

        // Используем настройку для AI отклонения ML подозрения
        factory.SetupAiMlRejectScenario(probability: 0.25, reason: "ML подозрение отклонено");
        
        // Создаем MessageHandler после настройки всех моков
        var handler = factory.CreateMessageHandlerWithRealUserBanService();

        // Act
        var update = new Update { Message = message };
        await handler.HandleAsync(update, CancellationToken.None);

        // Assert
        factory.UserBanServiceMock.Verify(
            x => x.AutoBanAsync(It.IsAny<Message>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    [Category("ban")]
    [Category("violations")]
    public async Task WhenRepeatedViolations_ShouldCallAutoBanAsync()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.From = user;
        message.Chat = chat;

        // Используем настройку для повторных нарушений
        factory.SetupRepeatedViolationsBanScenario("TextMention");
        
        // Настраиваем модерацию для возврата Delete с причиной, которая приведёт к нарушению
        factory.WithModerationServiceSetup(mock =>
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Delete, "ML решил что это спам"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });
        
        // Создаем MessageHandler после настройки всех моков
        var handler = factory.CreateMessageHandlerWithRealUserBanService();

        // Act
        var update = new Update { Message = message };
        await handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем legacy поведение (прямые вызовы бота)
        factory.BotMock.Verify(
            x => x.BanChatMember(
                It.IsAny<ChatId>(),
                It.IsAny<long>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        factory.BotMock.Verify(
            x => x.DeleteMessage(
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Test]
    [Category("ban")]
    [Category("violations")]
    public async Task WhenFirstViolation_ShouldNotCallAutoBanAsync()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.From = user;
        message.Chat = chat;

        // Используем стандартную настройку, но ViolationTracker возвращает false (первое нарушение)
        factory.SetupStandardBanTestScenario();
        factory.WithViolationTrackerSetup(mock =>
        {
            mock.Setup(x => x.RegisterViolation(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<ViolationType>()))
                .Returns(false); // Первое нарушение - бан не нужен
        });
        
        // Создаем MessageHandler после настройки всех моков
        var handler = factory.CreateMessageHandlerWithRealUserBanService();

        // Act
        var update = new Update { Message = message };
        await handler.HandleAsync(update, CancellationToken.None);

        // Assert
        factory.UserBanServiceMock.Verify(
            x => x.AutoBanAsync(It.IsAny<Message>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    [Category("ban")]
    [Category("channel")]
    public async Task WhenChannelSendsMessage_ShouldCallAutoBanChannelAsync()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();
        var channel = TK.CreateChannel();
        var chat = TK.Specialized.Chats.ForChannelTest(); // Используем специализированный метод
        var message = TK.CreateValidMessage();
        message.SenderChat = channel; // Сообщение от канала
        message.Chat = chat;
        message.From = null; // У сообщений от каналов From должен быть null

        // Используем настройку для автобана каналов
        factory.SetupChannelAutoBanScenario();
        
        // Настраиваем Bot для корректной работы с каналами
        factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.GetChat(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Chat?)null);
        });
        
        // Создаем MessageHandler после настройки всех моков
        var handler = factory.CreateMessageHandlerWithRealUserBanService();

        // Act
        var update = new Update { Message = message };
        await handler.HandleAsync(update, CancellationToken.None);

        // Assert - проверяем legacy поведение (прямые вызовы бота)
        factory.BotMock.Verify(
            x => x.BanChatSenderChat(
                It.IsAny<ChatId>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        factory.BotMock.Verify(
            x => x.DeleteMessage(
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category("ban")]
    [Category("channel")]
    public async Task WhenChannelAutoBanDisabled_ShouldNotCallAutoBanChannelAsync()
    {
        // Arrange
        var factory = new MessageHandlerTestFactory();
        var channel = TK.CreateChannel();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.SenderChat = channel; // Сообщение от канала
        message.Chat = chat;
        message.From = null; // У сообщений от каналов From должен быть null

        // Используем стандартную настройку, но отключаем автобан каналов
        factory.SetupStandardBanTestScenario();
        factory.WithAppConfigSetup(mock =>
        {
            // ChannelAutoBan отсутствует в эталонной версии momai
            // mock.Setup(x => x.ChannelAutoBan).Returns(false);
        });
        
        // Создаем MessageHandler после настройки всех моков
        var handler = factory.CreateMessageHandlerWithRealUserBanService();

        // Act
        var update = new Update { Message = message };
        await handler.HandleAsync(update, CancellationToken.None);

        // Assert
        factory.UserBanServiceMock.Verify(
            x => x.AutoBanChannelAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
} 
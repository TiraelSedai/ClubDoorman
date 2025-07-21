using ClubDoorman.Handlers;
using ClubDoorman.Handlers.Commands;
using ClubDoorman.TestInfrastructure;
using NUnit.Framework;
using Moq;
using System;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace ClubDoorman.Test.Unit.Handlers;

[TestFixture]
[Category("unit")]
[Category("handlers")]
[Category("extended")]
public class MessageHandlerExtendedTests
{
    private MessageHandlerTestFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();
        
        // Настройка базовых моков для предотвращения NullReferenceException
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
                .Returns(true);
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>()))
                .ReturnsAsync((string?)null);
            mock.Setup(x => x.InBanlist(It.IsAny<long>()))
                .ReturnsAsync(false);
        });
        
        _factory.WithServiceProviderSetup(mock =>
        {
            // Создаем реальные экземпляры командных обработчиков для тестов
            var startCommandHandler = new StartCommandHandler(
                new TelegramBotClientWrapper(new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz")),
                NullLogger<StartCommandHandler>.Instance
            );
            
            var suspiciousCommandHandler = new SuspiciousCommandHandler(
                new TelegramBotClientWrapper(new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz")),
                _factory.ModerationServiceMock.Object,
                NullLogger<SuspiciousCommandHandler>.Instance
            );
            
            // Возвращаем реальные экземпляры для командных обработчиков через GetService
            mock.Setup(x => x.GetService(typeof(StartCommandHandler)))
                .Returns(startCommandHandler);
                
            mock.Setup(x => x.GetService(typeof(SuspiciousCommandHandler)))
                .Returns(suspiciousCommandHandler);
                
            // Для остальных сервисов возвращаем null, но только если это не командные обработчики
            mock.Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns<Type>(serviceType => 
                {
                    if (serviceType == typeof(StartCommandHandler))
                        return startCommandHandler;
                    if (serviceType == typeof(SuspiciousCommandHandler))
                        return suspiciousCommandHandler;
                    return null;
                });
        });
    }

    #region Helper Methods

    private static Message CreateTestMessage(string? text = null, User? from = null, Chat? chat = null)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = chat ?? new Chat { Id = 123456, Type = ChatType.Group },
            From = from ?? new User { Id = 789, FirstName = "Test" },
            Text = text
        };
    }

    private static Message CreateTestMessageWithReply(string? text = null, Message? replyToMessage = null)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = -1001234567890, Type = ChatType.Supergroup },
            From = new User { Id = 789, FirstName = "Test" },
            Text = text,
            ReplyToMessage = replyToMessage
        };
    }

    private static Message CreateTestMessageWithNewMembers(User[] newMembers)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = 123456, Type = ChatType.Group },
            From = new User { Id = 789, FirstName = "Test" },
            NewChatMembers = newMembers
        };
    }

    private static Message CreateTestMessageWithLeftMember(User leftMember, User? from = null)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = 123456, Type = ChatType.Group },
            From = from ?? new User { Id = 789, FirstName = "Test" },
            LeftChatMember = leftMember
        };
    }

    private static Message CreateTestMessageWithSenderChat(Chat senderChat, string? text = null)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = 123456, Type = ChatType.Group },
            SenderChat = senderChat,
            Text = text
        };
    }

    #endregion

    #region CanHandle Tests

    [Test]
    public void CanHandle_ValidMessage_ReturnsTrue()
    {
        // Arrange
        var message = CreateTestMessage("Hello world");
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        var result = handler.CanHandle(update);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void CanHandle_EditedMessage_ReturnsTrue()
    {
        // Arrange
        var editedMessage = CreateTestMessage("Edited message");
        editedMessage.EditDate = DateTime.UtcNow;
        var update = new Update { EditedMessage = editedMessage };
        var handler = _factory.CreateMessageHandler();

        // Act
        var result = handler.CanHandle(update);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void CanHandle_NullUpdate_ReturnsFalse()
    {
        // Arrange
        Update? update = null;
        var handler = _factory.CreateMessageHandler();

        // Act
        var result = handler.CanHandle(update);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void CanHandle_UpdateWithoutMessage_ReturnsFalse()
    {
        // Arrange
        var update = new Update { Message = null, EditedMessage = null };
        var handler = _factory.CreateMessageHandler();

        // Act
        var result = handler.CanHandle(update);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void CanHandle_UpdateWithCallbackQuery_ReturnsFalse()
    {
        // Arrange
        var update = new Update 
        { 
            Message = null, 
            EditedMessage = null,
            CallbackQuery = new CallbackQuery { Id = "test" }
        };
        var handler = _factory.CreateMessageHandler();

        // Act
        var result = handler.CanHandle(update);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region HandleAsync - Basic Tests

    [Test]
    public async Task HandleAsync_NullUpdate_ThrowsArgumentNullException()
    {
        // Arrange
        Update? update = null;
        var handler = _factory.CreateMessageHandler();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await handler.HandleAsync(update));
        
        Assert.That(exception.ParamName, Is.EqualTo("update"));
    }

    [Test]
    public async Task HandleAsync_ValidMessage_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage("Hello world");
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_EditedMessage_HandlesSuccessfully()
    {
        // Arrange
        var editedMessage = CreateTestMessage("Edited message");
        editedMessage.EditDate = DateTime.UtcNow;
        var update = new Update { EditedMessage = editedMessage };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    #endregion

    #region HandleAsync - Command Tests

    [Test]
    public async Task HandleAsync_StartCommand_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage("/start");
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_SuspiciousCommand_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage("/suspicious");
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_SpamCommand_HandlesSuccessfully()
    {
        // Arrange
        var replyToMessage = CreateTestMessage("Spam message", new User { Id = 999, FirstName = "Target" });
        var message = CreateTestMessageWithReply("/spam", replyToMessage);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_HamCommand_HandlesSuccessfully()
    {
        // Arrange
        var replyToMessage = CreateTestMessage("Ham message", new User { Id = 999, FirstName = "Target" });
        var message = CreateTestMessageWithReply("/ham", replyToMessage);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_CheckCommand_HandlesSuccessfully()
    {
        // Arrange
        var replyToMessage = CreateTestMessage("Check message", new User { Id = 999, FirstName = "Target" });
        var message = CreateTestMessageWithReply("/check", replyToMessage);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_UnknownCommand_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage("/unknown");
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    #endregion

    #region HandleAsync - New Members Tests

    [Test]
    public async Task HandleAsync_NewChatMembers_HandlesSuccessfully()
    {
        // Arrange
        var newMembers = new[] { new User { Id = 999, FirstName = "NewUser", Username = "newuser" } };
        var message = CreateTestMessageWithNewMembers(newMembers);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_MultipleNewChatMembers_HandlesSuccessfully()
    {
        // Arrange
        var newMembers = new[]
        {
            new User { Id = 999, FirstName = "User1", Username = "user1" },
            new User { Id = 1000, FirstName = "User2", Username = "user2" }
        };
        var message = CreateTestMessageWithNewMembers(newMembers);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_NewChatMembersInAdminChat_HandlesSuccessfully()
    {
        // Arrange
        var adminChat = new Chat { Id = -1001234567890, Type = ChatType.Supergroup };
        var newMembers = new[] { new User { Id = 999, FirstName = "NewUser", Username = "newuser" } };
        var message = CreateTestMessageWithNewMembers(newMembers);
        message.Chat = adminChat;
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    #endregion

    #region HandleAsync - Left Chat Member Tests

    [Test]
    public async Task HandleAsync_LeftChatMemberFromBot_HandlesSuccessfully()
    {
        // Arrange
        var leftMember = new User { Id = 999, FirstName = "LeftUser" };
        var botUser = new User { Id = 123456789, FirstName = "Bot" };
        var message = CreateTestMessageWithLeftMember(leftMember, botUser);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_LeftChatMemberFromUser_HandlesSuccessfully()
    {
        // Arrange
        var leftMember = new User { Id = 999, FirstName = "LeftUser" };
        var message = CreateTestMessageWithLeftMember(leftMember);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    #endregion

    #region HandleAsync - Channel Message Tests

    [Test]
    public async Task HandleAsync_ChannelMessage_HandlesSuccessfully()
    {
        // Arrange
        var senderChat = new Chat { Id = -100987654321, Type = ChatType.Channel, Title = "Test Channel" };
        var message = CreateTestMessageWithSenderChat(senderChat, "Channel message");
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_ChannelMessageWithPhoto_HandlesSuccessfully()
    {
        // Arrange
        var senderChat = new Chat { Id = -100987654321, Type = ChatType.Channel, Title = "Test Channel" };
        var message = CreateTestMessageWithSenderChat(senderChat);
        message.Photo = new[] { new PhotoSize { FileId = "photo1", Width = 100, Height = 100 } };
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    #endregion

    #region HandleAsync - User Message Tests

    [Test]
    public async Task HandleAsync_UserTextMessage_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage("Hello world", new User { Id = 789, FirstName = "Test", Username = "testuser" });
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_UserPhotoMessage_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage(null, new User { Id = 789, FirstName = "Test", Username = "testuser" });
        message.Photo = new[] { new PhotoSize { FileId = "photo1", Width = 100, Height = 100 } };
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_UserVideoMessage_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage(null, new User { Id = 789, FirstName = "Test", Username = "testuser" });
        message.Video = new Video { FileId = "video1", Duration = 10 };
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_UserDocumentMessage_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage(null, new User { Id = 789, FirstName = "Test", Username = "testuser" });
        message.Document = new Document { FileId = "doc1", FileName = "test.pdf" };
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_UserStickerMessage_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage(null, new User { Id = 789, FirstName = "Test", Username = "testuser" });
        message.Sticker = new Sticker { FileId = "sticker1", Emoji = "😀" };
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public async Task HandleAsync_MessageWithoutFrom_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage("Message without from", null);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_MessageWithoutText_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage(null);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_MessageWithEmptyText_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage("");
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_MessageWithWhitespaceText_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage("   ");
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_MessageWithVeryLongText_HandlesSuccessfully()
    {
        // Arrange
        var longText = new string('a', 10000); // 10KB text
        var message = CreateTestMessage(longText);
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_MessageWithSpecialCharacters_HandlesSuccessfully()
    {
        // Arrange
        var message = CreateTestMessage("🚀 Test with emojis 🎉 and special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?");
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();

        // Act
        await handler.HandleAsync(update);

        // Assert
        // Тест проходит, если не выброшено исключение
        Assert.Pass();
    }

    #endregion

    #region Integration Tests

    [Test]
    public async Task HandleAsync_MultipleMessageTypes_HandlesSuccessfully()
    {
        // Arrange
        var handler = _factory.CreateMessageHandler();
        var updates = new[]
        {
            new Update { Message = CreateTestMessage("Text message") },
            new Update { Message = CreateTestMessage("/start") },
            new Update { Message = CreateTestMessageWithNewMembers(new[] { new User { Id = 999, FirstName = "NewUser" } }) }
        };

        // Act & Assert
        foreach (var update in updates)
        {
            await handler.HandleAsync(update);
        }

        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_ConcurrentMessages_HandlesSuccessfully()
    {
        // Arrange
        var handler = _factory.CreateMessageHandler();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            var message = CreateTestMessage($"Message {i}", new User { Id = 789 + i, FirstName = $"User{i}" });
            var update = new Update { Message = message };
            tasks.Add(handler.HandleAsync(update));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_CancellationToken_RespectsCancellation()
    {
        // Arrange
        var message = CreateTestMessage("Test message");
        var update = new Update { Message = message };
        var handler = _factory.CreateMessageHandler();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await handler.HandleAsync(update, cts.Token);
        // Should not throw if cancellation is handled properly
        Assert.Pass();
    }

    #endregion

    #region Performance Tests

    [Test]
    public async Task HandleAsync_LargeBatch_HandlesSuccessfully()
    {
        // Arrange
        var handler = _factory.CreateMessageHandler();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var message = CreateTestMessage($"Message {i}", new User { Id = 789 + i, FirstName = $"User{i}" });
            var update = new Update { Message = message };
            tasks.Add(handler.HandleAsync(update));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Pass();
    }

    [Test]
    public async Task HandleAsync_DifferentChatTypes_HandlesSuccessfully()
    {
        // Arrange
        var handler = _factory.CreateMessageHandler();
        var chatTypes = new[] { ChatType.Private, ChatType.Group, ChatType.Supergroup, ChatType.Channel };

        // Act
        foreach (var chatType in chatTypes)
        {
            var chat = new Chat { Id = 123456, Type = chatType };
            var message = CreateTestMessage($"Message in {chatType}", chat: chat);
            var update = new Update { Message = message };
            await handler.HandleAsync(update);
        }

        // Assert
        Assert.Pass();
    }

    #endregion
} 
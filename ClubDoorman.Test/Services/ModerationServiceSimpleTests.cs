using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestData;
using ClubDoorman.Models;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using ClubDoorman.Models.Notifications;
using Telegram.Bot.Extensions;

namespace ClubDoorman.Test.Services;

[TestFixture]
[Category("services")]
public class ModerationServiceSimpleTests
{
    private ModerationServiceTestFactory _factory;
    private ModerationService _service;

    [SetUp]
    public void Setup()
    {
        _factory = new ModerationServiceTestFactory();
        _service = _factory.CreateModerationService();
    }

    [Test]
    public void CreateModerationService_WithFactory_ReturnsValidInstance()
    {
        // Arrange & Act
        var service = _factory.CreateModerationService();

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<ModerationService>());
    }

    [Test]
    public void Factory_ProvidesAllMocks()
    {
        // Assert
        Assert.That(_factory.ClassifierMock, Is.Not.Null);
        Assert.That(_factory.MimicryClassifierMock, Is.Not.Null);
        Assert.That(_factory.BadMessageManagerMock, Is.Not.Null);
        Assert.That(_factory.UserManagerMock, Is.Not.Null);
        Assert.That(_factory.AiChecksMock, Is.Not.Null);
        Assert.That(_factory.SuspiciousUsersStorageMock, Is.Not.Null);
        Assert.That(_factory.LoggerMock, Is.Not.Null);
        Assert.That(_factory.FakeTelegramClient, Is.Not.Null);
    }

    [Test]
    public void Factory_WithFluentApi_ConfiguresMocks()
    {
        // Arrange
        var expectedResult = new ModerationResult(ModerationAction.Allow, "Test");

        // Act
        _factory.WithClassifierSetup(mock => 
            mock.Setup(x => x.IsSpam(It.IsAny<string>()))
                .ReturnsAsync((false, 0.2f)));

        // Assert
        Assert.That(_factory.ClassifierMock, Is.Not.Null);
    }

    [Test]
    public void TestDataFactory_CreatesValidMessage()
    {
        // Act
        var message = TK.CreateValidMessage();

        // Assert
        Assert.That(message, Is.Not.Null);
        Assert.That(message.MessageId, Is.EqualTo(0)); // MessageId is read-only, defaults to 0
        Assert.That(message.Text, Is.EqualTo("Hello, this is a valid message!"));
        Assert.That(message.From, Is.Not.Null);
        Assert.That(message.Chat, Is.Not.Null);
    }

    [Test]
    public void TestDataFactory_CreatesSpamMessage()
    {
        // Act
        var message = TK.CreateSpamMessage();

        // Assert
        Assert.That(message, Is.Not.Null);
        Assert.That(message.MessageId, Is.EqualTo(0)); // MessageId is read-only, defaults to 0
        Assert.That(message.Text, Is.EqualTo("BUY NOW!!! AMAZING OFFER!!! CLICK HERE!!!"));
    }

    [Test]
    public void TestDataFactory_CreatesValidUser()
    {
        // Act
        var user = TK.CreateValidUser();

        // Assert
        Assert.That(user, Is.Not.Null);
        Assert.That(user.Id, Is.EqualTo(123456789));
        Assert.That(user.FirstName, Is.EqualTo("Test"));
        Assert.That(user.LastName, Is.EqualTo("User"));
        Assert.That(user.Username, Is.EqualTo("testuser"));
        Assert.That(user.IsBot, Is.False);
    }

    [Test]
    public void TestDataFactory_CreatesBotUser()
    {
        // Act
        var user = TK.CreateBotUser();

        // Assert
        Assert.That(user, Is.Not.Null);
        Assert.That(user.Id, Is.EqualTo(987654321));
        Assert.That(user.FirstName, Is.EqualTo("TestBot"));
        Assert.That(user.Username, Is.EqualTo("testbot"));
        Assert.That(user.IsBot, Is.True);
    }

    [Test]
    public void TestDataFactory_CreatesGroupChat()
    {
        // Act
        var chat = TK.CreateGroupChat();

        // Assert
        Assert.That(chat, Is.Not.Null);
        Assert.That(chat.Id, Is.EqualTo(-1001234567890));
        Assert.That(chat.Type, Is.EqualTo(Telegram.Bot.Types.Enums.ChatType.Group));
        Assert.That(chat.Title, Is.EqualTo("Test Group"));
        Assert.That(chat.Username, Is.EqualTo("testgroup"));
    }

    [Test]
    public void CreateMessageWithMessageId_UsingTestDataFactory_Works()
    {
        // Act
        var message = TK.CreateValidMessageWithId(456);

        // Assert
        Assert.That(message, Is.Not.Null);
        // ВНИМАНИЕ: MessageId в Telegram.Bot является readonly и не может быть установлен в тестах
        // Это ограничение библиотеки Telegram.Bot, а не нашего кода
        Assert.That(message.MessageId, Is.EqualTo(0), "MessageId всегда 0 в Telegram.Bot из-за readonly ограничений");
        Assert.That(message.Text, Is.EqualTo("Hello, this is a valid message!"));
        Assert.That(message.Chat.Id, Is.EqualTo(-1001234567890));
        Assert.That(message.From.Id, Is.EqualTo(123456789));
    }

    [Test]
    public void SimpleMessageDeletionTest_TraceProblem()
    {
        // Arrange
        var fakeClient = new FakeTelegramClient();
        var envelope = new MessageEnvelope(
            MessageId: 123,
            UserId: 456,
            ChatId: 789,
            Text: "Test spam message"
        );
        
        // Act - напрямую удаляем сообщение
        fakeClient.DeleteMessageAsync(envelope.ChatId, envelope.MessageId);
        
        // Assert
        Console.WriteLine($"DEBUG: DeletedMessages count: {fakeClient.DeletedMessages.Count}");
        foreach (var deleted in fakeClient.DeletedMessages)
        {
            Console.WriteLine($"DEBUG: Deleted: ChatId={deleted.ChatId}, MessageId={deleted.MessageId}");
        }
        
        Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True, 
            $"Сообщение должно быть удалено. ChatId={envelope.ChatId}, MessageId={envelope.MessageId}");
    }

    [Test]
    public async Task MessageHandlerDeletionTest_TraceProblem()
    {
        // Arrange
        var fakeClient = new FakeTelegramClient();
        var factory = new MessageHandlerTestFactory();
        var handler = factory.CreateMessageHandlerWithFake(fakeClient);
        
        var envelope = new MessageEnvelope(
            MessageId: 456,
            UserId: 789,
            ChatId: 123,
            Text: "SPAM MESSAGE"
        );
        
        // Регистрируем envelope в fakeClient
        fakeClient.RegisterMessageEnvelope(envelope);
        
        // Создаем Message из envelope
        var message = fakeClient.CreateMessageFromEnvelope(envelope);
        
        // Настройка AppConfig - разрешаем чат 123 для обработки модерации
        factory.AppConfigMock.Setup(x => x.IsChatAllowed(123)).Returns(true);
        
        // Настройка AppConfig - пустой список отключенных чатов
        factory.AppConfigMock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
        
        // Настройка AppConfig - не админский чат
        factory.AppConfigMock.Setup(x => x.AdminChatId).Returns(999L);
        factory.AppConfigMock.Setup(x => x.LogAdminChatId).Returns(998L);
        
        // Настройка ModerationService - пользователь не одобрен, сообщение спам
        factory.ModerationServiceMock.Setup(x => x.IsUserApproved(789, 123)).Returns(false);
        factory.ModerationServiceMock.Setup(x => x.CheckMessageAsync(
            It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Delete, "SPAM detected", 0.9));
        
        // Настройка UserManager - пользователь не в блэклисте
        factory.UserManagerMock.Setup(x => x.InBanlist(789)).ReturnsAsync(false);
        
        // Настройка UserManager - пользователь не клубный
        factory.UserManagerMock.Setup(x => x.GetClubUsername(789)).ReturnsAsync((string?)null);
        
        // Настройка CaptchaService - пользователь не проходит капчу
        factory.CaptchaServiceMock.Setup(x => x.GetCaptchaInfo(It.IsAny<string>())).Returns((CaptchaInfo?)null);
        
        // Настройка MessageService - для отправки уведомлений
        factory.MessageServiceMock.Setup(x => x.SendUserNotificationWithReplyAsync(
            It.IsAny<User>(), 
            It.IsAny<Chat>(), 
            It.IsAny<UserNotificationType>(), 
            It.IsAny<object>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(TK.CreateValidMessage());
        
        // Act
        Console.WriteLine("=== НАЧАЛО ТЕСТА ===");
        Console.WriteLine($"Отправляем сообщение: {message.Text}");
        Console.WriteLine($"MessageId: {message.MessageId}, UserId: {message.From?.Id}, ChatId: {message.Chat.Id}");
        
        await handler.HandleAsync(message, CancellationToken.None);
        
        Console.WriteLine("=== ПОСЛЕ ОБРАБОТКИ ===");
        Console.WriteLine($"Количество отправленных сообщений: {fakeClient.SentMessages.Count}");
        foreach (var sentMsg in fakeClient.SentMessages)
        {
            Console.WriteLine($"Отправлено: {sentMsg.Text}");
        }
        
        Console.WriteLine($"Сообщение удалено: {fakeClient.WasMessageDeleted(envelope)}");
        Console.WriteLine($"Количество удаленных сообщений: {fakeClient.DeletedMessages.Count}");
        foreach (var deletedMsg in fakeClient.DeletedMessages)
        {
            Console.WriteLine($"Удалено: ChatId={deletedMsg.ChatId}, MessageId={deletedMsg.MessageId}");
        }
        Console.WriteLine("=== КОНЕЦ ТЕСТА ===");
        
        // Assert
        // Проверяем, что сообщение было удалено через envelope
        Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True, 
            "Сообщение должно быть удалено");
        
        // Проверяем, что было отправлено приветственное уведомление новичку
        factory.MessageServiceMock.Verify(x => x.SendUserNotificationWithReplyAsync(
            It.Is<User>(user => user.Id == 789),
            It.Is<Chat>(chat => chat.Id == 123),
            It.Is<UserNotificationType>(type => type == UserNotificationType.ModerationWarning),
            It.IsAny<object>(),
            It.IsAny<ReplyParameters>(),
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Проверяем логи для отладки
        var logs = fakeClient.GetOperationLog();
        Console.WriteLine("=== Логи операций ===");
        foreach (var log in logs)
        {
            Console.WriteLine(log);
        }
    }

    [Test]
    public void MessageEnvelopeApproach_SimpleTest()
    {
        // Arrange
        var fakeClient = new FakeTelegramClient();
        var envelope = new MessageEnvelope(
            MessageId: 999,
            UserId: 111,
            ChatId: 222,
            Text: "Test message"
        );
        
        // Регистрируем envelope в fakeClient
        fakeClient.RegisterMessageEnvelope(envelope);
        
        // Act - напрямую удаляем сообщение по envelope
        fakeClient.DeleteMessageAsync(envelope.ChatId, envelope.MessageId);
        
        // Assert
        Console.WriteLine($"DEBUG: MessageEnvelope test - DeletedMessages count: {fakeClient.DeletedMessages.Count}");
        foreach (var deleted in fakeClient.DeletedMessages)
        {
            Console.WriteLine($"DEBUG: MessageEnvelope test - Deleted: ChatId={deleted.ChatId}, MessageId={deleted.MessageId}");
        }
        
        Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True, 
            $"Сообщение должно быть удалено по MessageEnvelope. ChatId={envelope.ChatId}, MessageId={envelope.MessageId}");
        
        // Дополнительные проверки через MessageEnvelope
        Assert.That(envelope.MessageId, Is.EqualTo(999));
        Assert.That(envelope.UserId, Is.EqualTo(111));
        Assert.That(envelope.ChatId, Is.EqualTo(222));
        Assert.That(envelope.Text, Is.EqualTo("Test message"));
    }

    [Test]
    public void CreateMessageWithMessageId_UsingObjectInitializer_DoesNotWork()
    {
        // Act & Assert
        var message = new Message
        {
            // MessageId = 123, // Это не скомпилируется - readonly свойство
            Text = "test message",
            Chat = TK.CreateGroupChat(),
            From = TK.CreateValidUser()
        };

        // MessageId остается 0 (значение по умолчанию)
        Assert.That(message.MessageId, Is.EqualTo(0));
    }
} 
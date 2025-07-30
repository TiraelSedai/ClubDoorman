using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Test.TestData;
using ClubDoorman.TestInfrastructure;

namespace ClubDoorman.Test.TestKit;

/// <summary>
/// Улучшенная работа с Telegram объектами для тестов
/// Решает проблемы с MessageId и упрощает создание тестовых сценариев
/// <tags>telegram, message-id, scenarios, fake-client, test-infrastructure</tags>
/// </summary>
public static class TestKitTelegram
{
    private static int _nextMessageId = 1;
    
    /// <summary>
    /// Создает FakeTelegramClient с предустановленными настройками
    /// <tags>telegram, fake-client, test-infrastructure</tags>
    /// </summary>
    public static FakeTelegramClient CreateFakeClient()
    {
        return new FakeTelegramClient();
    }
    
    /// <summary>
    /// Создает MessageEnvelope с автоматическим MessageId
    /// <tags>telegram, message-envelope, message-id, test-infrastructure</tags>
    /// </summary>
    public static MessageEnvelope CreateEnvelope(
        long userId = 12345,
        long chatId = 67890,
        string text = "Test message",
        string? username = null,
        string? firstName = null,
        string? chatTitle = null)
    {
        return new MessageEnvelope(
            MessageId: _nextMessageId++,
            UserId: userId,
            ChatId: chatId,
            Text: text,
            Username: username ?? "testuser",
            FirstName: firstName ?? "Test",
            LastName: "User",
            IsBot: false,
            ChatTitle: chatTitle ?? "Test Chat",
            ChatUsername: "testchat",
            Date: DateTime.UtcNow
        );
    }
    
    /// <summary>
    /// Создает MessageEnvelope для спам-сообщений
    /// <tags>telegram, message-envelope, spam, message-id, test-infrastructure</tags>
    /// </summary>
    public static MessageEnvelope CreateSpamEnvelope(
        long userId = 12345,
        long chatId = 67890,
        int? messageId = null)
    {
        return new MessageEnvelope(
            MessageId: messageId ?? _nextMessageId++,
            UserId: userId,
            ChatId: chatId,
            Text: "🔥💰🎁 Make money fast! 💰🔥🎁",
            Username: "spammer",
            FirstName: "Spam",
            LastName: "User",
            IsBot: false,
            ChatTitle: "Test Chat",
            ChatUsername: "testchat",
            Date: DateTime.UtcNow
        );
    }
    
    /// <summary>
    /// Создает MessageEnvelope для новых участников
    /// <tags>telegram, message-envelope, new-user, message-id, test-infrastructure</tags>
    /// </summary>
    public static MessageEnvelope CreateNewUserEnvelope(
        long userId = 12345,
        long chatId = 67890,
        int? messageId = null)
    {
        return new MessageEnvelope(
            MessageId: messageId ?? _nextMessageId++,
            UserId: userId,
            ChatId: chatId,
            Text: "", // Пустой текст для новых участников
            Username: "newuser",
            FirstName: "New",
            LastName: "User",
            IsBot: false,
            ChatTitle: "Test Chat",
            ChatUsername: "testchat",
            Date: DateTime.UtcNow
        );
    }
    
    /// <summary>
    /// Создает Message из MessageEnvelope через FakeTelegramClient
    /// <tags>telegram, message, message-envelope, fake-client, test-infrastructure</tags>
    /// </summary>
    public static Message CreateMessageFromEnvelope(FakeTelegramClient fakeClient, MessageEnvelope envelope)
    {
        fakeClient.RegisterMessageEnvelope(envelope);
        return fakeClient.CreateMessageFromEnvelope(envelope);
    }
    
    /// <summary>
    /// Создает Update с Message из MessageEnvelope
    /// </summary>
    public static Update CreateUpdateFromEnvelope(FakeTelegramClient fakeClient, MessageEnvelope envelope)
    {
        var message = CreateMessageFromEnvelope(fakeClient, envelope);
        return new Update { Message = message };
    }
    
    /// <summary>
    /// Создает CallbackQuery с автоматическим MessageId
    /// </summary>
    public static CallbackQuery CreateCallbackQuery(
        long userId = 12345,
        long chatId = 67890,
        string data = "test_callback",
        int? messageId = null)
    {
        var message = TestKitBogus.CreateRealisticMessage();
        // MessageId readonly, используем TestDataFactory для создания с нужным ID
        if (messageId.HasValue)
        {
            message = TK.CreateValidMessageWithId(messageId.Value);
        }
        
        return new CallbackQuery
        {
            Id = Guid.NewGuid().ToString(),
            From = TestKitBogus.CreateRealisticUser(userId),
            Message = message,
            ChatInstance = Guid.NewGuid().ToString(),
            Data = data
        };
    }
    
    /// <summary>
    /// Создает Update с CallbackQuery
    /// </summary>
    public static Update CreateCallbackQueryUpdate(
        long userId = 12345,
        long chatId = 67890,
        string data = "test_callback",
        int? messageId = null)
    {
        return new Update
        {
            CallbackQuery = CreateCallbackQuery(userId, chatId, data, messageId)
        };
    }
    
    /// <summary>
    /// Создает ChatMemberUpdated для тестов
    /// </summary>
    public static ChatMemberUpdated CreateChatMemberUpdated(
        long userId = 12345,
        long chatId = 67890,
        ChatMemberStatus oldStatus = ChatMemberStatus.Member,
        ChatMemberStatus newStatus = ChatMemberStatus.Administrator)
    {
        var user = TestKitBogus.CreateRealisticUser(userId);
        
        return new ChatMemberUpdated
        {
            Chat = TestKitBogus.CreateRealisticGroup(),
            From = user,
            Date = DateTime.UtcNow,
            OldChatMember = CreateChatMemberByStatus(user, oldStatus),
            NewChatMember = CreateChatMemberByStatus(user, newStatus)
        };
    }
    
    /// <summary>
    /// Создает Update с ChatMemberUpdated
    /// </summary>
    public static Update CreateChatMemberUpdate(
        long userId = 12345,
        long chatId = 67890,
        ChatMemberStatus oldStatus = ChatMemberStatus.Member,
        ChatMemberStatus newStatus = ChatMemberStatus.Administrator)
    {
        return new Update
        {
            ChatMember = CreateChatMemberUpdated(userId, chatId, oldStatus, newStatus)
        };
    }
    
    /// <summary>
    /// Создает полный тестовый сценарий с FakeTelegramClient
    /// </summary>
    public static (FakeTelegramClient fakeClient, MessageEnvelope envelope, Message message, Update update) CreateFullScenario(
        long userId = 12345,
        long chatId = 67890,
        string text = "Test message")
    {
        var fakeClient = CreateFakeClient();
        var envelope = CreateEnvelope(userId, chatId, text);
        var message = CreateMessageFromEnvelope(fakeClient, envelope);
        var update = new Update { Message = message };
        
        return (fakeClient, envelope, message, update);
    }
    
    /// <summary>
    /// Создает спам-сценарий с FakeTelegramClient
    /// </summary>
    public static (FakeTelegramClient fakeClient, MessageEnvelope envelope, Message message, Update update) CreateSpamScenario(
        long userId = 12345,
        long chatId = 67890)
    {
        var fakeClient = CreateFakeClient();
        var envelope = CreateSpamEnvelope(userId, chatId);
        var message = CreateMessageFromEnvelope(fakeClient, envelope);
        var update = new Update { Message = message };
        
        return (fakeClient, envelope, message, update);
    }
    
    /// <summary>
    /// Создает сценарий нового участника с FakeTelegramClient
    /// </summary>
    public static (FakeTelegramClient fakeClient, MessageEnvelope envelope, Message message, Update update) CreateNewUserScenario(
        long userId = 12345,
        long chatId = 67890)
    {
        var fakeClient = CreateFakeClient();
        var envelope = CreateNewUserEnvelope(userId, chatId);
        var message = CreateMessageFromEnvelope(fakeClient, envelope);
        var update = new Update { Message = message };
        
        return (fakeClient, envelope, message, update);
    }
    
    /// <summary>
    /// Сбрасывает счетчик MessageId (для изоляции тестов)
    /// </summary>
    public static void ResetMessageIdCounter()
    {
        _nextMessageId = 1;
    }
    
    /// <summary>
    /// Устанавливает следующий MessageId (для предсказуемых тестов)
    /// </summary>
    public static void SetNextMessageId(int messageId)
    {
        _nextMessageId = messageId;
    }
    
    /// <summary>
    /// Создает ChatMember с нужным статусом
    /// </summary>
    private static ChatMember CreateChatMemberByStatus(User user, ChatMemberStatus status)
    {
        return status switch
        {
            ChatMemberStatus.Creator => new ChatMemberOwner { User = user },
            ChatMemberStatus.Administrator => new ChatMemberAdministrator { User = user },
            ChatMemberStatus.Member => new ChatMemberMember { User = user },
            ChatMemberStatus.Restricted => new ChatMemberRestricted { User = user },
            ChatMemberStatus.Left => new ChatMemberLeft { User = user },
            ChatMemberStatus.Kicked => new ChatMemberBanned { User = user },
            _ => new ChatMemberMember { User = user }
        };
    }
} 
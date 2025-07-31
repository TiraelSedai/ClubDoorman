using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Text;
using ClubDoorman.Services;
using ClubDoorman.Test.TestData;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// In-memory фейк для Telegram Bot API
/// Заменяет сложные моки и позволяет тестировать без реальных токенов
/// </summary>
[Obsolete("Use TestKitTelegram.CreateFakeClient() instead of new FakeTelegramClient()")]
public class FakeTelegramClient : ITelegramBotClientWrapper
{
    public FakeTelegramClient()
    {
        // Стандартный конструктор
    }

    public List<SentMessage> SentMessages { get; } = new();
    public List<DeletedMessage> DeletedMessages { get; } = new();
    public List<BannedUser> BannedUsers { get; } = new();
    public List<UnbannedUser> UnbannedUsers { get; } = new();
    public List<CallbackQuery> CallbackQueries { get; } = new();
    public List<AnsweredCallbackQuery> AnsweredCallbackQueries { get; } = new();
    public List<EditedMessage> EditedMessages { get; } = new();
    public List<SentPhoto> SentPhotos { get; } = new();
    public List<RestrictedUser> RestrictedUsers { get; } = new();
    
    // Для проверки порядка операций
    public List<string> OperationLog { get; } = new();
    
    public bool ShouldThrowException { get; set; } = false;
    public Exception? ExceptionToThrow { get; set; }
    public long BotId => 123456789;
    
    // Для интеграционных тестов
    private Dictionary<long, ChatFullInfo> _chatFullInfos = new();
    private Dictionary<string, string> _filePaths = new();
    private int _nextMessageId = 1;
    
    // MessageEnvelope для тестовых сценариев
    private Dictionary<int, MessageEnvelope> _messageEnvelopes = new();

    public Task<Message> SendMessageAsync(
        ChatId chatId,
        string text,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"DEBUG: FakeTelegramClient.SendMessageAsync called: chatId={chatId.Identifier}, text='{text}'");
        
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        // Используем TestDataFactory для создания Message с MessageId
        var message = TK.CreateValidMessageWithId(_nextMessageId++);
        // Переопределяем Chat, From и Text для соответствия параметрам
        message.Chat = new Chat { Id = chatId.Identifier ?? 0, Type = ChatType.Group };
        message.From = new User { Id = 123456789, IsBot = true, FirstName = "TestBot" };
        message.Text = text;

        SentMessages.Add(new SentMessage(
            chatId.Identifier ?? 0,
            text,
            parseMode,
            replyMarkup,
            message
        ));
        
        Console.WriteLine($"DEBUG: FakeTelegramClient.SendMessageAsync added message, SentMessages count: {SentMessages.Count}");
        OperationLog.Add($"SendMessageAsync: chatId={chatId.Identifier}, text={text}");

        return Task.FromResult(message);
    }

    public Task<bool> DeleteMessageAsync(
        ChatId chatId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"DEBUG: DeleteMessageAsync called: chatId={chatId.Identifier}, messageId={messageId}");
        
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        // Если messageId = 0 (что происходит с Telegram.Bot.Message), 
        // пытаемся найти соответствующий MessageEnvelope
        var actualMessageId = messageId;
        if (messageId == 0)
        {
            Console.WriteLine($"DEBUG: messageId is 0, searching for envelope in chat {chatId.Identifier}");
            Console.WriteLine($"DEBUG: Available envelopes: {string.Join(", ", _messageEnvelopes.Values.Select(e => $"ChatId={e.ChatId}, MessageId={e.MessageId}"))}");
            
            // Ищем MessageEnvelope по ChatId
            var envelope = _messageEnvelopes.Values.FirstOrDefault(e => e.ChatId == (chatId.Identifier ?? 0));
            if (envelope != null)
            {
                actualMessageId = envelope.MessageId;
                Console.WriteLine($"DEBUG: Found envelope with MessageId={envelope.MessageId}");
            }
            else
            {
                Console.WriteLine($"DEBUG: No envelope found for chat {chatId.Identifier}");
            }
        }

        Console.WriteLine($"DEBUG: Adding DeletedMessage: ChatId={chatId.Identifier}, MessageId={actualMessageId}");
        DeletedMessages.Add(new DeletedMessage(
            chatId.Identifier ?? 0,
            actualMessageId
        ));

        return Task.FromResult(true);
    }

    public Task<bool> BanChatMemberAsync(
        ChatId chatId,
        long userId,
        DateTime? untilDate = null,
        bool? revokeMessages = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        BannedUsers.Add(new BannedUser(
            chatId.Identifier ?? 0,
            userId,
            untilDate,
            revokeMessages ?? false
        ));

        return Task.FromResult(true);
    }

    public Task<bool> UnbanChatMemberAsync(
        ChatId chatId,
        long userId,
        bool? onlyIfBanned = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        UnbannedUsers.Add(new UnbannedUser(
            chatId.Identifier ?? 0,
            userId,
            onlyIfBanned ?? false
        ));

        return Task.FromResult(true);
    }

    public Task<User> GetMe(CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        return Task.FromResult(new User
        {
            Id = 123456789,
            IsBot = true,
            FirstName = "TestBot",
            Username = "test_bot"
        });
    }

    public Task DeleteMessage(ChatId chatId, int messageId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"DEBUG: DeleteMessage called: chatId={chatId.Identifier}, messageId={messageId}");
        return DeleteMessageAsync(chatId, messageId, cancellationToken);
    }

    public Task<Message> SendMessage(
        ChatId chatId,
        string text,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        return SendMessageAsync(chatId, text, parseMode, replyParameters, replyMarkup, cancellationToken);
    }

    public Task<Chat> GetChat(ChatId chatId, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        return Task.FromResult(new Chat
        {
            Id = chatId.Identifier ?? 0,
            Type = ChatType.Group,
            Title = "Test Chat"
        });
    }

    public Task<Message> ForwardMessage(
        ChatId chatId,
        ChatId fromChatId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        // Используем TestDataFactory для создания Message с MessageId
        var message = TK.CreateValidMessageWithId(_nextMessageId++);
        // Переопределяем Chat и From для соответствия параметрам
        message.Chat = new Chat { Id = chatId.Identifier ?? 0, Type = ChatType.Group };
        message.From = new User { Id = 123456789, IsBot = true, FirstName = "TestBot" };
        message.Text = "Forwarded message";

        return Task.FromResult(message);
    }

    public Task BanChatMember(
        ChatId chatId,
        long userId,
        DateTime? untilDate = null,
        bool revokeMessages = false,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        return Task.CompletedTask;
    }

    public Task BanChatSenderChat(ChatId chatId, long senderChatId, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        return Task.CompletedTask;
    }

    public Task RestrictChatMember(
        ChatId chatId,
        long userId,
        ChatPermissions permissions,
        DateTime? untilDate = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        RestrictedUsers.Add(new RestrictedUser(
            chatId.Identifier ?? 0,
            userId,
            permissions,
            untilDate
        ));
        
        OperationLog.Add($"RestrictChatMember: chatId={chatId.Identifier}, userId={userId}, untilDate={untilDate}");

        return Task.CompletedTask;
    }

    public Task<Message> SendPhoto(
        ChatId chatId,
        object photo,
        string? caption = null,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        // Используем TestDataFactory для создания Message с MessageId
        var message = TK.CreateValidMessageWithId(_nextMessageId++);
        // Переопределяем Chat и From для соответствия параметрам
        message.Chat = new Chat { Id = chatId.Identifier ?? 0, Type = ChatType.Group };
        message.From = new User { Id = 123456789, IsBot = true, FirstName = "TestBot" };
        message.Caption = caption;

        SentPhotos.Add(new SentPhoto(
            chatId.Identifier ?? 0,
            photo,
            caption,
            parseMode,
            replyMarkup,
            message
        ));
        
        OperationLog.Add($"SendPhoto: chatId={chatId.Identifier}, caption={caption}");

        return Task.FromResult(message);
    }

    public Task<ChatFullInfo> GetChatFullInfo(ChatId chatId, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        // Проверяем, есть ли настроенный ChatFullInfo для этого ID
        if (_chatFullInfos.TryGetValue(chatId.Identifier ?? 0, out var customChatFullInfo))
        {
            return Task.FromResult(customChatFullInfo);
        }

        // Дефолтный ChatFullInfo
        var chatFullInfo = new ChatFullInfo
        {
            Id = chatId.Identifier ?? 0,
            Type = ChatType.Group,
            Title = "Test Chat",
            Username = "testchat",
            Description = "Test chat description",
            InviteLink = "https://t.me/testchat",
            LinkedChatId = null,
            Photo = new ChatPhoto
            {
                SmallFileId = "fake_small_file_id",
                BigFileId = "fake_big_photo_id"
            }
        };

        return Task.FromResult(chatFullInfo);
    }
    
    public void SetupGetChatFullInfo(long chatId, ChatFullInfo chatFullInfo)
    {
        _chatFullInfos[chatId] = chatFullInfo;
    }

    public void SetupGetChatFullInfo(ChatFullInfo chatFullInfo)
    {
        _chatFullInfos[chatFullInfo.Id] = chatFullInfo;
    }
    
    public void SetupGetFile(string fileId, string filePath)
    {
        _filePaths[fileId] = filePath;
    }

    public Task<int> GetChatMemberCount(ChatId chatId, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        return Task.FromResult(Random.Shared.Next(10, 1000));
    }

    public Task<Update[]> GetUpdates(
        int? offset = null,
        int? limit = null,
        int? timeout = null,
        IEnumerable<UpdateType>? allowedUpdates = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        return Task.FromResult(Array.Empty<Update>());
    }

    public Task GetInfoAndDownloadFile(string fileId, Stream destination, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        // Проверяем, есть ли настроенный путь для этого fileId
        if (_filePaths.TryGetValue(fileId, out var customFilePath))
        {
            Console.WriteLine($"FakeTelegramClient: Найден путь для fileId {fileId}: {customFilePath}");
            if (File.Exists(customFilePath))
            {
                var fileBytes = File.ReadAllBytes(customFilePath);
                Console.WriteLine($"FakeTelegramClient: Файл найден, размер: {fileBytes.Length} байт");
                destination.Write(fileBytes, 0, fileBytes.Length);
                return Task.CompletedTask;
            }
            else
            {
                Console.WriteLine($"FakeTelegramClient: Файл НЕ найден: {customFilePath}");
            }
        }
        else
        {
            Console.WriteLine($"FakeTelegramClient: Путь НЕ найден для fileId: {fileId}");
        }

        // Записываем тестовые данные в поток
        var testData = Encoding.UTF8.GetBytes("fake_image_data");
        destination.Write(testData, 0, testData.Length);
        return Task.CompletedTask;
    }

    public Task AnswerCallbackQuery(string callbackQueryId, string? text = null, bool? showAlert = null, string? url = null, int? cacheTime = null, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        AnsweredCallbackQueries.Add(new AnsweredCallbackQuery(
            callbackQueryId,
            text,
            showAlert,
            url,
            cacheTime
        ));
        
        OperationLog.Add($"AnswerCallbackQuery: {callbackQueryId}, text: {text}, showAlert: {showAlert}");

        return Task.CompletedTask;
    }

    public Task EditMessageReplyMarkup(ChatId chatId, int messageId, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        EditedMessages.Add(new EditedMessage(
            chatId.Identifier ?? 0,
            messageId,
            null, // text
            replyMarkup
        ));
        
        OperationLog.Add($"EditMessageReplyMarkup: chatId={chatId.Identifier}, messageId={messageId}");

        return Task.CompletedTask;
    }

    public Task<Message> EditMessageText(ChatId chatId, int messageId, string text, ParseMode? parseMode = null, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        var message = new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = chatId.Identifier ?? 0, Type = ChatType.Group },
            From = new User { Id = 123456789, IsBot = true, FirstName = "TestBot" },
            Text = text
        };

        EditedMessages.Add(new EditedMessage(
            chatId.Identifier ?? 0,
            messageId,
            text,
            replyMarkup
        ));
        
        OperationLog.Add($"EditMessageText: chatId={chatId.Identifier}, messageId={messageId}, text={text}");

        return Task.FromResult(message);
    }

    public Task UnbanChatMember(ChatId chatId, long userId, bool? onlyIfBanned = null, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        UnbannedUsers.Add(new UnbannedUser(
            chatId.Identifier ?? 0,
            userId,
            onlyIfBanned ?? false
        ));

        return Task.CompletedTask;
    }

    public Task<ChatMember> GetChatMember(ChatId chatId, long userId, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        // По умолчанию возвращаем бота как администратора
        ChatMember chatMember;
        
        if (userId == BotId)
        {
            // Создаем базовый ChatMember и приводим к нужному типу
            var user = new User { Id = userId, IsBot = true, FirstName = "TestBot" };
            chatMember = new ChatMemberAdministrator
            {
                User = user,
                CanBeEdited = false,
                IsAnonymous = false,
                CanManageChat = true,
                CanDeleteMessages = true,
                CanManageVideoChats = true,
                CanRestrictMembers = true,
                CanPromoteMembers = true,
                CanChangeInfo = true,
                CanInviteUsers = true,
                CanPostMessages = true,
                CanEditMessages = true,
                CanPinMessages = true,
                CanPostStories = true,
                CanEditStories = true,
                CanDeleteStories = true,
                CanManageTopics = true
            };
        }
        else
        {
            var user = new User { Id = userId, IsBot = false, FirstName = "TestUser" };
            chatMember = new ChatMemberMember
            {
                User = user
            };
        }

        return Task.FromResult(chatMember);
    }

    // Методы для тестирования
    public void Reset()
    {
        SentMessages.Clear();
        DeletedMessages.Clear();
        BannedUsers.Clear();
        UnbannedUsers.Clear();
        CallbackQueries.Clear();
        AnsweredCallbackQueries.Clear();
        EditedMessages.Clear();
        SentPhotos.Clear();
        RestrictedUsers.Clear();
        OperationLog.Clear();
        ShouldThrowException = false;
        ExceptionToThrow = null;
    }

    public bool WasMessageSentTo(long chatId, string? textContains = null)
    {
        return SentMessages.Any(m => 
            m.ChatId == chatId && 
            (textContains == null || m.Text.Contains(textContains)));
    }

    public bool WasUserBanned(long chatId, long userId)
    {
        return BannedUsers.Any(b => b.ChatId == chatId && b.UserId == userId);
    }

    public bool WasMessageDeleted(long chatId, int messageId)
    {
        return DeletedMessages.Any(d => d.ChatId == chatId && d.MessageId == messageId);
    }
    
    // Методы для проверки порядка операций
    public bool WasCallbackQueryAnswered(string callbackQueryId)
    {
        return AnsweredCallbackQueries.Any(acq => acq.CallbackQueryId == callbackQueryId);
    }
    
    public bool WasMessageEdited(long chatId, int messageId)
    {
        return EditedMessages.Any(em => em.ChatId == chatId && em.MessageId == messageId);
    }
    
    public bool WasPhotoSent(long chatId, string? captionContains = null)
    {
        return SentPhotos.Any(sp => sp.ChatId == chatId && 
            (captionContains == null || (sp.Caption?.Contains(captionContains) ?? false)));
    }
    
    public bool WasUserRestricted(long chatId, long userId)
    {
        return RestrictedUsers.Any(ru => ru.ChatId == chatId && ru.UserId == userId);
    }
    
    public List<string> GetOperationLog()
    {
        return new List<string>(OperationLog);
    }
    
    public void ClearOperationLog()
    {
        OperationLog.Clear();
    }
    
    /// <summary>
    /// Регистрирует MessageEnvelope для тестовых сценариев
    /// </summary>
    public void RegisterMessageEnvelope(MessageEnvelope envelope)
    {
        _messageEnvelopes[envelope.MessageId] = envelope;
    }
    
    /// <summary>
    /// Проверяет, было ли удалено сообщение по MessageEnvelope
    /// </summary>
    public bool WasMessageDeleted(MessageEnvelope envelope)
    {
        return WasMessageDeleted(envelope.ChatId, envelope.MessageId);
    }
    
    /// <summary>
    /// Создает Message из MessageEnvelope для тестов
    /// ВНИМАНИЕ: MessageId всегда будет 0 из-за ограничений Telegram.Bot
    /// Используйте MessageEnvelope для проверки MessageId в тестах
    /// </summary>
    public Message CreateMessageFromEnvelope(MessageEnvelope envelope)
    {
        var message = TK.CreateValidMessage();
        message.Chat = new Chat 
        { 
            Id = envelope.ChatId, 
            Type = ChatType.Group,
            Title = envelope.ChatTitle ?? "Test Chat",
            Username = envelope.ChatUsername
        };
        message.From = new User 
        { 
            Id = envelope.UserId, 
            IsBot = envelope.IsBot, 
            FirstName = envelope.FirstName,
            LastName = envelope.LastName,
            Username = envelope.Username
        };
        message.Text = envelope.Text;
        message.Date = envelope.Date ?? DateTime.UtcNow;
        
        // ВНИМАНИЕ: message.MessageId останется 0 из-за ограничений Telegram.Bot
        // Используйте envelope.MessageId для проверок в тестах
        
        return message;
    }
}

// Модели для отслеживания действий
public record SentMessage(
    long ChatId,
    string Text,
    ParseMode? ParseMode,
    ReplyMarkup? ReplyMarkup,
    Message Message
);

public record DeletedMessage(
    long ChatId,
    int MessageId
);

public record BannedUser(
    long ChatId,
    long UserId,
    DateTime? UntilDate,
    bool RevokeMessages
);

public record UnbannedUser(
    long ChatId,
    long UserId,
    bool OnlyIfBanned
);

public record AnsweredCallbackQuery(
    string CallbackQueryId,
    string? Text,
    bool? ShowAlert,
    string? Url,
    int? CacheTime
);

public record EditedMessage(
    long ChatId,
    int MessageId,
    string? Text,
    ReplyMarkup? ReplyMarkup
);

public record SentPhoto(
    long ChatId,
    object Photo,
    string? Caption,
    ParseMode? ParseMode,
    ReplyMarkup? ReplyMarkup,
    Message Message
);

public record RestrictedUser(
    long ChatId,
    long UserId,
    ChatPermissions Permissions,
    DateTime? UntilDate
); 
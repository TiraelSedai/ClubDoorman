using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Text;
using ClubDoorman.Services;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// In-memory фейк для Telegram Bot API
/// Заменяет сложные моки и позволяет тестировать без реальных токенов
/// </summary>
public class FakeTelegramClient : ITelegramBotClientWrapper
{
    public List<SentMessage> SentMessages { get; } = new();
    public List<DeletedMessage> DeletedMessages { get; } = new();
    public List<BannedUser> BannedUsers { get; } = new();
    public List<UnbannedUser> UnbannedUsers { get; } = new();
    public List<CallbackQuery> CallbackQueries { get; } = new();
    
    public bool ShouldThrowException { get; set; } = false;
    public Exception? ExceptionToThrow { get; set; }
    public long BotId => 123456789;

    public Task<Message> SendMessageAsync(
        ChatId chatId,
        string text,
        ParseMode? parseMode = null,
        ReplyParameters? replyParameters = null,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
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

        SentMessages.Add(new SentMessage(
            chatId.Identifier ?? 0,
            text,
            parseMode,
            replyMarkup,
            message
        ));

        return Task.FromResult(message);
    }

    public Task<bool> DeleteMessageAsync(
        ChatId chatId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        DeletedMessages.Add(new DeletedMessage(
            chatId.Identifier ?? 0,
            messageId
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

        var message = new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = chatId.Identifier ?? 0, Type = ChatType.Group },
            From = new User { Id = 123456789, IsBot = true, FirstName = "TestBot" },
            Text = "Forwarded message"
        };
        // MessageId устанавливается через рефлексию, так как это readonly свойство
        typeof(Message).GetProperty("MessageId")?.SetValue(message, Random.Shared.Next(1, 10000));

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

        var message = new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = chatId.Identifier ?? 0, Type = ChatType.Group },
            From = new User { Id = 123456789, IsBot = true, FirstName = "TestBot" },
            Caption = caption
        };
        // MessageId устанавливается через рефлексию, так как это readonly свойство
        typeof(Message).GetProperty("MessageId")?.SetValue(message, Random.Shared.Next(1, 10000));

        return Task.FromResult(message);
    }

    public Task<ChatFullInfo> GetChatFullInfo(ChatId chatId, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        var chatFullInfo = new ChatFullInfo
        {
            Id = chatId.Identifier ?? 0,
            Type = ChatType.Group,
            Title = "Test Chat",
            Username = "testchat",
            Description = "Test chat description",
            InviteLink = "https://t.me/testchat",
            LinkedChatId = null
        };

        return Task.FromResult(chatFullInfo);
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

        // Записываем тестовые данные в поток
        var testData = Encoding.UTF8.GetBytes("fake_image_data");
        destination.Write(testData, 0, testData.Length);
        return Task.CompletedTask;
    }

    public Task AnswerCallbackQuery(string callbackQueryId, string? text = null, bool? showAlert = null, string? url = null, int? cacheTime = null, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

        return Task.CompletedTask;
    }

    public Task EditMessageReplyMarkup(ChatId chatId, int messageId, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowException)
            throw ExceptionToThrow ?? new Exception("Fake exception");

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

    // Методы для тестирования
    public void Reset()
    {
        SentMessages.Clear();
        DeletedMessages.Clear();
        BannedUsers.Clear();
        UnbannedUsers.Clear();
        CallbackQueries.Clear();
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
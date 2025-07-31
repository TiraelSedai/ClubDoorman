namespace ClubDoorman.Test.TestData;

/// <summary>
/// Обертка для тестовых сообщений, которая не зависит от ограничений Telegram.Bot
/// </summary>
public record MessageEnvelope(
    int MessageId,
    long UserId,
    long ChatId,
    string Text,
    string? Username = null,
    string? FirstName = null,
    string? LastName = null,
    bool IsBot = false,
    string? ChatTitle = null,
    string? ChatUsername = null,
    DateTime? Date = null
)
{
    /// <summary>
    /// Создает MessageEnvelope из Telegram.Bot.Types.Message
    /// </summary>
    public static MessageEnvelope FromMessage(Telegram.Bot.Types.Message message)
    {
        return new MessageEnvelope(
            MessageId: message.MessageId,
            UserId: message.From?.Id ?? 0,
            ChatId: message.Chat.Id,
            Text: message.Text ?? message.Caption ?? "",
            Username: message.From?.Username,
            FirstName: message.From?.FirstName,
            LastName: message.From?.LastName,
            IsBot: message.From?.IsBot ?? false,
            ChatTitle: message.Chat.Title,
            ChatUsername: message.Chat.Username,
            Date: message.Date
        );
    }

    /// <summary>
    /// Создает MessageEnvelope для тестовых сценариев
    /// </summary>
    public static MessageEnvelope CreateTest(
        int messageId = 123,
        long userId = 456,
        long chatId = 789,
        string text = "Test message"
    )
    {
        return new MessageEnvelope(
            MessageId: messageId,
            UserId: userId,
            ChatId: chatId,
            Text: text,
            Username: "testuser",
            FirstName: "Test",
            LastName: "User",
            ChatTitle: "Test Chat",
            Date: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Создает MessageEnvelope для спам-сообщений
    /// </summary>
    public static MessageEnvelope CreateSpam(
        int messageId = 123,
        long userId = 456,
        long chatId = 789
    )
    {
        return new MessageEnvelope(
            MessageId: messageId,
            UserId: userId,
            ChatId: chatId,
            Text: "Buy now! Spam spam spam!",
            Username: "spammer",
            FirstName: "Spam",
            LastName: "User",
            ChatTitle: "Test Chat",
            Date: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Создает MessageEnvelope для новых участников
    /// </summary>
    public static MessageEnvelope CreateNewUser(
        int messageId = 123,
        long userId = 456,
        long chatId = 789
    )
    {
        return new MessageEnvelope(
            MessageId: messageId,
            UserId: userId,
            ChatId: chatId,
            Text: "", // Пустой текст для новых участников
            Username: "newuser",
            FirstName: "New",
            LastName: "User",
            ChatTitle: "Test Chat",
            Date: DateTime.UtcNow
        );
    }
} 
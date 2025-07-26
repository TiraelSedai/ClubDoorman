using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.TestData;

/// <summary>
/// Фабрика тестовых данных для сообщений
/// </summary>
public static class MessageTestData
{
    public static User TestUser(string? username = "test_user", string? firstName = "Test", string? lastName = "User")
    {
        return new User
        {
            Id = 12345,
            IsBot = false,
            FirstName = firstName ?? "Test",
            LastName = lastName,
            Username = username
        };
    }

    public static User TestBot(string? username = "test_bot", string? firstName = "TestBot")
    {
        return new User
        {
            Id = 123456789,
            IsBot = true,
            FirstName = firstName ?? "TestBot",
            Username = username
        };
    }

    public static Chat TestChat(string? title = "Test Chat", ChatType type = ChatType.Group)
    {
        return new Chat
        {
            Id = 123456,
            Type = type,
            Title = title ?? "Test Chat"
        };
    }

    public static Message ValidMessage(string? text = "Hello, this is a valid message!")
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = TestChat(),
            From = TestUser(),
            Text = text ?? "Hello, this is a valid message!"
        };
    }

    public static Message SpamMessage(string? text = "BUY NOW!!! CLICK HERE!!!")
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = TestChat(),
            From = TestUser(),
            Text = text ?? "BUY NOW!!! CLICK HERE!!!"
        };
    }

    public static Message BotMessage(string? text = "Bot message")
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = TestChat(),
            From = TestBot(),
            Text = text ?? "Bot message"
        };
    }

    public static Message ServiceMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = TestChat(),
            From = TestUser(), // Service messages can have From for new members
            Text = null,
            NewChatMembers = new[] { TestUser() }
        };
    }

    public static Message StartCommand()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = TestChat(),
            From = TestUser(),
            Text = "/start"
        };
    }

    public static Message SuspiciousCommand()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = TestChat(),
            From = TestUser(),
            Text = "/suspicious"
        };
    }

    public static Message UnknownCommand()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = TestChat(),
            From = TestUser(),
            Text = "/unknown"
        };
    }

    public static Message NullMessage() => null!;

    public static Message EmptyMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = TestChat(),
            From = TestUser(),
            Text = ""
        };
    }

    public static Message VeryLongMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = TestChat(),
            From = TestUser(),
            Text = new string('A', 10000) // Very long message
        };
    }

    public static Message MessageWithSpecialCharacters()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = TestChat(),
            From = TestUser(),
            Text = "Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?"
        };
    }

    /// <summary>
    /// Тестовый пользователь @Dnekxpb с подозрительным профилем
    /// </summary>
    public static User SuspiciousUserDnekxpb()
    {
        return new User
        {
            Id = 987654321,
            IsBot = false,
            FirstName = "Manu",
            LastName = "Чыфыс",
            Username = "Dnekxpb"
        };
    }

    /// <summary>
    /// Сообщение от подозрительного пользователя @Dnekxpb
    /// </summary>
    public static Message SuspiciousUserMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = TestChat(),
            From = SuspiciousUserDnekxpb(),
            Text = "Продам слона пиши с лс"
        };
    }

    /// <summary>
    /// Полная информация о чате пользователя @Dnekxpb
    /// </summary>
    public static ChatFullInfo SuspiciousUserChatInfo()
    {
        return new ChatFullInfo
        {
            Id = 987654321,
            Type = ChatType.Private,
            Title = null,
            Username = "Dnekxpb",
            Bio = "Митиман\n\nManu Чыфыс:\nПродам слона пиши с лс",
            LinkedChatId = null,
            Photo = new ChatPhoto
            {
                SmallFileId = "fake_small_photo_id",
                BigFileId = "fake_big_photo_id"
            }
        };
    }

    /// <summary>
    /// Тестовый пользователь с явно подозрительным профилем
    /// </summary>
    public static User VerySuspiciousUser()
    {
        return new User
        {
            Id = 111222333,
            IsBot = false,
            FirstName = "🔥💰💎",
            LastName = "ПРЕМИУМ",
            Username = "premium_crypto_2024"
        };
    }

    /// <summary>
    /// Сообщение от очень подозрительного пользователя
    /// </summary>
    public static Message VerySuspiciousUserMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = TestChat(),
            From = VerySuspiciousUser(),
            Text = "🔥 ЗАРАБОТАЙ 1000$ В ДЕНЬ! 💰 КРИПТОВАЛЮТА! 💎 НАЖМИ СЕЙЧАС!"
        };
    }

    /// <summary>
    /// Полная информация о чате очень подозрительного пользователя
    /// </summary>
    public static ChatFullInfo VerySuspiciousUserChatInfo()
    {
        return new ChatFullInfo
        {
            Id = 111222333,
            Type = ChatType.Private,
            Title = null,
            Username = "premium_crypto_2024",
            Bio = "🔥 ПРЕМИУМ КРИПТО ТРЕЙДИНГ 💰\n\n💎 ЗАРАБОТАЙ 1000$ В ДЕНЬ!\n🔥 НАЖМИ СЕЙЧАС!\n💰 БЕСПЛАТНО!\n\n📱 Telegram: @crypto_scam\n🌐 Сайт: scam.crypto",
            LinkedChatId = null,
            Photo = new ChatPhoto
            {
                SmallFileId = "fake_suspicious_small_photo_id",
                BigFileId = "fake_suspicious_big_photo_id"
            }
        };
    }
} 
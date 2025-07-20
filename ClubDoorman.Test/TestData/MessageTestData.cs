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
            From = TestUser(), // Service messages can have From
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
            Text = "Message with special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?"
        };
    }
} 
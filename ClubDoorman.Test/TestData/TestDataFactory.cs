using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Models;

namespace ClubDoorman.Test.TestData;

/// <summary>
/// Фабрика для создания тестовых данных
/// </summary>
public static class TestDataFactory
{
    #region Messages

    public static Message CreateValidMessage()
    {
        return new Message
        {
            MessageId = 1,
            Date = DateTime.UtcNow,
            Text = "Hello, this is a valid message!",
            From = CreateValidUser(),
            Chat = CreateGroupChat()
        };
    }

    public static Message CreateSpamMessage()
    {
        return new Message
        {
            MessageId = 2,
            Date = DateTime.UtcNow,
            Text = "BUY NOW!!! AMAZING OFFER!!! CLICK HERE!!!",
            From = CreateValidUser(),
            Chat = CreateGroupChat()
        };
    }

    public static Message CreateEmptyMessage()
    {
        return new Message
        {
            MessageId = 3,
            Date = DateTime.UtcNow,
            Text = "",
            From = CreateValidUser(),
            Chat = CreateGroupChat()
        };
    }

    public static Message CreateNullTextMessage()
    {
        return new Message
        {
            MessageId = 4,
            Date = DateTime.UtcNow,
            Text = null,
            From = CreateValidUser(),
            Chat = CreateGroupChat()
        };
    }

    public static Message CreateLongMessage()
    {
        return new Message
        {
            MessageId = 5,
            Date = DateTime.UtcNow,
            Text = new string('A', 1000),
            From = CreateValidUser(),
            Chat = CreateGroupChat()
        };
    }

    #endregion

    #region Users

    public static User CreateValidUser()
    {
        return new User
        {
            Id = 123456789,
            IsBot = false,
            FirstName = "Test",
            LastName = "User",
            Username = "testuser"
        };
    }

    public static User CreateBotUser()
    {
        return new User
        {
            Id = 987654321,
            IsBot = true,
            FirstName = "TestBot",
            Username = "testbot"
        };
    }

    public static User CreateAnonymousUser()
    {
        return new User
        {
            Id = 111222333,
            IsBot = false,
            FirstName = "Anonymous"
        };
    }

    #endregion

    #region Chats

    public static Chat CreateGroupChat()
    {
        return new Chat
        {
            Id = -1001234567890,
            Type = ChatType.Group,
            Title = "Test Group",
            Username = "testgroup"
        };
    }

    public static Chat CreateSupergroupChat()
    {
        return new Chat
        {
            Id = -1009876543210,
            Type = ChatType.Supergroup,
            Title = "Test Supergroup",
            Username = "testsupergroup"
        };
    }

    public static Chat CreatePrivateChat()
    {
        return new Chat
        {
            Id = 123456789,
            Type = ChatType.Private,
            FirstName = "Test",
            LastName = "User",
            Username = "testuser"
        };
    }

    #endregion

    #region Callback Queries

    public static CallbackQuery CreateValidCallbackQuery()
    {
        return new CallbackQuery
        {
            Id = "test_callback_id",
            From = CreateValidUser(),
            Message = CreateValidMessage(),
            Data = "test_data"
        };
    }

    public static CallbackQuery CreateInvalidCallbackQuery()
    {
        return new CallbackQuery
        {
            Id = "invalid_callback_id",
            From = CreateValidUser(),
            Data = "invalid_data"
        };
    }

    #endregion

    #region Chat Members

    public static ChatMemberUpdated CreateMemberJoined()
    {
        return new ChatMemberUpdated
        {
            Chat = CreateGroupChat(),
            From = CreateValidUser(),
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMemberMember(),
            NewChatMember = new ChatMemberMember()
        };
    }

    public static ChatMemberUpdated CreateMemberLeft()
    {
        return new ChatMemberUpdated
        {
            Chat = CreateGroupChat(),
            From = CreateValidUser(),
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMemberMember(),
            NewChatMember = new ChatMemberLeft()
        };
    }

    public static ChatMemberUpdated CreateMemberBanned()
    {
        return new ChatMemberUpdated
        {
            Chat = CreateGroupChat(),
            From = CreateValidUser(),
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMemberMember(),
            NewChatMember = new ChatMemberBanned()
        };
    }

    public static ChatMemberUpdated CreateMemberRestricted()
    {
        return new ChatMemberUpdated
        {
            Chat = CreateGroupChat(),
            From = CreateValidUser(),
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMemberMember(),
            NewChatMember = new ChatMemberRestricted()
        };
    }

    public static ChatMemberUpdated CreateMemberPromoted()
    {
        return new ChatMemberUpdated
        {
            Chat = CreateGroupChat(),
            From = CreateValidUser(),
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMemberMember(),
            NewChatMember = new ChatMemberAdministrator()
        };
    }

    public static ChatMemberUpdated CreateMemberDemoted()
    {
        return new ChatMemberUpdated
        {
            Chat = CreateGroupChat(),
            From = CreateValidUser(),
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMemberAdministrator(),
            NewChatMember = new ChatMemberMember()
        };
    }

    #endregion

    #region Updates

    public static Update CreateMessageUpdate()
    {
        return new Update
        {
            Message = CreateValidMessage()
        };
    }

    public static Update CreateCallbackQueryUpdate()
    {
        return new Update
        {
            CallbackQuery = CreateValidCallbackQuery()
        };
    }

    public static Update CreateChatMemberUpdate()
    {
        return new Update
        {
            ChatMember = CreateMemberJoined()
        };
    }

    #endregion

    #region Moderation Results

    public static ModerationResult CreateAllowResult()
    {
        return new ModerationResult
        {
            Action = ModerationAction.Allow,
            Reason = "Message passed all checks",
            Confidence = 0.95f
        };
    }

    public static ModerationResult CreateDeleteResult()
    {
        return new ModerationResult
        {
            Action = ModerationAction.Delete,
            Reason = "Spam detected",
            Confidence = 0.85f
        };
    }

    public static ModerationResult CreateBanResult()
    {
        return new ModerationResult
        {
            Action = ModerationAction.Ban,
            Reason = "Repeated violations",
            Confidence = 0.90f
        };
    }

    #endregion

    #region Captcha Info

    public static CaptchaInfo CreateValidCaptchaInfo()
    {
        return new CaptchaInfo(
            chatId: 123456789,
            chatTitle: "Test Group",
            timestamp: DateTime.UtcNow,
            user: CreateValidUser(),
            correctAnswer: 1,
            cts: new CancellationTokenSource(),
            userJoinedMessage: CreateValidMessage()
        );
    }

    public static CaptchaInfo CreateExpiredCaptchaInfo()
    {
        return new CaptchaInfo(
            chatId: 123456789,
            chatTitle: "Test Group",
            timestamp: DateTime.UtcNow.AddMinutes(-10),
            user: CreateValidUser(),
            correctAnswer: 2,
            cts: new CancellationTokenSource(),
            userJoinedMessage: null
        );
    }

    #endregion
} 
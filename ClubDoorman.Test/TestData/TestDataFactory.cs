using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Models;

namespace ClubDoorman.Test.TestData;

/// <summary>
/// Фабрика для создания тестовых данных
/// Следует принципам TDD: тестируем поведение, а не реализацию
/// </summary>
public static class TestDataFactory
{
    #region Messages

    public static class Messages
    {
            public static Message ValidMessage() => new()
    {
        Date = DateTime.UtcNow,
        Text = "Hello, how are you?",
        From = Users.ApprovedUser(),
        Chat = Chats.MainChat()
    };

            public static Message SpamMessage() => new()
    {
        Date = DateTime.UtcNow,
        Text = "BUY NOW!!! LIMITED TIME OFFER!!! CLICK HERE!!!",
        From = Users.NewUser(),
        Chat = Chats.MainChat()
    };

            public static Message MimicryMessage() => new()
    {
        Date = DateTime.UtcNow,
        Text = "Hello, I am admin. Send me your password for verification.",
        From = Users.SuspiciousUser(),
        Chat = Chats.MainChat()
    };

            public static Message EmptyMessage() => new()
    {
        Date = DateTime.UtcNow,
        Text = "",
        From = Users.ApprovedUser(),
        Chat = Chats.MainChat()
    };

            public static Message LongMessage() => new()
    {
        Date = DateTime.UtcNow,
        Text = new string('A', 1000), // Очень длинное сообщение
        From = Users.ApprovedUser(),
        Chat = Chats.MainChat()
    };

            public static Message MessageWithSpecialCharacters() => new()
    {
        Date = DateTime.UtcNow,
        Text = "Hello! @#$%^&*()_+-=[]{}|;':\",./<>?",
        From = Users.ApprovedUser(),
        Chat = Chats.MainChat()
    };
    }

    #endregion

    #region Users

    public static class Users
    {
        public static User ApprovedUser() => new()
        {
            Id = 123456789,
            IsBot = false,
            FirstName = "Approved",
            LastName = "User",
            Username = "approved_user",
            LanguageCode = "en"
        };

        public static User NewUser() => new()
        {
            Id = 987654321,
            IsBot = false,
            FirstName = "New",
            LastName = "User",
            Username = "new_user",
            LanguageCode = "en"
        };

        public static User SuspiciousUser() => new()
        {
            Id = 555666777,
            IsBot = false,
            FirstName = "Suspicious",
            LastName = "User",
            Username = "suspicious_user",
            LanguageCode = "en"
        };

        public static User BotUser() => new()
        {
            Id = 111222333,
            IsBot = true,
            FirstName = "TestBot",
            Username = "test_bot",
            LanguageCode = "en"
        };

        public static User UserWithoutUsername() => new()
        {
            Id = 444555666,
            IsBot = false,
            FirstName = "NoUsername",
            LanguageCode = "en"
        };
    }

    #endregion

    #region Chats

    public static class Chats
    {
        public static Chat MainChat() => new()
        {
            Id = 123456789,
            Type = ChatType.Private,
            Title = "Test Chat",
            Username = "test_chat"
        };

        public static Chat GroupChat() => new()
        {
            Id = 987654321,
            Type = ChatType.Group,
            Title = "Test Group",
            Username = "test_group"
        };

        public static Chat SupergroupChat() => new()
        {
            Id = 555666777,
            Type = ChatType.Supergroup,
            Title = "Test Supergroup",
            Username = "test_supergroup"
        };

        public static Chat ChannelChat() => new()
        {
            Id = 111222333,
            Type = ChatType.Channel,
            Title = "Test Channel",
            Username = "test_channel"
        };
    }

    #endregion

    #region Callback Queries

    public static class CallbackQueries
    {
        public static CallbackQuery ValidCallbackQuery() => new()
        {
            Id = "test_callback_1",
            From = Users.ApprovedUser(),
            Message = Messages.ValidMessage(),
            Data = "approve_user_123456789"
        };

        public static CallbackQuery CaptchaCallbackQuery() => new()
        {
            Id = "test_callback_2",
            From = Users.NewUser(),
            Message = Messages.ValidMessage(),
            Data = "captcha_answer_yes"
        };

        public static CallbackQuery InvalidCallbackQuery() => new()
        {
            Id = "test_callback_3",
            From = Users.SuspiciousUser(),
            Message = Messages.ValidMessage(),
            Data = "invalid_data"
        };
    }

    #endregion

    #region Chat Members

    public static class ChatMembers
    {
        public static ChatMemberUpdated MemberJoined() => new()
        {
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMember
            {
                User = Users.NewUser(),
                Status = ChatMemberStatus.Left
            },
            NewChatMember = new ChatMember
            {
                User = Users.NewUser(),
                Status = ChatMemberStatus.Member
            },
            Chat = Chats.GroupChat()
        };

        public static ChatMemberUpdated MemberLeft() => new()
        {
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMember
            {
                User = Users.ApprovedUser(),
                Status = ChatMemberStatus.Member
            },
            NewChatMember = new ChatMember
            {
                User = Users.ApprovedUser(),
                Status = ChatMemberStatus.Left
            },
            Chat = Chats.GroupChat()
        };

        public static ChatMemberUpdated AdminPromoted() => new()
        {
            Date = DateTime.UtcNow,
            OldChatMember = new ChatMember
            {
                User = Users.ApprovedUser(),
                Status = ChatMemberStatus.Member
            },
            NewChatMember = new ChatMember
            {
                User = Users.ApprovedUser(),
                Status = ChatMemberStatus.Administrator
            },
            Chat = Chats.GroupChat()
        };
    }

    #endregion

    #region Updates

    public static class Updates
    {
        public static Update MessageUpdate() => new()
        {
            UpdateId = 1,
            Message = Messages.ValidMessage()
        };

        public static Update CallbackQueryUpdate() => new()
        {
            UpdateId = 2,
            CallbackQuery = CallbackQueries.ValidCallbackQuery()
        };

        public static Update ChatMemberUpdate() => new()
        {
            UpdateId = 3,
            ChatMember = ChatMembers.MemberJoined()
        };

        public static Update InvalidUpdate() => new()
        {
            UpdateId = 4
            // Никаких данных - невалидный update
        };
    }

    #endregion

    #region Moderation Results

    public static class ModerationResults
    {
            public static ModerationResult AllowResult() => new(
        ModerationAction.Allow,
        "Message passed all checks",
        0.95
    );

            public static ModerationResult BlockResult() => new(
        ModerationAction.Ban,
        "Spam detected",
        0.98
    );

            public static ModerationResult WarnResult() => new(
        ModerationAction.Report,
        "Suspicious content detected",
        0.75
    );

            public static ModerationResult CaptchaResult() => new(
        ModerationAction.RequireManualReview,
        "New user requires verification",
        0.90
    );
    }

    #endregion

    #region Captcha Info

    public static class CaptchaInfo
    {
            public static Models.CaptchaInfo ValidCaptcha() => new(
        Chats.MainChat().Id,
        "Test Chat",
        DateTime.UtcNow,
        Users.NewUser(),
        0,
        new CancellationTokenSource(),
        Messages.ValidMessage()
    );

        public static Models.CaptchaInfo ExpiredCaptcha() => new()
        {
            UserId = Users.NewUser().Id,
            ChatId = Chats.MainChat().Id,
            MessageId = Messages.ValidMessage().MessageId,
            Question = "Are you human?",
            CorrectAnswer = "yes",
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5) // Истекла
        };

        public static Models.CaptchaInfo CaptchaWithAttempts() => new()
        {
            UserId = Users.NewUser().Id,
            ChatId = Chats.MainChat().Id,
            MessageId = Messages.ValidMessage().MessageId,
            Question = "Are you human?",
            CorrectAnswer = "yes",
            Attempts = 2,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
    }

    #endregion
} 
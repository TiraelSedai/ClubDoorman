using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Models;
using System;
using System.Threading;
using System.Collections.Generic;

namespace ClubDoorman.Test.TestData;

/// <summary>
/// Фабрика для создания тестовых данных
/// Автоматически сгенерировано
/// </summary>
public static class TestDataFactory
{
    #region Telegram Types

    public static Message CreateValidMessage()
    {
        return new Message
        {
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
            Date = DateTime.UtcNow,
            Text = new string('A', 1000),
            From = CreateValidUser(),
            Chat = CreateGroupChat()
        };
    }

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
            Id = 111111111,
            IsBot = false,
            FirstName = "Anonymous",
            LastName = null,
            Username = null
        };
    }

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
            FirstName = "Private",
            LastName = "Chat"
        };
    }

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
            Message = null,
            Data = null
        };
    }

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

    #region Domain Models

    public static SuspiciousUserInfo CreateValidSuspiciousUserInfo()
    {
        return new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "First message", "Second message", "Third message" },
            0.5,
            true,
            0
        );
    }
    public static ModerationResult CreateValidModerationResult()
    {
        return new ModerationResult(
            ModerationAction.Allow,
            "Test reason"
        );
    }

    public static ModerationResult CreateEmptyModerationResult()
    {
        return new ModerationResult(
            ModerationAction.Allow,
            ""
        );
    }

    public static ModerationResult CreateLongModerationResult()
    {
        return new ModerationResult(
            ModerationAction.Allow,
            new string('A', 1000)
        );
    }

    public static ModerationResult CreateAllowResult()
    {
        return new ModerationResult(
            ModerationAction.Allow,
            "Message allowed"
        );
    }

    public static ModerationResult CreateDeleteResult()
    {
        return new ModerationResult(
            ModerationAction.Delete,
            "Message deleted"
        );
    }

    public static ModerationResult CreateBanResult()
    {
        return new ModerationResult(
            ModerationAction.Ban,
            "User banned"
        );
    }

    public static CaptchaInfo CreateExpiredCaptchaInfo()
    {
        return new CaptchaInfo(
            123456789,
            "Expired Chat",
            DateTime.UtcNow.AddHours(-2),
            TestDataFactory.CreateValidUser(),
            1,
            new CancellationTokenSource(),
            null
        );
    }
    public static ModerationAction CreateAllowModerationAction()
    {
        return ModerationAction.Allow;
    }

    public static ModerationAction CreateDeleteModerationAction()
    {
        return ModerationAction.Delete;
    }

    public static ModerationAction CreateBanModerationAction()
    {
        return ModerationAction.Ban;
    }

    public static ModerationAction CreateReportModerationAction()
    {
        return ModerationAction.Report;
    }

    public static ModerationAction CreateRequireManualReviewModerationAction()
    {
        return ModerationAction.RequireManualReview;
    }
    public static CaptchaInfo CreateValidCaptchaInfo()
    {
        return new CaptchaInfo(
            123456789,
            "Chattitle Title",
            DateTime.UtcNow,
            TestDataFactory.CreateValidUser(),
            1,
            new CancellationTokenSource(),
            null
        );
    }
    #endregion
}

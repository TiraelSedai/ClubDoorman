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
            new List<string> { "test1", "test2" },
            0.5,
            true,
            0
        );
    }
    public static ModerationResult CreateValidModerationResult()
    {
        return new ModerationResult(
            ModerationAction.Allow,
            "test_value"
        );
    }
    public static CaptchaInfo CreateValidCaptchaInfo()
    {
        return new CaptchaInfo(
            123456789L,
            "test-chat",
            DateTime.UtcNow,
            CreateValidUser(),
            42,
            new CancellationTokenSource(),
            CreateValidMessage()
        );
    }
    // Дополнительные методы для совместимости с существующими тестами
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
            Text = "This is a very long message that contains a lot of text and should be considered as a long message for testing purposes. " + 
                   "It has multiple sentences and should trigger any logic that handles long messages. " +
                   "The message continues with more content to ensure it's properly classified as long.",
            From = CreateValidUser(),
            Chat = CreateGroupChat()
        };
    }
    
    public static ModerationResult CreateAllowResult()
    {
        return new ModerationResult(ModerationAction.Allow, "Message allowed");
    }
    
    public static ModerationResult CreateDeleteResult()
    {
        return new ModerationResult(ModerationAction.Delete, "Message deleted");
    }
    
    public static ModerationResult CreateBanResult()
    {
        return new ModerationResult(ModerationAction.Ban, "User banned");
    }
    
    public static CaptchaInfo CreateExpiredCaptchaInfo()
    {
        return new CaptchaInfo(
            123456789L,
            "expired-chat",
            DateTime.UtcNow.AddHours(-2), // Expired 2 hours ago
            CreateValidUser(),
            3,
            new CancellationTokenSource(),
            CreateValidMessage()
        );
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
    
    public static Chat CreatePrivateChat()
    {
        return new Chat
        {
            Id = 123456789,
            Type = ChatType.Private,
            Title = "Private Chat",
            Username = "privateuser"
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


    public static Message CreateNewUserJoinMessage(long userId = 12345)
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            From = CreateValidUser(),
            Chat = CreateGroupChat(),
            NewChatMembers = new[]
            {
                new User
                {
                    Id = userId,
                    FirstName = "NewUser",
                    Username = $"user{userId}",
                    IsBot = false
                }
            }
        };
    }

    public static Message CreateSuspiciousUserMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Text = "Hello everyone!",
            From = new User
            {
                Id = 999999,
                FirstName = "Suspicious",
                Username = "suspicious_user",
                IsBot = false
            },
            Chat = CreateGroupChat()
        };
    }

    public static Message CreateAdminNotificationMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Text = "Новый пользователь присоединился к чату",
            From = new User
            {
                Id = 123456789, // ID админа
                FirstName = "Admin",
                Username = "admin",
                IsBot = false
            },
            Chat = new Chat
            {
                Id = 123456789,
                Title = "Admin Chat",
                Type = ChatType.Private
            },
            ReplyMarkup = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton("🥰 Свой") { CallbackData = "approve_user" },
                new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton("🤖 Бан") { CallbackData = "ban_user" },
                new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton("😶 Пропуск") { CallbackData = "skip_user" }
            })
        };
    }

    public static CallbackQuery CreateAdminApproveCallback()
    {
        return new CallbackQuery
        {
            Id = Guid.NewGuid().ToString(),
            From = new User
            {
                Id = 123456789,
                FirstName = "Admin",
                Username = "admin",
                IsBot = false
            },
            Message = CreateAdminNotificationMessage(),
            Data = "approve_user"
        };
    }

    public static CallbackQuery CreateAdminBanCallback()
    {
        return new CallbackQuery
        {
            Id = Guid.NewGuid().ToString(),
            From = new User
            {
                Id = 123456789,
                FirstName = "Admin",
                Username = "admin",
                IsBot = false
            },
            Message = CreateAdminNotificationMessage(),
            Data = "ban_user"
        };
    }

    public static CallbackQuery CreateAdminSkipCallback()
    {
        return new CallbackQuery
        {
            Id = Guid.NewGuid().ToString(),
            From = new User
            {
                Id = 123456789,
                FirstName = "Admin",
                Username = "admin",
                IsBot = false
            },
            Message = CreateAdminNotificationMessage(),
            Data = "skip_user"
        };
    }

    public static Message CreateStatsCommandMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Text = "/stats",
            From = CreateValidUser(),
            Chat = CreateGroupChat()
        };
    }

    public static Message CreateHelpCommandMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Text = "/help",
            From = CreateValidUser(),
            Chat = CreateGroupChat()
        };
    }

    public static CaptchaInfo CreateBaitCaptchaInfo()
    {
        var user = CreateValidUser();
        var chat = CreateGroupChat();
        var cts = new CancellationTokenSource();
        
        return new CaptchaInfo(
            chat.Id,
            chat.Title,
            DateTime.UtcNow,
            user,
            0,
            cts,
            null
        );
    }

    public static bool CreateCorrectCaptchaResult()
    {
        return true;
    }

    public static bool CreateIncorrectCaptchaResult()
    {
        return false;
    }

    public static User CreateBaitUser()
    {
        return new User
        {
            Id = 666666,
            FirstName = "Bait",
            Username = "bait_user",
            IsBot = false
        };
    }

    #endregion
}

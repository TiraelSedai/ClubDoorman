using ClubDoorman.Models;
using ClubDoorman.Test.TestData;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.TestKit
{
    /// <summary>
    /// Специализированные генераторы для конкретных доменных областей
    /// <tags>specialized, generators, domain-specific</tags>
    /// </summary>
    public static partial class TestKit
    {
        /// <summary>
        /// Специализированные генераторы для конкретных доменных областей
        /// <tags>specialized, generators, domain-specific</tags>
        /// </summary>
        public static class Specialized
        {
            /// <summary>
            /// Генераторы для работы с капчей
            /// <tags>captcha, moderation, user-verification</tags>
            /// </summary>
            public static class Captcha
            {
                /// <summary>
                /// Валидная капча для тестов
                /// <tags>captcha, valid, user-verification</tags>
                /// </summary>
                public static CaptchaInfo Valid() => TestDataFactory.CreateValidCaptchaInfo();
                
                /// <summary>
                /// Истекшая капча для тестов
                /// <tags>captcha, expired, user-verification</tags>
                /// </summary>
                public static CaptchaInfo Expired() => TestDataFactory.CreateExpiredCaptchaInfo();
                
                /// <summary>
                /// Приманка-капча для тестов
                /// <tags>captcha, bait, user-verification</tags>
                /// </summary>
                public static CaptchaInfo Bait() => TestDataFactory.CreateBaitCaptchaInfo();
                
                /// <summary>
                /// Правильный результат капчи
                /// <tags>captcha, correct, result</tags>
                /// </summary>
                public static bool CorrectResult() => TestDataFactory.CreateCorrectCaptchaResult();
                
                /// <summary>
                /// Неправильный результат капчи
                /// <tags>captcha, incorrect, result</tags>
                /// </summary>
                public static bool IncorrectResult() => TestDataFactory.CreateIncorrectCaptchaResult();
            }

            /// <summary>
            /// Генераторы для результатов модерации
            /// <tags>moderation, ml, ai, spam-detection</tags>
            /// </summary>
            public static class Moderation
            {
                /// <summary>
                /// Результат модерации: разрешить сообщение
                /// <tags>moderation, allow, ml, ai</tags>
                /// </summary>
                public static ModerationResult Allow() => TestDataFactory.CreateAllowResult();
                
                /// <summary>
                /// Результат модерации: удалить сообщение
                /// <tags>moderation, delete, ml, ai</tags>
                /// </summary>
                public static ModerationResult Delete() => TestDataFactory.CreateDeleteResult();
                
                /// <summary>
                /// Результат модерации: забанить пользователя
                /// <tags>moderation, ban, ml, ai</tags>
                /// </summary>
                public static ModerationResult Ban() => TestDataFactory.CreateBanResult();
            }

            /// <summary>
            /// Генераторы для админских действий
            /// <tags>admin, callback, moderation, admin-actions</tags>
            /// </summary>
            public static class Admin
            {
                /// <summary>
                /// Callback для одобрения пользователя админом
                /// <tags>admin, callback, approve, moderation</tags>
                /// </summary>
                public static CallbackQuery ApproveCallback() => TestDataFactory.CreateAdminApproveCallback();
                
                /// <summary>
                /// Callback для бана пользователя админом
                /// <tags>admin, callback, ban, moderation</tags>
                /// </summary>
                public static CallbackQuery BanCallback() => TestDataFactory.CreateAdminBanCallback();
                
                /// <summary>
                /// Callback для пропуска пользователя админом
                /// <tags>admin, callback, skip, moderation</tags>
                /// </summary>
                public static CallbackQuery SkipCallback() => TestDataFactory.CreateAdminSkipCallback();
                
                /// <summary>
                /// Уведомление для админов
                /// <tags>admin, notification, moderation</tags>
                /// </summary>
                public static Message Notification() => TestDataFactory.CreateAdminNotificationMessage();
                
                /// <summary>
                /// Команда статистики для админов
                /// <tags>admin, stats, command</tags>
                /// </summary>
                public static Message StatsCommand() => TestDataFactory.CreateStatsCommandMessage();
                
                /// <summary>
                /// Команда помощи для админов
                /// <tags>admin, help, command</tags>
                /// </summary>
                public static Message HelpCommand() => TestDataFactory.CreateHelpCommandMessage();
            }

            /// <summary>
            /// Генераторы для обновлений чата
            /// <tags>chat, updates, member-management</tags>
            /// </summary>
            public static class Updates
            {
                /// <summary>
                /// Пользователь присоединился к чату
                /// <tags>chat, member, joined, updates</tags>
                /// </summary>
                public static ChatMemberUpdated MemberJoined() => TestDataFactory.CreateMemberJoined();
                
                /// <summary>
                /// Пользователь покинул чат
                /// <tags>chat, member, left, updates</tags>
                /// </summary>
                public static ChatMemberUpdated MemberLeft() => TestDataFactory.CreateMemberLeft();
                
                /// <summary>
                /// Пользователь забанен в чате
                /// <tags>chat, member, banned, updates, ban</tags>
                /// </summary>
                public static ChatMemberUpdated MemberBanned() => TestDataFactory.CreateMemberBanned();
                
                /// <summary>
                /// Пользователь ограничен в чате
                /// <tags>chat, member, restricted, updates</tags>
                /// </summary>
                public static ChatMemberUpdated MemberRestricted() => TestDataFactory.CreateMemberRestricted();
                
                /// <summary>
                /// Пользователь повышен в чате
                /// <tags>chat, member, promoted, updates</tags>
                /// </summary>
                public static ChatMemberUpdated MemberPromoted() => TestDataFactory.CreateMemberPromoted();
                
                /// <summary>
                /// Пользователь понижен в чате
                /// <tags>chat, member, demoted, updates</tags>
                /// </summary>
                public static ChatMemberUpdated MemberDemoted() => TestDataFactory.CreateMemberDemoted();
            }

            /// <summary>
            /// Генераторы для callback query'ев
            /// <tags>callback, query, interaction</tags>
            /// </summary>
            public static class Callbacks
            {
                /// <summary>
                /// Валидный callback query
                /// <tags>callback, valid, interaction</tags>
                /// </summary>
                public static CallbackQuery Valid() => TestDataFactory.CreateValidCallbackQuery();
                
                /// <summary>
                /// Невалидный callback query
                /// <tags>callback, invalid, interaction</tags>
                /// </summary>
                public static CallbackQuery Invalid() => TestDataFactory.CreateInvalidCallbackQuery();
            }

            /// <summary>
            /// Генераторы для специальных сообщений
            /// <tags>messages, special, domain-specific</tags>
            /// </summary>
            public static class Messages
            {
                /// <summary>
                /// Сообщение от подозрительного пользователя
                /// <tags>messages, suspicious, user, ai-analysis</tags>
                /// </summary>
                public static Message SuspiciousUser() => TestDataFactory.CreateSuspiciousUserMessage();
                
                /// <summary>
                /// Сообщение о присоединении нового пользователя
                /// <tags>messages, new-user, join, member</tags>
                /// </summary>
                public static Message NewUserJoin(long userId = 12345) => TestDataFactory.CreateNewUserJoinMessage(userId);
            }

            /// <summary>
            /// Генераторы для специальных пользователей
            /// <tags>users, special, domain-specific</tags>
            /// </summary>
            public static class Users
            {
                /// <summary>
                /// Пользователь-приманка для тестов
                /// <tags>users, bait, suspicious, ai-analysis</tags>
                /// </summary>
                public static User Bait() => TestDataFactory.CreateBaitUser();
            }

            /// <summary>
            /// Генераторы для тестов банов
            /// <tags>ban, moderation, user-management, integration</tags>
            /// </summary>
            public static class BanTests
            {
                /// <summary>
                /// Пользователь для тестов банов
                /// <tags>ban, user, integration</tags>
                /// </summary>
                public static User UserForBan() => TestDataFactory.CreateValidUser();
                
                /// <summary>
                /// Чат для тестов банов
                /// <tags>ban, chat, integration</tags>
                /// </summary>
                public static Chat ChatForBanTest() => TestDataFactory.CreateGroupChat();
                
                /// <summary>
                /// Спам сообщение для тестов банов
                /// <tags>ban, spam, message, moderation</tags>
                /// </summary>
                public static Message SpamMessage() => TestDataFactory.CreateSpamMessage();
                
                /// <summary>
                /// Сообщение от канала для тестов банов
                /// <tags>ban, channel, message, moderation</tags>
                /// </summary>
                public static Message ChannelMessage() => TestDataFactory.CreateChannelMessage(-100123456789, -1009876543210);
                
                /// <summary>
                /// Результат модерации: бан
                /// <tags>ban, moderation, result, ml</tags>
                /// </summary>
                public static ModerationResult BanResult(string reason = "Спам") => new ModerationResult(ModerationAction.Ban, reason);
                
                /// <summary>
                /// Результат модерации: удаление
                /// <tags>ban, moderation, delete, result, ml</tags>
                /// </summary>
                public static ModerationResult DeleteResult(string reason = "ML решил что это спам") => new ModerationResult(ModerationAction.Delete, reason);
                
                /// <summary>
                /// Результат модерации: разрешить
                /// <tags>ban, moderation, allow, result, ml</tags>
                /// </summary>
                public static ModerationResult AllowResult(string reason = "Valid message") => new ModerationResult(ModerationAction.Allow, reason);

                /// <summary>
                /// Сценарий для тестирования временного бана
                /// <tags>ban, scenario, temporary, golden-master</tags>
                /// </summary>
                public static (User User, Chat Chat, Message Message, TimeSpan BanDuration, string Reason) TemporaryBanScenario()
                {
                    var user = TK.CreateUser(userId: 12345);
                    var chat = TK.CreateGroupChat();
                    var message = TK.CreateNewUserJoinMessage(user.Id);
                    message.Chat = chat;
                    
                    return (user, chat, message, TimeSpan.FromMinutes(10), "Длинное имя пользователя");
                }

                /// <summary>
                /// Сценарий для тестирования перманентного бана
                /// <tags>ban, scenario, permanent, golden-master</tags>
                /// </summary>
                public static (User User, Chat Chat, Message Message, TimeSpan? BanDuration, string Reason) PermanentBanScenario()
                {
                    var user = TK.CreateUser(userId: 67890);
                    var chat = TK.CreateGroupChat();
                    var message = TK.CreateNewUserJoinMessage(user.Id);
                    message.Chat = chat;
                    
                    return (user, chat, message, null, "Экстремально длинное имя пользователя");
                }

                /// <summary>
                /// Сценарий для тестирования попытки бана в приватном чате
                /// <tags>ban, scenario, private-chat, error-handling, golden-master</tags>
                /// </summary>
                public static (User User, Chat Chat, Message Message, TimeSpan BanDuration, string Reason) PrivateChatBanScenario()
                {
                    var user = TK.CreateUser(userId: 11111);
                    var chat = TK.CreatePrivateChat();
                    var message = TK.CreateNewUserJoinMessage(user.Id);
                    message.Chat = chat;
                    
                    return (user, chat, message, TimeSpan.FromMinutes(10), "Длинное имя пользователя");
                }

                /// <summary>
                /// Сценарий для тестирования бана пользователя из блэклиста
                /// <tags>ban, scenario, blacklist, golden-master</tags>
                /// </summary>
                public static (User User, Chat Chat, Message Message) BlacklistBanScenario()
                {
                    var user = TK.CreateUser(userId: 22222);
                    var chat = TK.CreateGroupChat();
                    var message = TK.CreateNewUserJoinMessage(user.Id);
                    message.Chat = chat;
                    
                    return (user, chat, message);
                }
            }

            /// <summary>
            /// Генераторы для чатов
            /// <tags>chat, domain-specific, test-scenarios</tags>
            /// </summary>
            public static class Chats
            {
                /// <summary>
                /// Чат для тестов банов
                /// <tags>chat, ban, integration</tags>
                /// </summary>
                public static Chat ForBanTest() => TestDataFactory.CreateGroupChat();
                
                /// <summary>
                /// Чат для тестов каналов (не announcement)
                /// <tags>chat, channel, integration, non-announcement</tags>
                /// </summary>
                public static Chat ForChannelTest() 
                {
                    var chat = TestDataFactory.CreateGroupChat();
                    chat.Id = -1009876543210; // Чат, который не настроен как announcement
                    return chat;
                }
            }
        }
    }
} 
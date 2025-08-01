using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Test.TestData;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.TestKit
{
    /// <summary>
    /// Основные методы TestKit для создания базовых тестовых объектов
    /// <tags>basic, generators, core, telegram</tags>
    /// </summary>
    public static partial class TestKit
    {
        

        #region Basic Objects

        /// <summary>
        /// Создает валидное сообщение
        /// <tags>message, valid, basic, telegram</tags>
        /// </summary>
        public static Message CreateValidMessage() => TestDataFactory.CreateValidMessage();

        /// <summary>
        /// Создает валидное сообщение с указанным ID
        /// <tags>message, valid, id, basic, telegram</tags>
        /// </summary>
        public static Message CreateValidMessageWithId(long messageId = 123) => TestDataFactory.CreateValidMessageWithId(messageId);

        /// <summary>
        /// Создает спам-сообщение
        /// <tags>message, spam, moderation, ml</tags>
        /// </summary>
        public static Message CreateSpamMessage() => TestDataFactory.CreateSpamMessage();

        /// <summary>
        /// Создает пустое сообщение
        /// <tags>message, empty, basic, telegram</tags>
        /// </summary>
        public static Message CreateEmptyMessage() => TestDataFactory.CreateEmptyMessage();

        /// <summary>
        /// Создает длинное сообщение
        /// <tags>message, long, basic, telegram</tags>
        /// </summary>
        public static Message CreateLongMessage() => TestDataFactory.CreateLongMessage();

        /// <summary>
        /// Создает сообщение с null текстом
        /// <tags>message, null-text, basic, telegram</tags>
        /// </summary>
        public static Message CreateNullTextMessage() => TestDataFactory.CreateNullTextMessage();

        /// <summary>
        /// Создает валидного пользователя
        /// <tags>user, valid, basic, telegram</tags>
        /// </summary>
        public static User CreateValidUser() => TestDataFactory.CreateValidUser();

        /// <summary>
        /// Создает бота-пользователя
        /// <tags>user, bot, basic, telegram</tags>
        /// </summary>
        public static User CreateBotUser() => TestDataFactory.CreateBotUser();

        /// <summary>
        /// Создает анонимного пользователя
        /// <tags>user, anonymous, basic, telegram</tags>
        /// </summary>
        public static User CreateAnonymousUser() => TestDataFactory.CreateAnonymousUser();

        /// <summary>
        /// Создает базовое сообщение (алиас к CreateValidMessage для удобства)
        /// <tags>message, basic, alias</tags>
        /// </summary>
        public static Message CreateMessage() => CreateValidMessage();

        /// <summary>
        /// Создает базового пользователя (алиас к CreateValidUser для удобства)
        /// <tags>user, basic, alias</tags>
        /// </summary>
        public static User CreateUser() => CreateValidUser();

        /// <summary>
        /// Создает групповой чат
        /// <tags>chat, group, basic, telegram</tags>
        /// </summary>
        public static Chat CreateGroupChat() => TestDataFactory.CreateGroupChat();

        /// <summary>
        /// Создает супергруппу
        /// <tags>chat, supergroup, basic, telegram</tags>
        /// </summary>
        public static Chat CreateSupergroupChat() => TestDataFactory.CreateSupergroupChat();

        /// <summary>
        /// Создает канал
        /// <tags>chat, channel, basic, telegram</tags>
        /// </summary>
        public static Chat CreateChannel() => ClubDoorman.Test.TestKit.TestKitBogus.CreateRealisticChannel();

        /// <summary>
        /// Создает приватный чат
        /// <tags>chat, private, basic, telegram</tags>
        /// </summary>
        public static Chat CreatePrivateChat() => TestDataFactory.CreatePrivateChat();

        /// <summary>
        /// Создает текстовое сообщение
        /// <tags>message, text, basic, telegram</tags>
        /// </summary>
        public static Message CreateTextMessage(long userId, long chatId, string text = "Test message") => TestDataFactory.CreateTextMessage(userId, chatId, text);

        /// <summary>
        /// Создает сообщение канала
        /// <tags>message, channel, basic, telegram</tags>
        /// </summary>
        public static Message CreateChannelMessage(long senderChatId, long chatId, string text = "Channel message") => TestDataFactory.CreateChannelMessage(senderChatId, chatId, text);

        /// <summary>
        /// Создает сообщение о присоединении нового пользователя
        /// <tags>message, new-user, join, member, telegram</tags>
        /// </summary>
        public static Message CreateNewUserJoinMessage(long userId = 12345) => TestDataFactory.CreateNewUserJoinMessage(userId);

        /// <summary>
        /// Создает сообщение от подозрительного пользователя
        /// <tags>message, suspicious, user, ai-analysis, telegram</tags>
        /// </summary>
        public static Message CreateSuspiciousUserMessage() => TestDataFactory.CreateSuspiciousUserMessage();

        /// <summary>
        /// Создает уведомление для админов
        /// <tags>message, admin, notification, moderation, telegram</tags>
        /// </summary>
        public static Message CreateAdminNotificationMessage() => TestDataFactory.CreateAdminNotificationMessage();

        /// <summary>
        /// Создает команду статистики
        /// <tags>message, admin, stats, command, telegram</tags>
        /// </summary>
        public static Message CreateStatsCommandMessage() => TestDataFactory.CreateStatsCommandMessage();

        /// <summary>
        /// Создает команду помощи
        /// <tags>message, admin, help, command, telegram</tags>
        /// </summary>
        public static Message CreateHelpCommandMessage() => TestDataFactory.CreateHelpCommandMessage();

        /// <summary>
        /// Создает валидный callback query
        /// <tags>callback, valid, interaction, telegram</tags>
        /// </summary>
        public static CallbackQuery CreateValidCallbackQuery() => TestDataFactory.CreateValidCallbackQuery();

        /// <summary>
        /// Создает невалидный callback query
        /// <tags>callback, invalid, interaction, telegram</tags>
        /// </summary>
        public static CallbackQuery CreateInvalidCallbackQuery() => TestDataFactory.CreateInvalidCallbackQuery();

        /// <summary>
        /// Создает callback для одобрения админом
        /// <tags>callback, admin, approve, moderation, telegram</tags>
        /// </summary>
        public static CallbackQuery CreateAdminApproveCallback() => TestDataFactory.CreateAdminApproveCallback();

        /// <summary>
        /// Создает callback для бана админом
        /// <tags>callback, admin, ban, moderation, telegram</tags>
        /// </summary>
        public static CallbackQuery CreateAdminBanCallback() => TestDataFactory.CreateAdminBanCallback();

        /// <summary>
        /// Создает callback для пропуска админом
        /// <tags>callback, admin, skip, moderation, telegram</tags>
        /// </summary>
        public static CallbackQuery CreateAdminSkipCallback() => TestDataFactory.CreateAdminSkipCallback();

        /// <summary>
        /// Создает update с сообщением
        /// <tags>update, message, basic, telegram</tags>
        /// </summary>
        public static Update CreateMessageUpdate() => TestDataFactory.CreateMessageUpdate();

        /// <summary>
        /// Создает update с callback query
        /// <tags>update, callback, interaction, telegram</tags>
        /// </summary>
        public static Update CreateCallbackQueryUpdate() => TestDataFactory.CreateCallbackQueryUpdate();

        /// <summary>
        /// Создает update с изменением участника чата
        /// <tags>update, chat-member, member-management, telegram</tags>
        /// </summary>
        public static Update CreateChatMemberUpdate() => TestDataFactory.CreateChatMemberUpdate();

        /// <summary>
        /// Создает приманку-капчу
        /// <tags>captcha, bait, user-verification, moderation</tags>
        /// </summary>
        public static CaptchaInfo CreateBaitCaptchaInfo() => TestDataFactory.CreateBaitCaptchaInfo();

        /// <summary>
        /// Создает результат модерации: разрешить
        /// <tags>moderation, allow, result, ml, ai</tags>
        /// </summary>
        public static ModerationResult CreateAllowResult() => TestDataFactory.CreateAllowResult();

        /// <summary>
        /// Создает результат модерации: удалить
        /// <tags>moderation, delete, result, ml, ai</tags>
        /// </summary>
        public static ModerationResult CreateDeleteResult() => TestDataFactory.CreateDeleteResult();

        /// <summary>
        /// Создает результат модерации: забанить
        /// <tags>moderation, ban, result, ml, ai</tags>
        /// </summary>
        public static ModerationResult CreateBanResult() => TestDataFactory.CreateBanResult();

        /// <summary>
        /// Создает валидную капчу
        /// <tags>captcha, valid, user-verification, moderation</tags>
        /// </summary>
        public static CaptchaInfo CreateValidCaptchaInfo() => TestDataFactory.CreateValidCaptchaInfo();

        /// <summary>
        /// Создает истекшую капчу
        /// <tags>captcha, expired, user-verification, moderation</tags>
        /// </summary>
        public static CaptchaInfo CreateExpiredCaptchaInfo() => TestDataFactory.CreateExpiredCaptchaInfo();

        /// <summary>
        /// Создает правильный результат капчи
        /// <tags>captcha, correct, result, user-verification</tags>
        /// </summary>
        public static bool CreateCorrectCaptchaResult() => TestDataFactory.CreateCorrectCaptchaResult();

        /// <summary>
        /// Создает неправильный результат капчи
        /// <tags>captcha, incorrect, result, user-verification</tags>
        /// </summary>
        public static bool CreateIncorrectCaptchaResult() => TestDataFactory.CreateIncorrectCaptchaResult();

        /// <summary>
        /// Создает пользователя-приманку
        /// <tags>user, bait, suspicious, ai-analysis</tags>
        /// </summary>
        public static User CreateBaitUser() => TestDataFactory.CreateBaitUser();

        #endregion

        #region Chat Members

        /// <summary>
        /// Создает событие присоединения участника
        /// <tags>chat-member, joined, member-management, telegram</tags>
        /// </summary>
        public static ChatMemberUpdated CreateMemberJoined() => TestDataFactory.CreateMemberJoined();

        /// <summary>
        /// Создает событие выхода участника
        /// <tags>chat-member, left, member-management, telegram</tags>
        /// </summary>
        public static ChatMemberUpdated CreateMemberLeft() => TestDataFactory.CreateMemberLeft();

        /// <summary>
        /// Создает событие бана участника
        /// <tags>chat-member, banned, member-management, ban, telegram</tags>
        /// </summary>
        public static ChatMemberUpdated CreateMemberBanned() => TestDataFactory.CreateMemberBanned();

        /// <summary>
        /// Создает событие ограничения участника
        /// <tags>chat-member, restricted, member-management, telegram</tags>
        /// </summary>
        public static ChatMemberUpdated CreateMemberRestricted() => TestDataFactory.CreateMemberRestricted();

        /// <summary>
        /// Создает событие повышения участника
        /// <tags>chat-member, promoted, member-management, telegram</tags>
        /// </summary>
        public static ChatMemberUpdated CreateMemberPromoted() => TestDataFactory.CreateMemberPromoted();

        /// <summary>
        /// Создает событие понижения участника
        /// <tags>chat-member, demoted, member-management, telegram</tags>
        /// </summary>
        public static ChatMemberUpdated CreateMemberDemoted() => TestDataFactory.CreateMemberDemoted();

        #endregion
    }
} 
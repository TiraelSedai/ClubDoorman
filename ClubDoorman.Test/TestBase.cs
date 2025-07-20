using ClubDoorman.Models;
using ClubDoorman.Test.TestData;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test;

/// <summary>
/// Базовый класс для всех тестов
/// Предоставляет общие утилиты и настройки
/// </summary>
public abstract class TestBase
{
    protected Mock<ILogger<T>> CreateLoggerMock<T>() where T : class
    {
        return new Mock<ILogger<T>>();
    }

    protected ILogger<T> CreateNullLogger<T>() where T : class
    {
        return NullLogger<T>.Instance;
    }

    /// <summary>
    /// Создает мок для любого интерфейса
    /// </summary>
    protected Mock<T> CreateMock<T>() where T : class
    {
        return new Mock<T>();
    }

    /// <summary>
    /// Проверяет, что исключение было выброшено
    /// </summary>
    protected static async Task AssertThrowsAsync<T>(Func<Task> action, string? message = null) where T : Exception
    {
        var exception = Assert.ThrowsAsync<T>(action);
        if (!string.IsNullOrEmpty(message))
        {
            Assert.That(exception.Message, Does.Contain(message));
        }
    }

    /// <summary>
    /// Проверяет, что исключение было выброшено (синхронная версия)
    /// </summary>
    protected static void AssertThrows<T>(Action action, string? message = null) where T : Exception
    {
        var exception = Assert.Throws<T>(action);
        if (!string.IsNullOrEmpty(message))
        {
            Assert.That(exception.Message, Does.Contain(message));
        }
    }

    /// <summary>
    /// Проверяет, что исключение было выброшено (синхронная версия)
    /// </summary>
    protected static void AssertThrows<T>(TestDelegate action, string? message = null) where T : Exception
    {
        var exception = Assert.Throws<T>(action);
        if (!string.IsNullOrEmpty(message))
        {
            Assert.That(exception.Message, Does.Contain(message));
        }
    }

    /// <summary>
    /// Проверяет, что исключение было выброшено (асинхронная версия)
    /// </summary>
    protected static async Task AssertThrowsAsync<T>(AsyncTestDelegate action, string? message = null) where T : Exception
    {
        var exception = Assert.ThrowsAsync<T>(action);
        if (!string.IsNullOrEmpty(message))
        {
            Assert.That(exception.Message, Does.Contain(message));
        }
    }

    /// <summary>
    /// Доступ к фабрике тестовых данных
    /// </summary>
    protected static class TestData
    {
        public static class Messages
        {
            public static Message Valid() => TestDataFactory.CreateValidMessage();
            public static Message Spam() => TestDataFactory.CreateSpamMessage();
            public static Message Empty() => TestDataFactory.CreateEmptyMessage();
            public static Message NullText() => TestDataFactory.CreateNullTextMessage();
            public static Message Long() => TestDataFactory.CreateLongMessage();
        }

        public static class Users
        {
            public static User Valid() => TestDataFactory.CreateValidUser();
            public static User Bot() => TestDataFactory.CreateBotUser();
            public static User Anonymous() => TestDataFactory.CreateAnonymousUser();
        }

        public static class Chats
        {
            public static Chat Group() => TestDataFactory.CreateGroupChat();
            public static Chat Supergroup() => TestDataFactory.CreateSupergroupChat();
            public static Chat Private() => TestDataFactory.CreatePrivateChat();
        }

        public static class CallbackQueries
        {
            public static CallbackQuery Valid() => TestDataFactory.CreateValidCallbackQuery();
            public static CallbackQuery Invalid() => TestDataFactory.CreateInvalidCallbackQuery();
        }

        public static class ChatMembers
        {
            public static ChatMemberUpdated Joined() => TestDataFactory.CreateMemberJoined();
            public static ChatMemberUpdated Left() => TestDataFactory.CreateMemberLeft();
            public static ChatMemberUpdated Banned() => TestDataFactory.CreateMemberBanned();
            public static ChatMemberUpdated Restricted() => TestDataFactory.CreateMemberRestricted();
            public static ChatMemberUpdated Promoted() => TestDataFactory.CreateMemberPromoted();
            public static ChatMemberUpdated Demoted() => TestDataFactory.CreateMemberDemoted();
        }

        public static class Updates
        {
            public static Update Message() => TestDataFactory.CreateMessageUpdate();
            public static Update CallbackQuery() => TestDataFactory.CreateCallbackQueryUpdate();
            public static Update ChatMember() => TestDataFactory.CreateChatMemberUpdate();
        }

        public static class ModerationResults
        {
            public static ModerationResult Allow() => TestDataFactory.CreateAllowResult();
            public static ModerationResult Delete() => TestDataFactory.CreateDeleteResult();
            public static ModerationResult Ban() => TestDataFactory.CreateBanResult();
        }

        public static class CaptchaInfo
        {
            public static Models.CaptchaInfo Valid() => TestDataFactory.CreateValidCaptchaInfo();
            public static Models.CaptchaInfo Expired() => TestDataFactory.CreateExpiredCaptchaInfo();
        }
    }
} 
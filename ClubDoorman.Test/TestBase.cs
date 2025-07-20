using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using ClubDoorman.Test.TestData;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Models;

namespace ClubDoorman.Test;

/// <summary>
/// Базовый класс для всех тестов
/// Следует принципам TDD: тестируем поведение, а не реализацию
/// </summary>
[TestFixture]
public abstract class TestBase
{
    protected ILogger<TestBase> Logger { get; private set; } = null!;

    [SetUp]
    public virtual void SetUp()
    {
        // Используем NullLogger для тестов - не тестируем логирование
        Logger = new NullLogger<TestBase>();
    }

    [TearDown]
    public virtual void TearDown()
    {
        // Очистка ресурсов если необходимо
    }

    /// <summary>
    /// Создает мок с базовой настройкой
    /// </summary>
    protected static Mock<T> CreateMock<T>() where T : class
    {
        return new Mock<T>(MockBehavior.Strict);
    }

    /// <summary>
    /// Создает мок с базовой настройкой и ленивой инициализацией
    /// </summary>
    protected static Mock<T> CreateLazyMock<T>() where T : class
    {
        return new Mock<T>(MockBehavior.Loose);
    }

    /// <summary>
    /// Проверяет, что исключение было выброшено
    /// </summary>
    protected static async Task AssertThrowsAsync<TException>(Func<Task> action, string? message = null) 
        where TException : Exception
    {
        var exception = await Assert.ThrowsAsync<TException>(action);
        
        if (!string.IsNullOrEmpty(message))
        {
            Assert.That(exception.Message, Contains.Substring(message));
        }
    }

    /// <summary>
    /// Проверяет, что исключение было выброшено синхронно
    /// </summary>
    protected static void AssertThrows<TException>(Action action, string? message = null) 
        where TException : Exception
    {
        var exception = Assert.Throws<TException>(action);
        
        if (!string.IsNullOrEmpty(message))
        {
            Assert.That(exception.Message, Contains.Substring(message));
        }
    }

    /// <summary>
    /// Проверяет, что действие не выбрасывает исключений
    /// </summary>
    protected static void AssertDoesNotThrow(Action action)
    {
        Assert.DoesNotThrow(action);
    }

    /// <summary>
    /// Проверяет, что асинхронное действие не выбрасывает исключений
    /// </summary>
    protected static async Task AssertDoesNotThrowAsync(Func<Task> action)
    {
        await Assert.DoesNotThrowAsync(action);
    }

    /// <summary>
    /// Проверяет, что значение находится в диапазоне
    /// </summary>
    protected static void AssertInRange<T>(T value, T min, T max) where T : IComparable<T>
    {
        Assert.That(value, Is.GreaterThanOrEqualTo(min));
        Assert.That(value, Is.LessThanOrEqualTo(max));
    }

    /// <summary>
    /// Проверяет, что время выполнения находится в допустимых пределах
    /// </summary>
    protected static async Task AssertExecutionTimeAsync(Func<Task> action, TimeSpan maxTime)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await action();
        stopwatch.Stop();

        Assert.That(stopwatch.Elapsed, Is.LessThan(maxTime), 
            $"Execution time {stopwatch.Elapsed} exceeded maximum {maxTime}");
    }

    /// <summary>
    /// Проверяет, что время выполнения находится в допустимых пределах (синхронно)
    /// </summary>
    protected static void AssertExecutionTime(Action action, TimeSpan maxTime)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        action();
        stopwatch.Stop();

        Assert.That(stopwatch.Elapsed, Is.LessThan(maxTime), 
            $"Execution time {stopwatch.Elapsed} exceeded maximum {maxTime}");
    }

    /// <summary>
    /// Создает тестовые данные для сообщений
    /// </summary>
    protected static class TestMessages 
    { 
        public static Message ValidMessage() => TestDataFactory.Messages.ValidMessage();
        public static Message SpamMessage() => TestDataFactory.Messages.SpamMessage();
        public static Message MimicryMessage() => TestDataFactory.Messages.MimicryMessage();
        public static Message EmptyMessage() => TestDataFactory.Messages.EmptyMessage();
        public static Message LongMessage() => TestDataFactory.Messages.LongMessage();
        public static Message MessageWithSpecialCharacters() => TestDataFactory.Messages.MessageWithSpecialCharacters();
    }

    /// <summary>
    /// Создает тестовые данные для пользователей
    /// </summary>
    protected static class TestUsers 
    { 
        public static User ApprovedUser() => TestDataFactory.Users.ApprovedUser();
        public static User NewUser() => TestDataFactory.Users.NewUser();
        public static User SuspiciousUser() => TestDataFactory.Users.SuspiciousUser();
        public static User BotUser() => TestDataFactory.Users.BotUser();
        public static User UserWithoutUsername() => TestDataFactory.Users.UserWithoutUsername();
    }

    /// <summary>
    /// Создает тестовые данные для чатов
    /// </summary>
    protected static class TestChats 
    { 
        public static Chat MainChat() => TestDataFactory.Chats.MainChat();
        public static Chat GroupChat() => TestDataFactory.Chats.GroupChat();
        public static Chat SupergroupChat() => TestDataFactory.Chats.SupergroupChat();
        public static Chat ChannelChat() => TestDataFactory.Chats.ChannelChat();
    }

    /// <summary>
    /// Создает тестовые данные для callback запросов
    /// </summary>
    protected static class TestCallbackQueries 
    { 
        public static CallbackQuery ValidCallbackQuery() => TestDataFactory.CallbackQueries.ValidCallbackQuery();
        public static CallbackQuery CaptchaCallbackQuery() => TestDataFactory.CallbackQueries.CaptchaCallbackQuery();
        public static CallbackQuery InvalidCallbackQuery() => TestDataFactory.CallbackQueries.InvalidCallbackQuery();
    }

    /// <summary>
    /// Создает тестовые данные для участников чата
    /// </summary>
    protected static class TestChatMembers 
    { 
        public static ChatMemberUpdated MemberJoined() => TestDataFactory.ChatMembers.MemberJoined();
        public static ChatMemberUpdated MemberLeft() => TestDataFactory.ChatMembers.MemberLeft();
        public static ChatMemberUpdated AdminPromoted() => TestDataFactory.ChatMembers.AdminPromoted();
    }

    /// <summary>
    /// Создает тестовые данные для обновлений
    /// </summary>
    protected static class TestUpdates 
    { 
        public static Update MessageUpdate() => TestDataFactory.Updates.MessageUpdate();
        public static Update CallbackQueryUpdate() => TestDataFactory.Updates.CallbackQueryUpdate();
        public static Update ChatMemberUpdate() => TestDataFactory.Updates.ChatMemberUpdate();
        public static Update InvalidUpdate() => TestDataFactory.Updates.InvalidUpdate();
    }

    /// <summary>
    /// Создает тестовые данные для результатов модерации
    /// </summary>
    protected static class TestModerationResults 
    { 
        public static ModerationResult AllowResult() => TestDataFactory.ModerationResults.AllowResult();
        public static ModerationResult BlockResult() => TestDataFactory.ModerationResults.BlockResult();
        public static ModerationResult WarnResult() => TestDataFactory.ModerationResults.WarnResult();
        public static ModerationResult CaptchaResult() => TestDataFactory.ModerationResults.CaptchaResult();
    }

    /// <summary>
    /// Создает тестовые данные для капчи
    /// </summary>
    protected static class TestCaptchaInfo 
    { 
        public static Models.CaptchaInfo ValidCaptcha() => TestDataFactory.CaptchaInfo.ValidCaptcha();
        public static Models.CaptchaInfo ExpiredCaptcha() => TestDataFactory.CaptchaInfo.ExpiredCaptcha();
        public static Models.CaptchaInfo CaptchaWithAttempts() => TestDataFactory.CaptchaInfo.CaptchaWithAttempts();
    }
} 
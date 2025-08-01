using AutoFixture;
using AutoFixture.AutoMoq;
using ClubDoorman.Services;
using ClubDoorman.Models;
using ClubDoorman.Handlers;
using ClubDoorman.Infrastructure;
using ClubDoorman.Test.TestKit.Infra;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClubDoorman.Test.TestKit;

/// <summary>
/// Расширение TestKit с AutoFixture для автоматического создания тестовых объектов
/// <tags>autofixture, auto-generation, dependencies, test-infrastructure</tags>
/// </summary>
public static class TestKitAutoFixture
{
    /// <summary>
    /// Создает настроенный AutoFixture с кастомизациями для Telegram-типов
    /// <tags>autofixture, customization, telegram, test-infrastructure</tags>
    /// </summary>
    public static IFixture CreateFixture() => Infra.TestKitAutoFixture.CreateFixture();

    /// <summary>
    /// Получает глобальный экземпляр AutoFixture
    /// <tags>autofixture, global, singleton, test-infrastructure</tags>
    /// </summary>
    public static IFixture GetFixture() => Infra.TestKitAutoFixture.GetFixture();

    /// <summary>
    /// Создает объект любого типа с автоматической генерацией зависимостей
    /// <tags>autofixture, auto-generation, generic, test-infrastructure</tags>
    /// </summary>
    public static T Create<T>() => Infra.TestKitAutoFixture.Create<T>();

    /// <summary>
    /// Создает коллекцию объектов указанного типа
    /// <tags>autofixture, create-many, collection, test-infrastructure</tags>
    /// </summary>
    public static IEnumerable<T> CreateMany<T>(int count = 3) => Infra.TestKitAutoFixture.CreateMany<T>(count);

    /// <summary>
    /// Заполняет свойства существующего объекта
    /// <tags>autofixture, populate, fill-properties, test-infrastructure</tags>
    /// </summary>
    public static T Populate<T>(T item) where T : class => Infra.TestKitAutoFixture.Populate(item);

    /// <summary>
    /// Создает MessageHandler с автозависимостями
    /// <tags>autofixture, message-handler, dependencies, test-infrastructure</tags>
    /// </summary>
    public static MessageHandler CreateMessageHandler() => Infra.TestKitAutoFixture.CreateMessageHandler();

    /// <summary>
    /// Создает ModerationService с автозависимостями
    /// <tags>autofixture, moderation-service, dependencies, test-infrastructure</tags>
    /// </summary>
    public static ModerationService CreateModerationService() => Infra.TestKitAutoFixture.CreateModerationService();

    /// <summary>
    /// Создает список реалистичных пользователей
    /// <tags>autofixture, users, realistic, test-infrastructure</tags>
    /// </summary>
    public static List<Telegram.Bot.Types.User> CreateRealisticUsers(int count = 3) => Infra.TestKitAutoFixture.CreateRealisticUsers(count);

    /// <summary>
    /// Создает список реалистичных сообщений
    /// <tags>autofixture, messages, realistic, test-infrastructure</tags>
    /// </summary>
    public static List<Telegram.Bot.Types.Message> CreateRealisticMessages(int count = 3) => Infra.TestKitAutoFixture.CreateRealisticMessages(count);

    /// <summary>
    /// Создает тестовый сценарий с пользователем, чатом и сообщением
    /// <tags>autofixture, scenario, user-chat-message, test-infrastructure</tags>
    /// </summary>
    public static (Telegram.Bot.Types.User User, Telegram.Bot.Types.Chat Chat, Telegram.Bot.Types.Message Message) CreateTestScenario() => 
        Infra.TestKitAutoFixture.CreateTestScenario();

    /// <summary>
    /// Создает произвольное количество объектов (от 1 до 5)
    /// <tags>autofixture, random-count, flexible, test-infrastructure</tags>
    /// </summary>
    public static IEnumerable<T> CreateSome<T>() => Infra.TestKitAutoFixture.CreateSome<T>();

    /// <summary>
    /// Создает объект с переопределенными свойствами
    /// <tags>autofixture, override, customization, test-infrastructure</tags>
    /// </summary>
    public static T CreateWith<T>(Action<T> customization) where T : class => Infra.TestKitAutoFixture.CreateWith(customization);

    /// <summary>
    /// Замораживает фиксированные значения для воспроизводимых тестов
    /// <tags>autofixture, freeze, reproducible, test-infrastructure</tags>
    /// </summary>
    public static T Freeze<T>() => Infra.TestKitAutoFixture.Freeze<T>();

    /// <summary>
    /// Регистрирует кастомную фабрику для типа
    /// <tags>autofixture, customize, factory, test-infrastructure</tags>
    /// </summary>
    public static void CustomizeWith<T>(Func<T> factory) => Infra.TestKitAutoFixture.CustomizeWith(factory);

    /// <summary>
    /// Инжектирует конкретный экземпляр для типа
    /// <tags>autofixture, inject, singleton, test-infrastructure</tags>
    /// </summary>
    public static void Inject<T>(T instance) => Infra.TestKitAutoFixture.Inject(instance);
    
    #region Backward Compatibility Methods
    
    /// <summary>
    /// Создает CaptchaService с автозависимостями
    /// <tags>autofixture, captcha-service, dependencies, test-infrastructure</tags>
    /// </summary>
    public static ICaptchaService CreateCaptchaService() => Infra.TestKitAutoFixture.CreateCaptchaService();
    
    /// <summary>
    /// Создает UserManager с автозависимостями
    /// <tags>autofixture, user-manager, dependencies, test-infrastructure</tags>
    /// </summary>
    public static IUserManager CreateUserManager() => Infra.TestKitAutoFixture.CreateUserManager();
    
    /// <summary>
    /// Создает Update объект
    /// <tags>autofixture, update, telegram, test-infrastructure</tags>
    /// </summary>
    public static Telegram.Bot.Types.Update CreateUpdate() => Infra.TestKitAutoFixture.CreateUpdate();
    
    /// <summary>
    /// Создает MessageUpdate
    /// <tags>autofixture, message-update, telegram, test-infrastructure</tags>
    /// </summary>
    public static Telegram.Bot.Types.Update CreateMessageUpdate() => Infra.TestKitAutoFixture.CreateMessageUpdate();
    
    /// <summary>
    /// Создает CallbackQueryUpdate
    /// <tags>autofixture, callback-query-update, telegram, test-infrastructure</tags>
    /// </summary>
    public static Telegram.Bot.Types.Update CreateCallbackQueryUpdate() => Infra.TestKitAutoFixture.CreateCallbackQueryUpdate();
    
    /// <summary>
    /// Создает много сообщений
    /// <tags>autofixture, many-messages, collection, test-infrastructure</tags>
    /// </summary>
    public static List<Telegram.Bot.Types.Message> CreateManyMessages(int count = 3) => Infra.TestKitAutoFixture.CreateManyMessages(count);
    
    /// <summary>
    /// Создает много пользователей
    /// <tags>autofixture, many-users, collection, test-infrastructure</tags>
    /// </summary>
    public static List<Telegram.Bot.Types.User> CreateManyUsers(int count = 3) => Infra.TestKitAutoFixture.CreateManyUsers(count);
    
    /// <summary>
    /// Создает много спам-сообщений
    /// <tags>autofixture, many-spam-messages, collection, test-infrastructure</tags>
    /// </summary>
    public static List<Telegram.Bot.Types.Message> CreateManySpamMessages(int count = 3) => Infra.TestKitAutoFixture.CreateManySpamMessages(count);
    
    /// <summary>
    /// Создает объект с фикстурой для кастомизации
    /// <tags>autofixture, customization, fixture, test-infrastructure</tags>
    /// </summary>
    public static (T sut, IFixture fixture) CreateWithFixture<T>() => Infra.TestKitAutoFixture.CreateWithFixture<T>();

    #endregion
} 
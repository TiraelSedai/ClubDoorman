using AutoFixture;
using AutoFixture.AutoMoq;
using ClubDoorman.Services;
using ClubDoorman.Models;
using ClubDoorman.Handlers;
using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClubDoorman.Test.TestKit;

/// <summary>
/// Расширение TestKit с AutoFixture для автоматического создания тестовых объектов
/// <tags>autofixture, auto-generation, dependencies, test-infrastructure</tags>
/// </summary>
public static class TestKitAutoFixture
{
    private static readonly IFixture _fixture = CreateFixture();

    /// <summary>
    /// Создает настроенный AutoFixture с кастомизациями для Telegram-типов
    /// <tags>autofixture, customization, telegram, test-infrastructure</tags>
    /// </summary>
    public static IFixture CreateFixture()
    {
        var fixture = new Fixture()
            .Customize(new AutoMoqCustomization { ConfigureMembers = true });

        // Кастомизации для Telegram-типов
        fixture.Customize<Telegram.Bot.Types.User>(composer => composer
            .FromFactory(() => TestKitBogus.CreateRealisticUser())
            .OmitAutoProperties());

        fixture.Customize<Telegram.Bot.Types.Chat>(composer => composer
            .FromFactory(() => TestKitBogus.CreateRealisticGroup())
            .OmitAutoProperties());

        fixture.Customize<Telegram.Bot.Types.Message>(composer => composer
            .FromFactory(() => TestKitBogus.CreateRealisticMessage())
            .OmitAutoProperties());

        // Кастомизации для наших сервисов
        fixture.Customize<ILogger<MessageHandler>>(composer => composer
            .FromFactory(() => NullLogger<MessageHandler>.Instance));

        fixture.Customize<ILogger<ModerationService>>(composer => composer
            .FromFactory(() => NullLogger<ModerationService>.Instance));

        fixture.Customize<ILogger<CaptchaService>>(composer => composer
            .FromFactory(() => NullLogger<CaptchaService>.Instance));

        // Кастомизации для конфигурации
        fixture.Customize<IAppConfig>(composer => composer
            .FromFactory(() => CreateTestAppConfig()));

        // Умные кастомизации для моков сервисов
        fixture.Customize<IModerationService>(composer => composer
            .FromFactory(() => CreateSmartModerationServiceMock()));

        return fixture;
    }

    /// <summary>
    /// Создает объект любого типа с автоматической генерацией зависимостей
    /// <tags>autofixture, auto-generation, generic, test-infrastructure</tags>
    /// </summary>
    public static T Create<T>()
    {
        return _fixture.Create<T>();
    }

    /// <summary>
    /// Создает объект с замоканными зависимостями
    /// <tags>autofixture, mocks, dependencies, test-infrastructure</tags>
    /// </summary>
    public static T CreateWithMocks<T>() where T : class
    {
        return _fixture.Create<T>();
    }

    /// <summary>
    /// Создает объект и возвращает его вместе с fixture для дальнейшей настройки
    /// <tags>autofixture, customization, fixture, test-infrastructure</tags>
    /// </summary>
    public static (T sut, IFixture fixture) CreateWithFixture<T>()
    {
        var fixture = CreateFixture();
        return (fixture.Create<T>(), fixture);
    }

    /// <summary>
    /// Создает MessageHandler с автоматически сгенерированными зависимостями
    /// <tags>autofixture, message-handler, auto-generation, test-infrastructure</tags>
    /// </summary>
    public static MessageHandler CreateMessageHandler()
    {
        return _fixture.Create<MessageHandler>();
    }

    /// <summary>
    /// Создает ModerationService с автоматически сгенерированными зависимостями
    /// <tags>autofixture, moderation-service, auto-generation, test-infrastructure</tags>
    /// </summary>
    public static ModerationService CreateModerationService()
    {
        return _fixture.Create<ModerationService>();
    }

    /// <summary>
    /// Создает CaptchaService с автоматически сгенерированными зависимостями
    /// </summary>
    public static CaptchaService CreateCaptchaService()
    {
        return _fixture.Create<CaptchaService>();
    }

    /// <summary>
    /// Создает UserManager с автоматически сгенерированными зависимостями
    /// </summary>
    public static IUserManager CreateUserManager()
    {
        return _fixture.Create<IUserManager>();
    }

    /// <summary>
    /// Создает Update с автоматически сгенерированным сообщением
    /// </summary>
    public static Telegram.Bot.Types.Update CreateUpdate()
    {
        return _fixture.Create<Telegram.Bot.Types.Update>();
    }

    /// <summary>
    /// Создает Update с сообщением
    /// </summary>
    public static Telegram.Bot.Types.Update CreateMessageUpdate()
    {
        var update = _fixture.Create<Telegram.Bot.Types.Update>();
        update.Message = TestKitBogus.CreateRealisticMessage();
        return update;
    }

    /// <summary>
    /// Создает Update с CallbackQuery
    /// </summary>
    public static Telegram.Bot.Types.Update CreateCallbackQueryUpdate()
    {
        var update = _fixture.Create<Telegram.Bot.Types.Update>();
        update.CallbackQuery = TestKit.CreateValidCallbackQuery();
        return update;
    }

    /// <summary>
    /// Создает коллекцию объектов
    /// </summary>
    public static IEnumerable<T> CreateMany<T>(int count = 3)
    {
        return _fixture.CreateMany<T>(count);
    }

    /// <summary>
    /// Создает коллекцию сообщений
    /// </summary>
    public static IEnumerable<Telegram.Bot.Types.Message> CreateManyMessages(int count = 3)
    {
        return Enumerable.Range(0, count)
            .Select(_ => TestKitBogus.CreateRealisticMessage())
            .ToList();
    }

    /// <summary>
    /// Создает коллекцию пользователей
    /// </summary>
    public static IEnumerable<Telegram.Bot.Types.User> CreateManyUsers(int count = 3)
    {
        return Enumerable.Range(0, count)
            .Select(_ => TestKitBogus.CreateRealisticUser())
            .ToList();
    }

    /// <summary>
    /// Создает коллекцию спам-сообщений
    /// </summary>
    public static IEnumerable<Telegram.Bot.Types.Message> CreateManySpamMessages(int count = 3)
    {
        return Enumerable.Range(0, count)
            .Select(_ => TestKitBogus.CreateRealisticSpamMessage())
            .ToList();
    }

    #region Helper Methods

    private static IAppConfig CreateTestAppConfig()
    {
        var mock = new Moq.Mock<IAppConfig>();
        mock.Setup(x => x.NoCaptchaGroups).Returns(new HashSet<long>());
        mock.Setup(x => x.NoVpnAdGroups).Returns(new HashSet<long>());
        mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
        mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
        mock.Setup(x => x.AdminChatId).Returns(123456789);
        mock.Setup(x => x.LogAdminChatId).Returns(987654321);
        return mock.Object;
    }

    #endregion

    #region Smart Mock Factories

    /// <summary>
    /// Создает умный мок IModerationService с разумными настройками по умолчанию
    /// </summary>
    private static IModerationService CreateSmartModerationServiceMock()
    {
        var mock = new Moq.Mock<IModerationService>();
        
        // По умолчанию разрешаем сообщения
        mock.Setup(x => x.CheckMessageAsync(It.IsAny<Telegram.Bot.Types.Message>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid message"));
        
        // Умная логика для спам-сообщений
        mock.Setup(x => x.CheckMessageAsync(It.Is<Telegram.Bot.Types.Message>(m => 
            m.Text != null && TestKitBogus.IsSpamText(m.Text))))
            .ReturnsAsync(new ModerationResult(ModerationAction.Delete, "Spam detected"));
        
        // Умная логика для пустых сообщений
        mock.Setup(x => x.CheckMessageAsync(It.Is<Telegram.Bot.Types.Message>(m => 
            string.IsNullOrEmpty(m.Text))))
            .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Empty message allowed"));
        
        return mock.Object;
    }



    #endregion
} 
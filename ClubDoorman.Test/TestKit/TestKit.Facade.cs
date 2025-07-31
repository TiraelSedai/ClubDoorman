using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Test.TestData;
using ClubDoorman.Handlers;
using Telegram.Bot.Types;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;

namespace ClubDoorman.Test.TestKit
{
    /// <summary>
    /// Тонкий фасад TestKit - основной интерфейс для всех тестовых утилит
    /// <tags>facade, factory, test-infrastructure, unified-interface</tags>
    /// </summary>
    public static partial class TestKit
    {
        #region Factory Methods

        /// <summary>
        /// Создает фабрику для MessageHandler
        /// <tags>factory, message-handler, test-infrastructure</tags>
        /// </summary>
        public static MessageHandlerTestFactory CreateMessageHandlerFactory() => new();

        /// <summary>
        /// Создает билдер для MessageHandler
        /// <tags>builder, message-handler, test-infrastructure, fluent-api</tags>
        /// </summary>
        public static MessageHandlerBuilder CreateMessageHandlerBuilder() => new();

        /// <summary>
        /// Создает фабрику для ModerationService
        /// <tags>factory, moderation-service, test-infrastructure</tags>
        /// </summary>
        public static ModerationServiceTestFactory CreateModerationServiceFactory() => new();

        /// <summary>
        /// Создает фабрику для CaptchaService
        /// <tags>factory, captcha-service, test-infrastructure</tags>
        /// </summary>
        public static CaptchaServiceTestFactory CreateCaptchaServiceFactory() => new();

        /// <summary>
        /// Создает фабрику для CallbackQueryHandler
        /// <tags>factory, callback-query-handler, test-infrastructure</tags>
        /// </summary>
        public static CallbackQueryHandlerTestFactory CreateCallbackQueryHandlerFactory() => new();

        /// <summary>
        /// Создает фабрику для ChatMemberHandler
        /// <tags>factory, chat-member-handler, test-infrastructure</tags>
        /// </summary>
        public static ChatMemberHandlerTestFactory CreateChatMemberHandlerFactory() => new();

        /// <summary>
        /// Создает фабрику для ServiceChatDispatcher
        /// <tags>factory, service-chat-dispatcher, test-infrastructure</tags>
        /// </summary>
        public static ServiceChatDispatcherTestFactory CreateServiceChatDispatcherFactory() => new();

        /// <summary>
        /// Создает базовую конфигурацию приложения
        /// <tags>config, app-config, test-infrastructure</tags>
        /// </summary>
        public static IAppConfig CreateAppConfig() => AppConfigTestFactory.CreateDefault();

        /// <summary>
        /// Создает конфигурацию приложения без AI
        /// <tags>config, app-config, no-ai, test-infrastructure</tags>
        /// </summary>
        public static IAppConfig CreateAppConfigWithoutAi() => AppConfigTestFactory.CreateWithoutAi();

        /// <summary>
        /// Создает фабрику для AiChecks
        /// <tags>factory, ai-checks, ai, test-infrastructure</tags>
        /// </summary>
        public static AiChecksTestFactory CreateAiChecksFactory() => new();

        /// <summary>
        /// Создает фабрику для StatisticsService
        /// <tags>factory, statistics-service, test-infrastructure</tags>
        /// </summary>
        public static StatisticsServiceTestFactory CreateStatisticsServiceFactory() => new();

        /// <summary>
        /// Создает фабрику для SpamHamClassifier
        /// <tags>factory, spam-ham-classifier, ml, test-infrastructure</tags>
        /// </summary>
        public static SpamHamClassifierTestFactory CreateSpamHamClassifierFactory() => new();

        /// <summary>
        /// Создает фабрику для MimicryClassifier
        /// <tags>factory, mimicry-classifier, ml, test-infrastructure</tags>
        /// </summary>
        public static MimicryClassifierTestFactory CreateMimicryClassifierFactory() => new();

        #endregion

        #region Mock Builder Methods

        /// <summary>
        /// Создает билдер для мока IModerationService
        /// <tags>builder, moderation-service, mocks, fluent-api</tags>
        /// </summary>
        public static ModerationServiceMockBuilder CreateModerationServiceMock() => TestKitMockBuilders.CreateModerationServiceMock();
        
        /// <summary>
        /// Создает билдер для мока IUserManager
        /// <tags>builder, user-manager, mocks, fluent-api</tags>
        /// </summary>
        public static UserManagerMockBuilder CreateUserManagerMock() => TestKitMockBuilders.CreateUserManagerMock();
        
        /// <summary>
        /// Создает билдер для мока ICaptchaService
        /// <tags>builder, captcha-service, mocks, fluent-api</tags>
        /// </summary>
        public static CaptchaServiceMockBuilder CreateCaptchaServiceMock() => TestKitMockBuilders.CreateCaptchaServiceMock();
        
        /// <summary>
        /// Создает билдер для мока IAiChecks
        /// <tags>builder, ai-checks, mocks, fluent-api</tags>
        /// </summary>
        public static AiChecksMockBuilder CreateAiChecksMock() => TestKitMockBuilders.CreateAiChecksMock();
        
        /// <summary>
        /// Создает билдер для мока ITelegramBotClientWrapper
        /// <tags>builder, telegram-bot, mocks, fluent-api</tags>
        /// </summary>
        public static TelegramBotMockBuilder CreateTelegramBotMock() => TestKitMockBuilders.CreateTelegramBotMock();

        #endregion

        #region AutoFixture Methods

        /// <summary>
        /// Создает экземпляр типа T с автоматически сгенерированными зависимостями
        /// <tags>autofixture, auto-generation, dependencies, test-infrastructure</tags>
        /// </summary>
        public static T Create<T>() => TestKitAutoFixture.Create<T>();

        /// <summary>
        /// Создает коллекцию экземпляров типа T
        /// <tags>autofixture, collection, auto-generation, test-infrastructure</tags>
        /// </summary>
        public static IEnumerable<T> CreateMany<T>(int count = 3) => TestKitAutoFixture.CreateMany<T>(count);

        /// <summary>
        /// Создает экземпляр типа T с фикстурой для кастомизации
        /// <tags>autofixture, customization, fixture, test-infrastructure</tags>
        /// </summary>
        public static (T sut, IFixture fixture) CreateWithFixture<T>()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var sut = fixture.Create<T>();
            return (sut, fixture);
        }

        #endregion

            #region Bogus Methods

    /// <summary>
    /// Создает пользователя с помощью Bogus
    /// </summary>
    public static Telegram.Bot.Types.User CreateUser(long? userId = null) => 
        TestKitBogus.CreateRealisticUser(userId);

    /// <summary>
    /// Создает сообщение с помощью Bogus
    /// </summary>
    public static Telegram.Bot.Types.Message CreateMessage() => 
        TestKitBogus.CreateRealisticMessage();

    /// <summary>
    /// Создает спам-сообщение с помощью Bogus
    /// </summary>
    public static Telegram.Bot.Types.Message CreateBogusSpamMessage() => 
        TestKitBogus.CreateRealisticSpamMessage();

    #endregion

        #region Convenience Methods

        /// <summary>
        /// Создает MessageHandler с базовой настройкой для тестов
        /// </summary>
        public static MessageHandler CreateMessageHandlerWithDefaults()
        {
            var factory = CreateMessageHandlerFactory();
            
            // Базовая настройка AppConfig
            factory.WithAppConfigSetup(mock => 
            {
                mock.Setup(x => x.NoCaptchaGroups).Returns(new HashSet<long>());
                mock.Setup(x => x.NoVpnAdGroups).Returns(new HashSet<long>());
                mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
                mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
                mock.Setup(x => x.AdminChatId).Returns(123456789);
                mock.Setup(x => x.LogAdminChatId).Returns(987654321);
            });

            return factory.CreateMessageHandler();
        }

        /// <summary>
        /// Создает MessageHandler с FakeTelegramClient
        /// </summary>
        public static MessageHandler CreateMessageHandlerWithFake(FakeTelegramClient? fakeClient = null)
        {
            var factory = CreateMessageHandlerFactory();
            return factory.CreateMessageHandlerWithFake(fakeClient ?? new FakeTelegramClient());
        }

        #endregion

        #region Telegram Integration Methods

        /// <summary>
        /// Создает FakeTelegramClient для тестов
        /// </summary>
        public static FakeTelegramClient CreateFakeClient() => TestKitTelegram.CreateFakeClient();
        
        /// <summary>
        /// Создает MessageEnvelope с автоматическим MessageId
        /// </summary>
        public static MessageEnvelope CreateEnvelope(
            long userId = 12345,
            long chatId = 67890,
            string text = "Test message") => TestKitTelegram.CreateEnvelope(userId, chatId, text);
        
        /// <summary>
        /// Создает спам-сценарий с FakeTelegramClient
        /// </summary>
        public static (FakeTelegramClient fakeClient, MessageEnvelope envelope, Message message, Update update) CreateSpamScenario(
            long userId = 12345,
            long chatId = 67890) => TestKitTelegram.CreateSpamScenario(userId, chatId);
        
        /// <summary>
        /// Создает полный тестовый сценарий с FakeTelegramClient
        /// </summary>
        public static (FakeTelegramClient fakeClient, MessageEnvelope envelope, Message message, Update update) CreateFullScenario(
            long userId = 12345,
            long chatId = 67890,
            string text = "Test message") => TestKitTelegram.CreateFullScenario(userId, chatId, text);
        
        /// <summary>
        /// Создает сценарий нового участника с FakeTelegramClient
        /// </summary>
        public static (FakeTelegramClient fakeClient, MessageEnvelope envelope, Message message, Update update) CreateNewUserScenario(
            long userId = 12345,
            long chatId = 67890) => TestKitTelegram.CreateNewUserScenario(userId, chatId);
        
        /// <summary>
        /// Сбрасывает счетчик MessageId (для изоляции тестов)
        /// </summary>
        public static void ResetMessageIdCounter() => TestKitTelegram.ResetMessageIdCounter();

        #endregion
    }
} 
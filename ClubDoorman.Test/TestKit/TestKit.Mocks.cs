using ClubDoorman.Services;
using ClubDoorman.Models;
using ClubDoorman.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Moq;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Test.TestKit
{
    /// <summary>
    /// Централизованные моки для всех тестов
    /// Унифицированный подход к созданию моков
    /// <tags>mocks, centralized, unified, test-infrastructure</tags>
    /// </summary>
    public static partial class TestKit
    {
        #region Basic Mocks

        /// <summary>
        /// Создает базовый мок для любого интерфейса
        /// <tags>mock, basic, generic, test-infrastructure</tags>
        /// </summary>
        public static Mock<T> CreateMock<T>() where T : class
        {
            return new Mock<T>();
        }

        /// <summary>
        /// Создает мок логгера
        /// <tags>mock, logger, logging, test-infrastructure</tags>
        /// </summary>
        public static Mock<ILogger<T>> CreateLoggerMock<T>() where T : class
        {
            return new Mock<ILogger<T>>();
        }

        /// <summary>
        /// Создает null логгер
        /// <tags>logger, null, logging, test-infrastructure</tags>
        /// </summary>
        public static ILogger<T> CreateNullLogger<T>() where T : class
        {
            return NullLogger<T>.Instance;
        }

        #endregion

        #region Telegram Mocks

        /// <summary>
        /// Создает мок ITelegramBotClient
        /// <tags>mock, telegram, bot-client, api</tags>
        /// </summary>
        public static Mock<ITelegramBotClient> CreateMockBotClient()
        {
            var mock = new Mock<ITelegramBotClient>();
            
            // Настройка базовых методов
            mock.Setup(x => x.GetMe(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User 
                { 
                    Id = 123456789, 
                    IsBot = true, 
                    FirstName = "TestBot",
                    Username = "test_bot"
                });
                
            return mock;
        }

        /// <summary>
        /// Создает мок ITelegramBotClientWrapper
        /// <tags>mock, telegram, bot-client-wrapper, api</tags>
        /// </summary>
        public static Mock<ITelegramBotClientWrapper> CreateMockBotClientWrapper()
        {
            var mock = new Mock<ITelegramBotClientWrapper>();
            
            // Настройка базовых методов
            mock.Setup(x => x.GetMe(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User 
                { 
                    Id = 123456789, 
                    IsBot = true, 
                    FirstName = "TestBot",
                    Username = "test_bot"
                });
                
            return mock;
        }

        /// <summary>
        /// Создает тестовое сообщение
        /// <tags>mock, telegram, message, test-data</tags>
        /// </summary>
        public static Message CreateTestMessage(string text = "Test message")
        {
            var user = CreateTestUser();
            var chat = CreateTestChat();
            
            // Создаем сообщение с базовыми свойствами
            var message = new Message
            {
                Date = DateTime.UtcNow,
                Chat = chat,
                From = user,
                Text = text
            };
            
            return message;
        }

        /// <summary>
        /// Создает тестового пользователя
        /// </summary>
        public static User CreateTestUser(string? username = "test_user", string? firstName = "Test", string? lastName = "User")
        {
            return new User
            {
                Id = 12345,
                IsBot = false,
                FirstName = firstName ?? "Test",
                LastName = lastName,
                Username = username
            };
        }

        /// <summary>
        /// Создает тестовый чат
        /// </summary>
        public static Chat CreateTestChat(string? title = "Test Chat", ChatType type = ChatType.Group)
        {
            return new Chat
            {
                Id = -1001234567890,
                Type = type,
                Title = title
            };
        }

        /// <summary>
        /// Создает тестовый Update
        /// </summary>
        public static Update CreateTestUpdate(Message? message = null)
        {
            return new Update
            {
                Message = message ?? CreateTestMessage()
            };
        }
        #endregion

        #region Service Mocks

        /// <summary>
        /// Создает мок ISpamHamClassifier
        /// </summary>
        public static Mock<ISpamHamClassifier> CreateMockSpamHamClassifier()
        {
            return new Mock<ISpamHamClassifier>();
        }

        /// <summary>
        /// Создает реальный SpamHamClassifier с мок-логгером
        /// </summary>
        public static SpamHamClassifier CreateSpamHamClassifier()
        {
            var mockLogger = new Mock<ILogger<SpamHamClassifier>>();
            return new SpamHamClassifier(mockLogger.Object);
        }

        public static Mock<IModerationService> CreateMockModerationService(ModerationAction action = ModerationAction.Allow, string reason = "Mock moderation")
        {
            var mock = new Mock<IModerationService>();
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>())).ReturnsAsync(new ModerationResult(action, reason));
            return mock;
        }

        public static Mock<ICaptchaService> CreateMockCaptchaService()
        {
            var mock = new Mock<ICaptchaService>();
            mock.Setup(x => x.ValidateCaptchaAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);
            return mock;
        }

        public static Mock<IUserManager> CreateMockUserManager()
        {
            var mock = new Mock<IUserManager>();
            mock.Setup(x => x.Approved(It.IsAny<long>(), It.IsAny<long?>())).Returns(true);
            return mock;
        }

        public static Mock<IUserBanService> CreateMockUserBanService()
        {
            var mock = new Mock<IUserBanService>();
            // Базовые настройки для IUserBanService
            return mock;
        }

        public static Mock<IViolationTracker> CreateMockViolationTracker()
        {
            var mock = new Mock<IViolationTracker>();
            mock.Setup(x => x.RegisterViolation(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<ViolationType>())).Returns(false);
            return mock;
        }
        
        /// <summary> Создает Mock IMessageService </summary>
        public static Mock<IMessageService> CreateMockMessageService()
        {
            var mock = new Mock<IMessageService>();
            // Базовые настройки для IMessageService
            return mock;
        }
        
        /// <summary> Создает Mock IStatisticsService </summary>
        public static Mock<IStatisticsService> CreateMockStatisticsService()
        {
            var mock = new Mock<IStatisticsService>();
            // Базовые настройки для IStatisticsService
            return mock;
        }
        
        /// <summary> Создает Mock IBotPermissionsService </summary>
        public static Mock<IBotPermissionsService> CreateMockBotPermissionsService()
        {
            var mock = new Mock<IBotPermissionsService>();
            // Базовые настройки для IBotPermissionsService
            return mock;
        }
        
        /// <summary> Создает Mock IServiceProvider </summary>
        public static Mock<IServiceProvider> CreateMockServiceProvider()
        {
            var mock = new Mock<IServiceProvider>();
            // Базовые настройки для IServiceProvider
            return mock;
        }
        #endregion

        #region Specialized Mocks
        
        /// <summary>
        /// Создает полный набор моков для MessageHandler тестов
        /// </summary>
        public static (Mock<IModerationService> Moderation, Mock<ICaptchaService> Captcha, Mock<IUserBanService> UserBan, Mock<IMessageService> Message, Mock<IAiChecks> AiChecks) CreateMessageHandlerMocks()
        {
            return (
                CreateMockModerationService(),
                CreateMockCaptchaService(),
                CreateMockUserBanService(),
                CreateMockMessageService(),
                CreateMockAiChecks()
            );
        }
        
        /// <summary>
        /// Создает полный набор моков для CallbackQueryHandler тестов
        /// </summary>
        public static (Mock<ICaptchaService> Captcha, Mock<IStatisticsService> Statistics, Mock<IModerationService> Moderation, Mock<IMessageService> Message) CreateCallbackQueryHandlerMocks()
        {
            return (
                CreateMockCaptchaService(),
                CreateMockStatisticsService(),
                CreateMockModerationService(),
                CreateMockMessageService()
            );
        }
        
        /// <summary>
        /// Создает мок IModerationService с предустановленным действием бана
        /// </summary>
        public static Mock<IModerationService> CreateBanModerationService(string reason = "Mock ban")
        {
            return CreateMockModerationService(ModerationAction.Ban, reason);
        }
        
        /// <summary>
        /// Создает мок IModerationService с предустановленным действием удаления
        /// </summary>
        public static Mock<IModerationService> CreateDeleteModerationService(string reason = "Mock delete")
        {
            return CreateMockModerationService(ModerationAction.Delete, reason);
        }
        
        /// <summary>
        /// Создает мок ICaptchaService с предустановленным успешным ответом
        /// </summary>
        public static Mock<ICaptchaService> CreateSuccessfulCaptchaService()
        {
            var mock = CreateMockCaptchaService();
            mock.Setup(x => x.ValidateCaptchaAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);
            return mock;
        }
        
        /// <summary>
        /// Создает мок ICaptchaService с предустановленным неуспешным ответом
        /// </summary>
        public static Mock<ICaptchaService> CreateFailedCaptchaService()
        {
            var mock = CreateMockCaptchaService();
            mock.Setup(x => x.ValidateCaptchaAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
            return mock;
        }
        
        /// <summary>
        /// Создает мок IUserManager с предустановленным одобренным пользователем
        /// </summary>
        public static Mock<IUserManager> CreateApprovedUserManager()
        {
            var mock = CreateMockUserManager();
            mock.Setup(x => x.Approved(It.IsAny<long>(), It.IsAny<long?>())).Returns(true);
            return mock;
        }
        
        /// <summary>
        /// Создает мок IUserManager с предустановленным неодобренным пользователем
        /// </summary>
        public static Mock<IUserManager> CreateUnapprovedUserManager()
        {
            var mock = CreateMockUserManager();
            mock.Setup(x => x.Approved(It.IsAny<long>(), It.IsAny<long?>())).Returns(false);
            return mock;
        }
        
        /// <summary>
        /// Создает мок IViolationTracker с предустановленным триггером бана
        /// </summary>
        public static Mock<IViolationTracker> CreateBanTriggeringViolationTracker()
        {
            var mock = CreateMockViolationTracker();
            mock.Setup(x => x.RegisterViolation(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<ViolationType>())).Returns(true);
            return mock;
        }
        #endregion

        #region AI Mocks

        /// <summary>
        /// Создает Mock IAiChecks с предустановленными ответами
        /// </summary>
        public static Mock<IAiChecks> CreateMockAiChecks(
            double spamProbability = 0.5,
            double attentionBaitProbability = 0.3,
            double eroticPhotoBaitProbability = 0.1,
            double suspiciousUserSpamProbability = 0.4,
            bool shouldThrowException = false,
            Exception? exceptionToThrow = null)
        {
            var mock = new Mock<IAiChecks>();

            if (shouldThrowException)
            {
                var ex = exceptionToThrow ?? new AiServiceException("Mock API error");
                mock.Setup(x => x.GetSpamProbability(It.IsAny<Message>())).ThrowsAsync(ex);
                mock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), It.IsAny<Func<string, Task>>())).ThrowsAsync(ex);
                mock.Setup(x => x.GetSuspiciousUserSpamProbability(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<List<string>>(), It.IsAny<double>())).ThrowsAsync(ex);
                return mock;
            }

            // Настройка GetSpamProbability
            mock.Setup(x => x.GetSpamProbability(It.IsAny<Message>()))
                .ReturnsAsync(new SpamProbability { Probability = spamProbability, Reason = "Mock spam check" });

            // Настройка GetAttentionBaitProbability
            mock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), It.IsAny<Func<string, Task>>()))
                .ReturnsAsync(new SpamPhotoBio(new SpamProbability { Probability = attentionBaitProbability, Reason = "Mock attention bait check" }, new byte[0], "Mock user bio"));

            // Настройка GetAttentionBaitProbability с текстом
            mock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<Func<string, Task>>()))
                .ReturnsAsync(new SpamPhotoBio(new SpamProbability { Probability = attentionBaitProbability, Reason = "Mock attention bait check" }, new byte[0], "Mock user bio"));

            // Настройка GetSuspiciousUserSpamProbability
            mock.Setup(x => x.GetSuspiciousUserSpamProbability(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<List<string>>(), It.IsAny<double>()))
                .ReturnsAsync(new SpamProbability { Probability = suspiciousUserSpamProbability, Reason = "Mock suspicious user check" });

            // GetMlSuspiciousMessageAnalysis отсутствует в эталонной версии momai
            // mock.Setup(x => x.GetMlSuspiciousMessageAnalysis(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<double>()))
            //     .ReturnsAsync(new SpamProbability { Probability = spamProbability, Reason = "Mock ML analysis" });

            return mock;
        }

        /// <summary>
        /// Создает Mock IAiChecks для сценария "спам"
        /// </summary>
        public static Mock<IAiChecks> CreateSpamAiChecks()
        {
            return CreateMockAiChecks(
                spamProbability: 0.9,
                attentionBaitProbability: 0.8,
                eroticPhotoBaitProbability: 0.7,
                suspiciousUserSpamProbability: 0.9
            );
        }

        /// <summary>
        /// Создает Mock IAiChecks для сценария "нормальное сообщение"
        /// </summary>
        public static Mock<IAiChecks> CreateNormalAiChecks()
        {
            return CreateMockAiChecks(
                spamProbability: 0.1,
                attentionBaitProbability: 0.1,
                eroticPhotoBaitProbability: 0.05,
                suspiciousUserSpamProbability: 0.1
            );
        }

        /// <summary>
        /// Создает Mock IAiChecks для сценария "подозрительный пользователь"
        /// </summary>
        public static Mock<IAiChecks> CreateSuspiciousUserAiChecks()
        {
            return CreateMockAiChecks(
                spamProbability: 0.3,
                attentionBaitProbability: 0.2,
                eroticPhotoBaitProbability: 0.1,
                suspiciousUserSpamProbability: 0.8
            );
        }

        /// <summary>
        /// Создает Mock IAiChecks для сценария "ошибка API"
        /// </summary>
        public static Mock<IAiChecks> CreateErrorAiChecks(Exception? exception = null)
        {
            return CreateMockAiChecks(
                shouldThrowException: true,
                exceptionToThrow: exception ?? new AiServiceException("Mock API error")
            );
        }

        #endregion

        #region Configuration Mocks

        /// <summary>
        /// Создает мок IAppConfig с базовыми настройками
        /// </summary>
        public static Mock<IAppConfig> CreateMockAppConfig()
        {
            var mock = new Mock<IAppConfig>();
            
            mock.Setup(x => x.NoCaptchaGroups).Returns(new HashSet<long>());
            mock.Setup(x => x.NoVpnAdGroups).Returns(new HashSet<long>());
            mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
            mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
            mock.Setup(x => x.AdminChatId).Returns(123456789);
            mock.Setup(x => x.LogAdminChatId).Returns(987654321);
            // ChannelAutoBan отсутствует в эталонной версии momai
            // mock.Setup(x => x.ChannelAutoBan).Returns(true);
            
            return mock;
        }

        #endregion

        /// <summary>
        /// Создает мок IBotPermissionsService с заданным значением isAdmin
        /// <tags>mock, bot-permissions, admin, test-infrastructure</tags>
        /// </summary>
        public static Mock<IBotPermissionsService> CreateBotPermissionsServiceMock(bool isAdmin)
        {
            var mock = CreateMockBotPermissionsService();
            mock.Setup(x => x.IsBotAdminAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(isAdmin);
            mock.Setup(x => x.IsSilentModeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            return mock;
        }

        /// <summary>
        /// Создает мок IBotPermissionsService, который возвращает true только для заданного chatId
        /// <tags>mock, bot-permissions, admin, test-infrastructure, chat-specific</tags>
        /// </summary>
        public static Mock<IBotPermissionsService> CreateBotPermissionsServiceMockForChat(long adminChatId)
        {
            var mock = CreateMockBotPermissionsService();
            mock.Setup(x => x.IsBotAdminAsync(adminChatId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            mock.Setup(x => x.IsBotAdminAsync(It.Is<long>(id => id != adminChatId), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            mock.Setup(x => x.IsSilentModeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            return mock;
        }
    }
} 
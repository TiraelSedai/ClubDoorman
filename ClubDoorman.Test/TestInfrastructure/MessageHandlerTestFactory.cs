using ClubDoorman.Handlers;
using ClubDoorman.Handlers.Commands;
using ClubDoorman.Services;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Test.TestKit;
using ClubDoorman.Test.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


namespace ClubDoorman.Test.TestInfrastructure;

/// <summary>
/// Фабрика для создания MessageHandler с настроенными моками
/// Использует TestKit для создания моков и тестовых данных
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class MessageHandlerTestFactory
{
    // Используем TestKit для создания моков
    public Mock<ITelegramBotClientWrapper> BotMock { get; } = TK.CreateMockBotClientWrapper();
    public Mock<IModerationService> ModerationServiceMock { get; } = TK.CreateMockModerationService();
    public Mock<ICaptchaService> CaptchaServiceMock { get; } = TK.CreateMockCaptchaService();
    public Mock<IUserManager> UserManagerMock { get; } = TK.CreateMockUserManager();
    public Mock<ISpamHamClassifier> ClassifierMock { get; } = TK.CreateMockSpamHamClassifier();
    public Mock<IBadMessageManager> BadMessageManagerMock { get; } = TK.CreateMock<IBadMessageManager>();
    public Mock<IAiChecks> AiChecksMock { get; } = TK.CreateMockAiChecks();
    public Mock<IStatisticsService> StatisticsServiceMock { get; } = TK.CreateMockStatisticsService();
    public Mock<IServiceProvider> ServiceProviderMock { get; } = TK.CreateMockServiceProvider();
    public Mock<IUserFlowLogger> UserFlowLoggerMock { get; } = TK.CreateMock<IUserFlowLogger>();
    public Mock<IMessageService> MessageServiceMock { get; } = TK.CreateMockMessageService();
    public Mock<IChatLinkFormatter> ChatLinkFormatterMock { get; } = TK.CreateMock<IChatLinkFormatter>();
    public Mock<IBotPermissionsService> BotPermissionsServiceMock { get; } = TK.CreateMockBotPermissionsService();
    public Mock<IAppConfig> AppConfigMock { get; } = TK.CreateMockAppConfig();
    public Mock<IViolationTracker> ViolationTrackerMock { get; } = TK.CreateMockViolationTracker();
    public Mock<IUserBanService> UserBanServiceMock { get; } = TK.CreateMockUserBanService();
    public Mock<ILogger<MessageHandler>> LoggerMock { get; } = TK.CreateLoggerMock<MessageHandler>();
    public Mock<ILogger<SuspiciousCommandHandler>> SuspiciousCommandHandlerLoggerMock { get; } = TK.CreateLoggerMock<SuspiciousCommandHandler>();
    public Mock<ISuspiciousUsersStorage> SuspiciousUsersStorageMock { get; } = TK.CreateMock<ISuspiciousUsersStorage>();
    public FakeTelegramClient FakeBotClient { get; } = FakeTelegramClientFactory.Create();

    public MessageHandler CreateMessageHandler()
    {
        return new MessageHandler(
            BotMock.Object,
            ModerationServiceMock.Object,
            CaptchaServiceMock.Object,
            UserManagerMock.Object,
            ClassifierMock.Object,
            BadMessageManagerMock.Object,
            AiChecksMock.Object,
            new GlobalStatsManager(),
            StatisticsServiceMock.Object,
            ServiceProviderMock.Object,
            UserFlowLoggerMock.Object,
            MessageServiceMock.Object,
            ChatLinkFormatterMock.Object,
            BotPermissionsServiceMock.Object,
            AppConfigMock.Object,
            ViolationTrackerMock.Object,
            LoggerMock.Object,
            UserBanServiceMock.Object
        );
    }

    #region Configuration Methods

    public MessageHandlerTestFactory WithBotSetup(Action<Mock<ITelegramBotClientWrapper>> setup)
    {
        setup(BotMock);
        return this;
    }

    public MessageHandlerTestFactory WithModerationServiceSetup(Action<Mock<IModerationService>> setup)
    {
        setup(ModerationServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithCaptchaServiceSetup(Action<Mock<ICaptchaService>> setup)
    {
        setup(CaptchaServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithUserManagerSetup(Action<Mock<IUserManager>> setup)
    {
        setup(UserManagerMock);
        return this;
    }

    public MessageHandlerTestFactory WithClassifierSetup(Action<Mock<ISpamHamClassifier>> setup)
    {
        setup(ClassifierMock);
        return this;
    }

    public MessageHandlerTestFactory WithBadMessageManagerSetup(Action<Mock<IBadMessageManager>> setup)
    {
        setup(BadMessageManagerMock);
        return this;
    }

    public MessageHandlerTestFactory WithAiChecksSetup(Action<Mock<IAiChecks>> setup)
    {
        setup(AiChecksMock);
        return this;
    }

    public MessageHandlerTestFactory WithStatisticsServiceSetup(Action<Mock<IStatisticsService>> setup)
    {
        setup(StatisticsServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithServiceProviderSetup(Action<Mock<IServiceProvider>> setup)
    {
        setup(ServiceProviderMock);
        return this;
    }

    public MessageHandlerTestFactory WithUserFlowLoggerSetup(Action<Mock<IUserFlowLogger>> setup)
    {
        setup(UserFlowLoggerMock);
        return this;
    }

    public MessageHandlerTestFactory WithMessageServiceSetup(Action<Mock<IMessageService>> setup)
    {
        setup(MessageServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithChatLinkFormatterSetup(Action<Mock<IChatLinkFormatter>> setup)
    {
        setup(ChatLinkFormatterMock);
        return this;
    }

    public MessageHandlerTestFactory WithLoggerSetup(Action<Mock<ILogger<MessageHandler>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    public MessageHandlerTestFactory WithAppConfigSetup(Action<Mock<IAppConfig>> setup)
    {
        setup(AppConfigMock);
        return this;
    }

    public MessageHandlerTestFactory WithBotPermissionsServiceSetup(Action<Mock<IBotPermissionsService>> setup)
    {
        setup(BotPermissionsServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithViolationTrackerSetup(Action<Mock<IViolationTracker>> setup)
    {
        setup(ViolationTrackerMock);
        return this;
    }

    public MessageHandlerTestFactory WithUserBanServiceSetup(Action<Mock<IUserBanService>> setup)
    {
        setup(UserBanServiceMock);
        return this;
    }

    public MessageHandlerTestFactory WithSuspiciousUsersStorageSetup(Action<Mock<ISuspiciousUsersStorage>> setup)
    {
        setup(SuspiciousUsersStorageMock);
        return this;
    }

    public MessageHandlerTestFactory WithSuspiciousCommandHandlerLoggerSetup(Action<Mock<ILogger<SuspiciousCommandHandler>>> setup)
    {
        setup(SuspiciousCommandHandlerLoggerMock);
        return this;
    }

    #endregion

    #region Композиционные методы настройки

    /// <summary>
    /// Настройка стандартных моков для всех тестов
    /// </summary>
    public MessageHandlerTestFactory WithStandardMocks()
    {
        WithAppConfigSetup(mock => 
        {
            mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
            mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
            mock.Setup(x => x.AdminChatId).Returns(123456789);
            mock.Setup(x => x.LogAdminChatId).Returns(987654321);
        });
        
        WithBotPermissionsServiceSetup(mock =>
        {
            mock.Setup(x => x.IsSilentModeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        });
        
        WithCaptchaServiceSetup(mock =>
        {
            mock.Setup(x => x.GenerateKey(It.IsAny<long>(), It.IsAny<long>()))
                .Returns("test-key");
            mock.Setup(x => x.GetCaptchaInfo(It.IsAny<string>()))
                .Returns((CaptchaInfo?)null);
        });
        
        WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.InBanlist(It.IsAny<long>()))
                .ReturnsAsync(false);
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>()))
                .ReturnsAsync((string?)null);
        });
        
        WithModerationServiceSetup(mock => 
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(TK.Specialized.Moderation.Allow());
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });
        
        // Настройка ServiceProvider для CommandHandler'ов
        WithServiceProviderSetup(mock =>
        {
            // Создаем реальные экземпляры CommandHandler'ов с правильными логгерами
            var startCommandHandler = new StartCommandHandler(
                BotMock.Object,
                TK.CreateLoggerMock<StartCommandHandler>().Object,
                MessageServiceMock.Object,
                AppConfigMock.Object
            );
            
            var suspiciousCommandHandler = new SuspiciousCommandHandler(
                BotMock.Object,
                ModerationServiceMock.Object,
                MessageServiceMock.Object,
                TK.CreateLoggerMock<SuspiciousCommandHandler>().Object,
                AppConfigMock.Object
            );
            
            // Настраиваем ServiceProvider для возврата CommandHandler'ов
            mock.Setup(x => x.GetService(typeof(StartCommandHandler)))
                .Returns(startCommandHandler);
            mock.Setup(x => x.GetService(typeof(SuspiciousCommandHandler)))
                .Returns(suspiciousCommandHandler);
        });
        
        return this;
    }

    /// <summary>
    /// Настройка моков для сценариев бана
    /// </summary>
    public MessageHandlerTestFactory WithBanMocks()
    {
        // В legacy режиме UserBanService не используется, поэтому не настраиваем его
        // MessageHandler будет вызывать BotMock напрямую
        return this;
    }

    /// <summary>
    /// Настройка моков для сценариев с каналами
    /// </summary>
    public MessageHandlerTestFactory WithChannelMocks()
    {
        WithAppConfigSetup(mock =>
        {
            // ChannelAutoBan отсутствует в эталонной версии momai
            // mock.Setup(x => x.ChannelAutoBan).Returns(true);
        });
        
        // В legacy режиме UserBanService не используется, поэтому не настраиваем его
        // MessageHandler будет вызывать BotMock напрямую
        
        return this;
    }

    /// <summary>
    /// Настройка моков для сценариев модерации
    /// </summary>
    public MessageHandlerTestFactory WithModerationMocks(ModerationAction action = ModerationAction.Allow, string reason = "Test moderation")
    {
        WithModerationServiceSetup(mock => 
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(action, reason));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });
        
        return this;
    }

    #endregion

    #region Factory Methods

    public FakeTelegramClient FakeTelegramClient => FakeTelegramClientFactory.Create();

    public Mock<ITelegramBotClientWrapper> TelegramBotClientWrapperMock { get; } = new();

    public ModerationService CreateModerationServiceWithFake()
    {
        var mockLogger = new Mock<ILogger<ModerationService>>();
        var mockClassifier = new Mock<ISpamHamClassifier>();
        var mockMimicryClassifier = new Mock<IMimicryClassifier>();
        var mockBadMessageManager = new Mock<IBadMessageManager>();
        var mockUserManager = new Mock<IUserManager>();
        var mockAiChecks = new Mock<IAiChecks>();
        var mockSuspiciousUsersStorage = new Mock<ISuspiciousUsersStorage>();
        var mockMessageService = new Mock<IMessageService>();

        return new ModerationService(
            mockClassifier.Object,
            mockMimicryClassifier.Object,
            mockBadMessageManager.Object,
            mockUserManager.Object,
            mockAiChecks.Object,
            mockSuspiciousUsersStorage.Object,
            FakeBotClient as ITelegramBotClient,
            mockMessageService.Object,
            mockLogger.Object
        );
    }

    public CaptchaService CreateCaptchaServiceWithFake()
    {
        var mockLogger = new Mock<ILogger<CaptchaService>>();
        var mockMessageService = new Mock<IMessageService>();
        var mockAppConfig = new Mock<IAppConfig>();
        return new CaptchaService(TelegramBotClientWrapperMock.Object, mockLogger.Object, mockMessageService.Object, mockAppConfig.Object);
    }

    public IUserManager CreateUserManagerWithFake()
    {
        var mockLogger = new Mock<ILogger<UserManager>>();
        var mockApprovedUsersLogger = new Mock<ILogger<ApprovedUsersStorage>>();
        var approvedUsersStorage = new ApprovedUsersStorage(mockApprovedUsersLogger.Object);
        var mockAppConfig = new Mock<IAppConfig>();
        return new UserManager(mockLogger.Object, approvedUsersStorage, mockAppConfig.Object);
    }

    public async Task<MessageHandler> CreateAsync()
    {
        return CreateMessageHandler();
    }

    public SpamHamClassifier CreateMockSpamHamClassifier()
    {
        var mockLogger = new Mock<ILogger<SpamHamClassifier>>();
        return new SpamHamClassifier(mockLogger.Object);
    }

    public MessageHandler CreateMessageHandlerWithFake()
    {
        return CreateMessageHandlerWithFake(FakeTelegramClientFactory.Create());
    }

    public MessageHandler CreateMessageHandlerWithFake(FakeTelegramClient fakeClient)
    {
        // Настраиваем мок для удаления сообщений
        TelegramBotClientWrapperMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<ChatId, int, CancellationToken>((chatId, messageId, token) =>
            {
                fakeClient.DeleteMessage(chatId, messageId, token);
            });

        return new MessageHandler(
            TelegramBotClientWrapperMock.Object,
            ModerationServiceMock.Object,
            CaptchaServiceMock.Object,
            UserManagerMock.Object,
            ClassifierMock.Object,
            BadMessageManagerMock.Object,
            AiChecksMock.Object,
            new GlobalStatsManager(),
            StatisticsServiceMock.Object,
            ServiceProviderMock.Object,
            UserFlowLoggerMock.Object,
            MessageServiceMock.Object,
            ChatLinkFormatterMock.Object,
            BotPermissionsServiceMock.Object,
            AppConfigMock.Object,
            ViolationTrackerMock.Object,
            LoggerMock.Object,
            UserBanServiceMock.Object
        );
    }

    public MessageHandler CreateMessageHandlerWithFake(Action<MessageHandlerTestFactory> setup)
    {
        setup(this);
        return CreateMessageHandler();
    }

    #endregion

    #region Ban Test Scenarios

    /// <summary>
    /// Настраивает моки для сценария бана пользователя с длинным именем
    /// </summary>
    public MessageHandlerTestFactory SetupLongNameBanScenario(User user)
    {
        UserManagerMock.Setup(x => x.Approved(user.Id, null)).Returns(false);
        UserManagerMock.Setup(x => x.InBanlist(user.Id)).ReturnsAsync(false);
        UserManagerMock.Setup(x => x.GetClubUsername(user.Id)).ReturnsAsync((string?)null);
        ModerationServiceMock.Setup(x => x.CheckUserNameAsync(user))
            .ReturnsAsync(new ModerationResult(ModerationAction.Ban, "Длинное имя пользователя"));
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария бана пользователя из блэклиста
    /// </summary>
    public MessageHandlerTestFactory SetupBlacklistBanScenario(User user, Chat chat)
    {
        UserManagerMock.Setup(x => x.Approved(user.Id, null)).Returns(false);
        UserManagerMock.Setup(x => x.InBanlist(user.Id)).ReturnsAsync(true);
        UserManagerMock.Setup(x => x.GetClubUsername(user.Id)).ReturnsAsync((string?)null);
        ModerationServiceMock.Setup(x => x.CheckUserNameAsync(user))
            .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Нормальное имя"));
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария обработки бана из блэклиста
    /// </summary>
    public MessageHandlerTestFactory SetupBlacklistBanHandlingScenario(User user, string reason)
    {
        UserManagerMock.Setup(x => x.Approved(user.Id, null)).Returns(false);
        UserManagerMock.Setup(x => x.InBanlist(user.Id)).ReturnsAsync(true);
        ModerationServiceMock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Ban, reason));
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария удаления по результату модерации (ModerationAction.Delete)
    /// </summary>
    public MessageHandlerTestFactory SetupModerationDeleteScenario(string reason = "ML решил что это спам")
    {
        SetupStandardBanTestScenario();
        
        WithModerationServiceSetup(mock =>
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Delete, reason));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });
        
        // Настраиваем BotMock для обработки ForwardMessage
        WithBotSetup(mock =>
        {
            mock.Setup(x => x.ForwardMessage(It.IsAny<ChatId>(), It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 123456789 } });
            mock.Setup(x => x.SendMessage(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<ReplyParameters>(), It.IsAny<ReplyMarkup>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 123456789 } });
        });
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария AI подтверждения ML подозрения
    /// </summary>
    public MessageHandlerTestFactory SetupAiMlBanScenario(double probability = 0.8, string reason = "ML подозрение")
    {
        SetupStandardBanTestScenario();
        
        WithModerationServiceSetup(mock =>
        {
            // RequireAiReview отсутствует в эталонной версии momai
            // mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            //     .ReturnsAsync(new ModerationResult(ModerationAction.RequireAiReview, reason, 0.75));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });
        
        WithAiChecksSetup(mock =>
        {
            // GetMlSuspiciousMessageAnalysis отсутствует в эталонной версии momai
            // mock.Setup(x => x.GetMlSuspiciousMessageAnalysis(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<double>()))
            //     .ReturnsAsync(new SpamProbability { Probability = probability, Reason = reason });
        });
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария AI отклонения ML подозрения
    /// </summary>
    public MessageHandlerTestFactory SetupAiMlRejectScenario(double probability = 0.3, string reason = "ML подозрение")
    {
        SetupStandardBanTestScenario();
        
        WithModerationServiceSetup(mock =>
        {
            // RequireAiReview отсутствует в эталонной версии momai
            // mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            //     .ReturnsAsync(new ModerationResult(ModerationAction.RequireAiReview, reason, 0.75));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        });
        
        WithAiChecksSetup(mock =>
        {
            // GetMlSuspiciousMessageAnalysis отсутствует в эталонной версии momai
            // mock.Setup(x => x.GetMlSuspiciousMessageAnalysis(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<double>()))
            //     .ReturnsAsync(new SpamProbability { Probability = probability, Reason = reason });
        });
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария повторных нарушений
    /// </summary>
    public MessageHandlerTestFactory SetupRepeatedViolationsBanScenario(string violationType = "TextMention")
    {
        SetupStandardBanTestScenario();
        
        WithViolationTrackerSetup(mock =>
        {
            mock.Setup(x => x.RegisterViolation(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<ViolationType>()))
                .Returns(true); // Возвращаем true, что означает необходимость бана
        });
        
        return this;
    }

    /// <summary>
    /// Настройка стандартных моков для тестов бана (повторяющаяся логика)
    /// </summary>
    public MessageHandlerTestFactory SetupStandardBanTestScenario()
    {
        return this
            .WithStandardMocks()
            .WithBanMocks();
    }

    /// <summary>
    /// Настройка моков для сценария автобана пользователя
    /// </summary>
    public MessageHandlerTestFactory SetupAutoBanScenario(User user, string reason = "Автобан")
    {
        return this
            .WithStandardMocks()
            .WithBanMocks()
            .WithModerationMocks(ModerationAction.Ban, reason)
            .WithUserManagerSetup(mock =>
            {
                mock.Setup(x => x.Approved(user.Id, null)).Returns(false);
                mock.Setup(x => x.InBanlist(user.Id)).ReturnsAsync(false);
            });
        // В legacy режиме UserBanService не используется, поэтому не настраиваем его
        // MessageHandler будет вызывать BotMock напрямую
    }

    /// <summary>
    /// Настройка моков для сценария автобана каналов
    /// </summary>
    public MessageHandlerTestFactory SetupChannelAutoBanScenario()
    {
        return this
            .WithStandardMocks()
            .WithBanMocks()
            .WithChannelMocks();
    }

    /// <summary>
    /// Настройка моков для сценария бана по результату модерации (ModerationAction.Ban)
    /// </summary>
    public MessageHandlerTestFactory SetupModerationBanScenario(string reason = "Спам сообщение")
    {
        return this
            .WithStandardMocks()
            .WithBanMocks()
            .WithModerationMocks(ModerationAction.Ban, reason);
        // В legacy режиме UserBanService не используется, поэтому не настраиваем его
        // MessageHandler будет вызывать BotMock напрямую
    }

    /// <summary>
    /// Настраивает стандартные моки для тестов с длинным именем пользователя
    /// </summary>
    public MessageHandlerTestFactory SetupLongNameBanTestScenario(User user)
    {
        SetupStandardBanTestScenario();
        
        WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.Approved(user.Id, null)).Returns(false);
            mock.Setup(x => x.InBanlist(user.Id)).ReturnsAsync(false);
            mock.Setup(x => x.GetClubUsername(user.Id)).ReturnsAsync((string?)null);
        });
        
        WithModerationServiceSetup(mock => 
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid message"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
            mock.Setup(x => x.CheckUserNameAsync(It.IsAny<User>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Ban, "Длинное имя пользователя"));
        });
        
        // В legacy режиме UserBanService не используется, поэтому не настраиваем его
        // MessageHandler будет вызывать BotMock напрямую
        
        return this;
    }

    /// <summary>
    /// Настраивает стандартные моки для тестов с пользователем в блэклисте
    /// </summary>
    public MessageHandlerTestFactory SetupBlacklistUserTestScenario(User user)
    {
        SetupStandardBanTestScenario();
        
        WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.Approved(user.Id, null)).Returns(false);
            mock.Setup(x => x.InBanlist(user.Id)).ReturnsAsync(true); // Пользователь в блэклисте
            mock.Setup(x => x.GetClubUsername(user.Id)).ReturnsAsync((string?)null);
        });
        
        WithModerationServiceSetup(mock => 
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid message"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
            mock.Setup(x => x.CheckUserNameAsync(It.IsAny<User>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Имя пользователя в порядке"));
        });
        
        // В legacy режиме UserBanService не используется, поэтому не настраиваем его
        // MessageHandler будет вызывать BotMock напрямую
        
        return this;
    }

    /// <summary>
    /// Настраивает стандартные моки для тестов с каналом
    /// </summary>
    public MessageHandlerTestFactory SetupChannelTestScenario(Chat chat)
    {
        SetupStandardBanTestScenario();
        
        WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.InBanlist(It.IsAny<long>())).ReturnsAsync(false);
            mock.Setup(x => x.GetClubUsername(It.IsAny<long>())).ReturnsAsync((string?)null);
        });
        
        WithModerationServiceSetup(mock => 
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid message"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
        })
        .WithBotSetup(mock => 
        {
            // Настраиваем GetChat для корректной работы HandleChannelMessageAsync
            mock.Setup(x => x.GetChat(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(chat);
        });
        
        return this;
    }

    #endregion
}

using ClubDoorman.Handlers;
using ClubDoorman.Handlers.Commands;
using ClubDoorman.Services;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Models.Requests;
using ClubDoorman.Test.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.Test.TestKit;

/// <summary>
/// Билдер для создания MessageHandler с настроенными зависимостями
/// <tags>builders, message-handler, fluent-api, test-infrastructure</tags>
/// </summary>
public class MessageHandlerBuilder
{
    private readonly Mock<ITelegramBotClientWrapper> _botMock = TK.CreateMockBotClientWrapper();
    private readonly Mock<IModerationService> _moderationServiceMock = TK.CreateMockModerationService();
    private readonly Mock<ICaptchaService> _captchaServiceMock = TK.CreateMockCaptchaService();
    private readonly Mock<IUserManager> _userManagerMock = TK.CreateMockUserManager();
    private readonly Mock<ISpamHamClassifier> _classifierMock = TK.CreateMockSpamHamClassifier();
    private readonly Mock<IBadMessageManager> _badMessageManagerMock = TK.CreateMock<IBadMessageManager>();
    private readonly Mock<IAiChecks> _aiChecksMock = TK.CreateMockAiChecks();
    private readonly Mock<IStatisticsService> _statisticsServiceMock = TK.CreateMockStatisticsService();
    private readonly Mock<IServiceProvider> _serviceProviderMock = TK.CreateMockServiceProvider();
    private readonly Mock<IUserFlowLogger> _userFlowLoggerMock = TK.CreateMock<IUserFlowLogger>();
    private readonly Mock<IMessageService> _messageServiceMock = TK.CreateMockMessageService();
    private readonly Mock<IChatLinkFormatter> _chatLinkFormatterMock = TK.CreateMock<IChatLinkFormatter>();
    private readonly Mock<IBotPermissionsService> _botPermissionsServiceMock = TK.CreateMockBotPermissionsService();
    private readonly Mock<IAppConfig> _appConfigMock = TK.CreateMockAppConfig();
    private readonly Mock<IViolationTracker> _violationTrackerMock = TK.CreateMockViolationTracker();
    private readonly Mock<IUserBanService> _userBanServiceMock = TK.CreateMockUserBanService();
    private readonly Mock<ILogger<MessageHandler>> _loggerMock = TK.CreateLoggerMock<MessageHandler>();
    private readonly Mock<ILogger<SuspiciousCommandHandler>> _suspiciousCommandHandlerLoggerMock = TK.CreateLoggerMock<SuspiciousCommandHandler>();
    private readonly Mock<ISuspiciousUsersStorage> _suspiciousUsersStorageMock = TK.CreateMock<ISuspiciousUsersStorage>();
    private readonly Mock<IMessageHandler> _messageHandlerMock = new();

    /// <summary>
    /// Настраивает модерационный сервис через билдер
    /// <tags>builders, message-handler, moderation-service, fluent-api</tags>
    /// </summary>
    public MessageHandlerBuilder WithModerationService(Action<ModerationServiceMockBuilder> configure)
    {
        var builder = TK.CreateModerationServiceMock();
        configure(builder);
        var mock = builder.Build();
        
        // Копируем настройки в основной мок
        _moderationServiceMock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .Returns(mock.Object.CheckMessageAsync(It.IsAny<Message>()));
        _moderationServiceMock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(mock.Object.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()));
        
        return this;
    }

    /// <summary>
    /// Настраивает менеджер пользователей через билдер
    /// <tags>builders, message-handler, user-manager, fluent-api</tags>
    /// </summary>
    public MessageHandlerBuilder WithUserManager(Action<UserManagerMockBuilder> configure)
    {
        var builder = TK.CreateUserManagerMock();
        configure(builder);
        var mock = builder.Build();
        
        // Копируем настройки в основной мок
        _userManagerMock.Setup(x => x.Approved(It.IsAny<long>(), null))
            .Returns(mock.Object.Approved(It.IsAny<long>(), null));
        _userManagerMock.Setup(x => x.InBanlist(It.IsAny<long>()))
            .Returns(mock.Object.InBanlist(It.IsAny<long>()));
        
        return this;
    }

    /// <summary>
    /// Настраивает сервис капчи через билдер
    /// <tags>builders, message-handler, captcha-service, fluent-api</tags>
    /// </summary>
    public MessageHandlerBuilder WithCaptchaService(Action<CaptchaServiceMockBuilder> configure)
    {
        var builder = TK.CreateCaptchaServiceMock();
        configure(builder);
        var mock = builder.Build();
        
        // Копируем настройки в основной мок
        _captchaServiceMock.Setup(x => x.CreateCaptchaAsync(It.IsAny<CreateCaptchaRequest>()))
            .Returns(mock.Object.CreateCaptchaAsync(It.IsAny<CreateCaptchaRequest>()));
        
        return this;
    }

    /// <summary>
    /// Настраивает AI проверки через билдер
    /// <tags>builders, message-handler, ai-checks, fluent-api</tags>
    /// </summary>
    public MessageHandlerBuilder WithAiChecks(Action<AiChecksMockBuilder> configure)
    {
        var builder = TK.CreateAiChecksMock();
        configure(builder);
        var mock = builder.Build();
        
        // Копируем настройки в основной мок
        _aiChecksMock.Setup(x => x.GetSpamProbability(It.IsAny<Message>()))
            .Returns(mock.Object.GetSpamProbability(It.IsAny<Message>()));
        
        return this;
    }

    /// <summary>
    /// Настраивает Telegram бота через билдер
    /// <tags>builders, message-handler, telegram-bot, fluent-api</tags>
    /// </summary>
    public MessageHandlerBuilder WithTelegramBot(Action<TelegramBotMockBuilder> configure)
    {
        var builder = TK.CreateTelegramBotMock();
        configure(builder);
        var mock = builder.Build();
        
        // Копируем настройки в основной мок
        _botMock.Setup(x => x.SendMessageAsync(
            It.IsAny<ChatId>(),
            It.IsAny<string>(),
            It.IsAny<ParseMode>(),
            It.IsAny<ReplyParameters>(),
            It.IsAny<ReplyMarkup>(),
            It.IsAny<CancellationToken>()))
            .Returns(mock.Object.SendMessageAsync(
                It.IsAny<ChatId>(),
                It.IsAny<string>(),
                It.IsAny<ParseMode>(),
                It.IsAny<ReplyParameters>(),
                It.IsAny<ReplyMarkup>(),
                It.IsAny<CancellationToken>()));
        
        return this;
    }

    /// <summary>
    /// Настраивает стандартные моки (базовая конфигурация)
    /// <tags>builders, message-handler, standard-mocks, fluent-api</tags>
    /// </summary>
    public MessageHandlerBuilder WithStandardMocks()
    {
        // Настройка стандартных моков
        _moderationServiceMock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Test moderation"));
        _moderationServiceMock.Setup(x => x.CheckUserNameAsync(It.IsAny<User>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Test user name"));
        _moderationServiceMock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>())).Returns(false);
        
        _userManagerMock.Setup(x => x.Approved(It.IsAny<long>(), null)).Returns(false);
        _userManagerMock.Setup(x => x.InBanlist(It.IsAny<long>())).ReturnsAsync(false);
        
        _captchaServiceMock.Setup(x => x.CreateCaptchaAsync(It.IsAny<CreateCaptchaRequest>()))
            .ReturnsAsync(new CaptchaInfo(123, "Test Chat", DateTime.UtcNow, new User { Id = 456 }, 1, new CancellationTokenSource(), null));
        
        _classifierMock.Setup(x => x.IsSpam(It.IsAny<string>())).ReturnsAsync((false, 0.5f));
        
        _badMessageManagerMock.Setup(x => x.KnownBadMessage(It.IsAny<string>())).Returns(false);
        
        _aiChecksMock.Setup(x => x.GetSpamProbability(It.IsAny<Message>()))
            .ReturnsAsync(new SpamProbability { Probability = 0.1, Reason = "Approved" });
        
        _botMock.Setup(x => x.SendMessageAsync(
            It.IsAny<ChatId>(),
            It.IsAny<string>(),
            It.IsAny<ParseMode>(),
            It.IsAny<ReplyParameters>(),
            It.IsAny<ReplyMarkup>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message());
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария бана
    /// <tags>builders, message-handler, ban-scenario, fluent-api</tags>
    /// </summary>
    public MessageHandlerBuilder WithBanMocks()
    {
        WithStandardMocks();
        
        _moderationServiceMock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Ban, "Spam detected"));
        
        _botMock.Setup(x => x.BanChatMemberAsync(
            It.IsAny<ChatId>(),
            It.IsAny<long>(),
            It.IsAny<DateTime?>(),
            It.IsAny<bool?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария канала
    /// <tags>builders, message-handler, channel-scenario, fluent-api</tags>
    /// </summary>
    public MessageHandlerBuilder WithChannelMocks()
    {
        WithStandardMocks();
        
        // ChannelAutoBan отсутствует в IAppConfig, это статическое свойство в Config.cs
        // _appConfigMock.Setup(x => x.ChannelAutoBan).Returns(true);
        
        _botMock.Setup(x => x.BanChatMemberAsync(
            It.IsAny<ChatId>(),
            It.IsAny<long>(),
            It.IsAny<DateTime?>(),
            It.IsAny<bool?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария модерации
    /// <tags>builders, message-handler, moderation-scenario, fluent-api</tags>
    /// </summary>
    public MessageHandlerBuilder WithModerationMocks(ModerationAction action = ModerationAction.Allow, string reason = "Test moderation")
    {
        WithStandardMocks();
        
        _moderationServiceMock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(action, reason));
        
        if (action == ModerationAction.Delete)
        {
            _botMock.Setup(x => x.DeleteMessageAsync(
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария AI/ML
    /// <tags>builders, message-handler, ai-ml-scenario, fluent-api</tags>
    /// </summary>
    public MessageHandlerBuilder WithAiMlMocks(double probability = 0.8, string reason = "ML подозрение")
    {
        WithStandardMocks();
        
        _classifierMock.Setup(x => x.IsSpam(It.IsAny<string>())).ReturnsAsync((true, (float)probability));
        
        _aiChecksMock.Setup(x => x.GetSpamProbability(It.IsAny<Message>()))
            .ReturnsAsync(new SpamProbability { Probability = probability, Reason = probability > 0.5 ? "Spam detected" : "Approved" });
        
        return this;
    }

    /// <summary>
    /// Создает MessageHandler с настроенными зависимостями
    /// <tags>builders, message-handler, build, fluent-api</tags>
    /// </summary>
    public MessageHandler Build()
    {
        return new MessageHandler(
            _botMock.Object,
            _moderationServiceMock.Object,
            _captchaServiceMock.Object,
            _userManagerMock.Object,
            _classifierMock.Object,
            _badMessageManagerMock.Object,
            _aiChecksMock.Object,
            new GlobalStatsManager(),
            _statisticsServiceMock.Object,
            _serviceProviderMock.Object,
            _userFlowLoggerMock.Object,
            _messageServiceMock.Object,
            _chatLinkFormatterMock.Object,
            _botPermissionsServiceMock.Object,
            _appConfigMock.Object,
            _violationTrackerMock.Object,
            _loggerMock.Object,
            _userBanServiceMock.Object
        );
    }

    /// <summary>
    /// Создает Mock<IMessageHandler> для прокси-сервисов
    /// <tags>builders, message-handler, proxy-services, fluent-api</tags>
    /// </summary>
    public Mock<IMessageHandler> BuildMock()
    {
        // Настраиваем мок IMessageHandler на основе реального MessageHandler
        var realHandler = Build();
        
        // Настраиваем прокси-вызовы к реальному MessageHandler
        _messageHandlerMock.Setup(x => x.DeleteAndReportMessage(It.IsAny<Message>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns<Message, string, bool, CancellationToken>((msg, reason, silent, ct) => 
                realHandler.DeleteAndReportMessage(msg, reason, silent, ct));
                
        _messageHandlerMock.Setup(x => x.DeleteAndReportToLogChat(It.IsAny<Message>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<Message, string, CancellationToken>((msg, reason, ct) => 
                realHandler.DeleteAndReportToLogChat(msg, reason, ct));
                
        _messageHandlerMock.Setup(x => x.DontDeleteButReportMessage(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns<Message, User, bool, CancellationToken>((msg, user, silent, ct) => 
                realHandler.DontDeleteButReportMessage(msg, user, silent, ct));
                
        _messageHandlerMock.Setup(x => x.SendSuspiciousMessageWithButtons(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<SuspiciousMessageNotificationData>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns<Message, User, SuspiciousMessageNotificationData, bool, CancellationToken>((msg, user, data, silent, ct) => 
                realHandler.SendSuspiciousMessageWithButtons(msg, user, data, silent, ct));
                
        _messageHandlerMock.Setup(x => x.HandleNewMembersAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns<Message, CancellationToken>((msg, ct) => 
                realHandler.HandleNewMembersAsync(msg, ct));
                
        _messageHandlerMock.Setup(x => x.ProcessNewUserAsync(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns<Message, User, CancellationToken>((msg, user, ct) => 
                realHandler.ProcessNewUserAsync(msg, user, ct));
                
        _messageHandlerMock.Setup(x => x.CanHandle(It.IsAny<Message>()))
            .Returns<Message>(msg => realHandler.CanHandle(msg));
            
        _messageHandlerMock.Setup(x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns<Message, CancellationToken>((msg, ct) => 
                realHandler.HandleAsync(msg, ct));
        
        return _messageHandlerMock;
    }

    /// <summary>
    /// Возвращает мок бота для верификации
    /// <tags>builders, message-handler, bot-mock, verification</tags>
    /// </summary>
    public Mock<ITelegramBotClientWrapper> BotMock => _botMock;

    /// <summary>
    /// Возвращает мок модерационного сервиса для верификации
    /// <tags>builders, message-handler, moderation-mock, verification</tags>
    /// </summary>
    public Mock<IModerationService> ModerationServiceMock => _moderationServiceMock;

    /// <summary>
    /// Возвращает мок менеджера пользователей для верификации
    /// <tags>builders, message-handler, user-manager-mock, verification</tags>
    /// </summary>
    public Mock<IUserManager> UserManagerMock => _userManagerMock;
} 
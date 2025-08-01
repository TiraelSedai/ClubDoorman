using ClubDoorman.Handlers;
using ClubDoorman.Services.UserJoin;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Services;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Models.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Telegram.Bot.Types;
using ClubDoorman.Test.TestKit.Builders.MockBuilders;

namespace ClubDoorman.Test.TestKit;

/// <summary>
/// Билдер для создания UserJoinService с настроенными зависимостями
/// <tags>builders, user-join-service, fluent-api, test-infrastructure</tags>
/// </summary>
public class UserJoinServiceBuilder
{
    private readonly Mock<ITelegramBotClientWrapper> _botMock = new();
    private readonly Mock<IModerationService> _moderationServiceMock = new();
    private readonly Mock<ICaptchaService> _captchaServiceMock = new();
    private readonly Mock<IUserManager> _userManagerMock = new();
    private readonly Mock<ISpamHamClassifier> _classifierMock = new();
    private readonly Mock<IBadMessageManager> _badMessageManagerMock = new();
    private readonly Mock<IAiChecks> _aiChecksMock = new();
    private readonly Mock<GlobalStatsManager> _globalStatsManagerMock = new();
    private readonly Mock<IStatisticsService> _statisticsServiceMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<IUserFlowLogger> _userFlowLoggerMock = new();
    private readonly Mock<IMessageService> _messageServiceMock = new();
    private readonly Mock<IChatLinkFormatter> _chatLinkFormatterMock = new();
    private readonly Mock<IBotPermissionsService> _botPermissionsServiceMock = new();
    private readonly Mock<IAppConfig> _appConfigMock = new();
    private readonly Mock<IViolationTracker> _violationTrackerMock = new();
    private readonly Mock<ILogger<MessageHandler>> _messageHandlerLoggerMock = new();
    private readonly Mock<IUserBanService> _userBanServiceMock = new();
    private readonly Mock<ILogger<UserJoinService>> _loggerMock = new();
    
    private Mock<IMessageHandler> _messageHandlerMock = new();

    /// <summary>
    /// Настраивает стандартные моки для базового сценария
    /// <tags>builders, user-join-service, standard-mocks, fluent-api</tags>
    /// </summary>
    public UserJoinServiceBuilder WithStandardMocks()
    {
        // Используем BilboBuilder для создания Mock<IMessageHandler>
        var messageHandlerMock = TK.CreateMessageHandlerBuilder()
            .WithStandardMocks()
            .BuildMock();
            
        _messageHandlerMock = messageHandlerMock;
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария успешного присоединения пользователя
    /// <tags>builders, user-join-service, success-scenario, fluent-api</tags>
    /// </summary>
    public UserJoinServiceBuilder WithSuccessfulJoinScenario()
    {
        // Создаем MessageHandler с моками для успешного сценария
        var messageHandlerMock = TK.CreateMessageHandlerBuilder()
            .WithStandardMocks()
            .WithModerationService(builder => builder.ThatAllowsMessages())
            .WithUserManager(builder => builder.ThatIsNotInBanlist(It.IsAny<long>()))
            .WithCaptchaService(builder => builder.ThatSucceeds())
            .WithTelegramBot(builder => builder.ThatSendsMessageSuccessfully())
            .BuildMock();
            
        _messageHandlerMock = messageHandlerMock;
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария пользователя в блэклисте
    /// <tags>builders, user-join-service, blacklist-scenario, fluent-api</tags>
    /// </summary>
    public UserJoinServiceBuilder WithBlacklistedUserScenario()
    {
        // Создаем MessageHandler с моками для пользователя в блэклисте
        var messageHandlerMock = TK.CreateMessageHandlerBuilder()
            .WithStandardMocks()
            .WithUserManager(builder => builder.ThatIsInBanlist(It.IsAny<long>()))
            .WithTelegramBot(builder => builder.ThatSendsMessageSuccessfully())
            .BuildMock();
            
        _messageHandlerMock = messageHandlerMock;
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария создания капчи
    /// <tags>builders, user-join-service, captcha-scenario, fluent-api</tags>
    /// </summary>
    public UserJoinServiceBuilder WithCaptchaScenario()
    {
        // Создаем MessageHandler с моками для создания капчи
        var messageHandlerMock = TK.CreateMessageHandlerBuilder()
            .WithStandardMocks()
            .WithModerationService(builder => builder.ThatAllowsMessages())
            .WithUserManager(builder => builder.ThatIsNotInBanlist(It.IsAny<long>()))
            .WithCaptchaService(builder => builder.ThatSucceeds())
            .WithTelegramBot(builder => builder.ThatSendsMessageSuccessfully())
            .BuildMock();
            
        _messageHandlerMock = messageHandlerMock;
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария длинного имени пользователя
    /// <tags>builders, user-join-service, long-name-scenario, fluent-api</tags>
    /// </summary>
    public UserJoinServiceBuilder WithLongNameScenario()
    {
        // Создаем MessageHandler с моками для длинного имени
        var messageHandlerMock = TK.CreateMessageHandlerBuilder()
            .WithStandardMocks()
            .WithModerationService(builder => builder.ThatBansUsers())
            .WithTelegramBot(builder => builder.ThatSendsMessageSuccessfully())
            .BuildMock();
            
        _messageHandlerMock = messageHandlerMock;
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария клубного пользователя
    /// <tags>builders, user-join-service, club-user-scenario, fluent-api</tags>
    /// </summary>
    public UserJoinServiceBuilder WithClubUserScenario()
    {
        // Создаем MessageHandler с моками для клубного пользователя
        var messageHandlerMock = TK.CreateMessageHandlerBuilder()
            .WithStandardMocks()
            .WithUserManager(builder => builder.ThatApprovesUser(It.IsAny<long>()))
            .WithTelegramBot(builder => builder.ThatSendsMessageSuccessfully())
            .BuildMock();
            
        _messageHandlerMock = messageHandlerMock;
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария ошибки внешнего сервиса
    /// <tags>builders, user-join-service, error-scenario, fluent-api</tags>
    /// </summary>
    public UserJoinServiceBuilder WithErrorScenario()
    {
        // Создаем MessageHandler с моками, которые выбрасывают исключения
        var messageHandlerMock = TK.CreateMessageHandlerBuilder()
            .WithStandardMocks()
            .WithModerationService(builder => builder.ThatBansUsers("Error occurred"))
            .WithTelegramBot(builder => builder.ThatSendsMessageSuccessfully())
            .BuildMock();
            
        _messageHandlerMock = messageHandlerMock;
        
        return this;
    }

    /// <summary>
    /// Настраивает моки для сценария с кастомными настройками
    /// <tags>builders, user-join-service, custom-scenario, fluent-api</tags>
    /// </summary>
    public UserJoinServiceBuilder WithCustomScenario(Action<Mock<IMessageHandler>> customSetup)
    {
        var messageHandlerMock = TK.CreateMessageHandlerBuilder()
            .WithStandardMocks()
            .BuildMock();
            
        customSetup?.Invoke(messageHandlerMock);
        _messageHandlerMock = messageHandlerMock;
        
        return this;
    }

    /// <summary>
    /// Создает UserJoinService с настроенными зависимостями
    /// <tags>builders, user-join-service, build, fluent-api</tags>
    /// </summary>
    public UserJoinService Build()
    {
        return new UserJoinService(_messageHandlerMock.Object, _loggerMock.Object);
    }

    /// <summary>
    /// Возвращает мок IMessageHandler для дополнительной настройки
    /// <tags>builders, user-join-service, message-handler-mock, fluent-api</tags>
    /// </summary>
    public Mock<IMessageHandler> MessageHandlerMock => _messageHandlerMock;

    /// <summary>
    /// Возвращает мок логгера для дополнительной настройки
    /// <tags>builders, user-join-service, logger-mock, fluent-api</tags>
    /// </summary>
    public Mock<ILogger<UserJoinService>> LoggerMock => _loggerMock;
} 
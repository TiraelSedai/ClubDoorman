using ClubDoorman.Handlers;
using ClubDoorman.Services.Notifications;
using ClubDoorman.Services;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Telegram.Bot.Types;
using ClubDoorman.Models.Notifications;

namespace ClubDoorman.Test.TestKit;

/// <summary>
/// Билдер для создания NotificationService с настроенными зависимостями
/// <tags>builders, notification-service, fluent-api, test-infrastructure</tags>
/// </summary>
public class NotificationServiceBuilder
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
    
    private Mock<IMessageHandler> _messageHandlerMock = new();

    /// <summary>
    /// Настраивает стандартные моки для базового сценария
    /// <tags>builders, notification-service, standard-mocks, fluent-api</tags>
    /// </summary>
    public NotificationServiceBuilder WithStandardMocks()
    {
        // Используем BilboBuilder для создания Mock<IMessageHandler>
        var messageHandlerMock = TK.CreateMessageHandlerBuilder()
            .WithStandardMocks()
            .BuildMock();
            
        _messageHandlerMock = messageHandlerMock;
        
        return this;
    }

    /// <summary>
    /// Создает NotificationService с настроенными зависимостями
    /// <tags>builders, notification-service, build, fluent-api</tags>
    /// </summary>
    public NotificationService Build()
    {
        return new NotificationService(_messageHandlerMock.Object);
    }

    /// <summary>
    /// Возвращает мок IMessageHandler для дополнительной настройки
    /// <tags>builders, notification-service, message-handler-mock, fluent-api</tags>
    /// </summary>
    public Mock<IMessageHandler> MessageHandlerMock => _messageHandlerMock;

    /// <summary>
    /// Возвращает мок TelegramBot для дополнительной настройки
    /// <tags>builders, notification-service, bot-mock, fluent-api</tags>
    /// </summary>
    public Mock<ITelegramBotClientWrapper> BotMock => _botMock;

    /// <summary>
    /// Возвращает мок MessageService для дополнительной настройки
    /// <tags>builders, notification-service, message-service-mock, fluent-api</tags>
    /// </summary>
    public Mock<IMessageService> MessageServiceMock => _messageServiceMock;
} 
using ClubDoorman.Services;
using ClubDoorman.Handlers;

namespace ClubDoorman.Test.TestKit.Builders.MockBuilders;

/// <summary>
/// Билдеры для создания и настройки моков
/// <tags>builders, mocks, fluent-api, test-infrastructure</tags>
/// </summary>
public static class TestKitMockBuilders
{
    /// <summary>
    /// Создает билдер для мока IModerationService
    /// <tags>builders, moderation-service, mocks, fluent-api</tags>
    /// </summary>
    public static ModerationServiceMockBuilder CreateModerationServiceMock() => new();
    
    /// <summary>
    /// Создает билдер для мока IUserManager
    /// <tags>builders, user-manager, mocks, fluent-api</tags>
    /// </summary>
    public static UserManagerMockBuilder CreateUserManagerMock() => new();
    
    /// <summary>
    /// Создает билдер для мока ICaptchaService
    /// <tags>builders, captcha-service, mocks, fluent-api</tags>
    /// </summary>
    public static CaptchaServiceMockBuilder CreateCaptchaServiceMock() => new();
    
    /// <summary>
    /// Создает билдер для мока IAiChecks
    /// <tags>builders, ai-checks, mocks, fluent-api</tags>
    /// </summary>
    public static AiChecksMockBuilder CreateAiChecksMock() => new();
    
    /// <summary>
    /// Создает билдер для мока ITelegramBotClientWrapper
    /// <tags>builders, telegram-bot, mocks, fluent-api</tags>
    /// </summary>
    public static TelegramBotMockBuilder CreateTelegramBotMock() => new();
    
    /// <summary>
    /// Создает билдер для мока IMessageService
    /// <tags>builders, message-service, mocks, fluent-api</tags>
    /// </summary>
    public static MessageServiceMockBuilder CreateMessageServiceMock() => new();
    
    /// <summary>
    /// Создает билдер для мока MessageHandler
    /// <tags>builders, message-handler, mocks, fluent-api</tags>
    /// </summary>
    public static MessageHandlerMockBuilder CreateMessageHandlerMock() => new();
} 
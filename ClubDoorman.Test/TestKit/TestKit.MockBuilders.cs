using ClubDoorman.Services;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Models.Requests;
using ClubDoorman.Handlers;
using ClubDoorman.Handlers.Commands;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.Test.TestKit;

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
}

/// <summary>
/// Билдер для мока IModerationService
/// <tags>builders, moderation-service, mocks, fluent-api</tags>
/// </summary>
public class ModerationServiceMockBuilder
{
    private readonly Mock<IModerationService> _mock = new();
    private ModerationAction _defaultAction = ModerationAction.Allow;
    private string _defaultReason = "Test moderation";
    private double? _defaultConfidence = null;

    /// <summary>
    /// Настраивает мок для возврата указанного действия
    /// <tags>builders, moderation-service, action, fluent-api</tags>
    /// </summary>
    public ModerationServiceMockBuilder ThatReturns(ModerationAction action)
    {
        _defaultAction = action;
        return this;
    }

    /// <summary>
    /// Настраивает мок для возврата указанной причины
    /// <tags>builders, moderation-service, reason, fluent-api</tags>
    /// </summary>
    public ModerationServiceMockBuilder WithReason(string reason)
    {
        _defaultReason = reason;
        return this;
    }

    /// <summary>
    /// Настраивает мок для возврата указанной уверенности
    /// <tags>builders, moderation-service, confidence, fluent-api</tags>
    /// </summary>
    public ModerationServiceMockBuilder WithConfidence(double confidence)
    {
        _defaultConfidence = confidence;
        return this;
    }

    /// <summary>
    /// Настраивает мок для разрешения сообщений
    /// <tags>builders, moderation-service, allow, fluent-api</tags>
    /// </summary>
    public ModerationServiceMockBuilder ThatAllowsMessages()
    {
        _defaultAction = ModerationAction.Allow;
        _defaultReason = "Message allowed";
        return this;
    }

    /// <summary>
    /// Настраивает мок для удаления сообщений
    /// <tags>builders, moderation-service, delete, fluent-api</tags>
    /// </summary>
    public ModerationServiceMockBuilder ThatDeletesMessages(string reason = "Spam detected")
    {
        _defaultAction = ModerationAction.Delete;
        _defaultReason = reason;
        return this;
    }

    /// <summary>
    /// Настраивает мок для бана пользователей
    /// <tags>builders, moderation-service, ban, fluent-api</tags>
    /// </summary>
    public ModerationServiceMockBuilder ThatBansUsers(string reason = "Spam detected")
    {
        _defaultAction = ModerationAction.Ban;
        _defaultReason = reason;
        return this;
    }

    /// <summary>
    /// Настраивает мок для проверки одобрения пользователя
    /// <tags>builders, moderation-service, user-approval, fluent-api</tags>
    /// </summary>
    public ModerationServiceMockBuilder ThatApprovesUser(long userId)
    {
        _mock.Setup(x => x.IsUserApproved(userId, It.IsAny<long>())).Returns(true);
        return this;
    }

    /// <summary>
    /// Настраивает мок для отклонения пользователя
    /// <tags>builders, moderation-service, user-rejection, fluent-api</tags>
    /// </summary>
    public ModerationServiceMockBuilder ThatRejectsUser(long userId)
    {
        _mock.Setup(x => x.IsUserApproved(userId, It.IsAny<long>())).Returns(false);
        return this;
    }

    /// <summary>
    /// Создает настроенный мок
    /// <tags>builders, moderation-service, build, fluent-api</tags>
    /// </summary>
    public Mock<IModerationService> Build()
    {
        _mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(_defaultAction, _defaultReason, _defaultConfidence));
        
        return _mock;
    }

    /// <summary>
    /// Создает объект сервиса из мока
    /// <tags>builders, moderation-service, object, fluent-api</tags>
    /// </summary>
    public IModerationService BuildObject() => Build().Object;
}

/// <summary>
/// Билдер для мока IUserManager
/// <tags>builders, user-manager, mocks, fluent-api</tags>
/// </summary>
public class UserManagerMockBuilder
{
    private readonly Mock<IUserManager> _mock = new();

    /// <summary>
    /// Настраивает мок для одобрения пользователя
    /// <tags>builders, user-manager, approve, fluent-api</tags>
    /// </summary>
    public UserManagerMockBuilder ThatApprovesUser(long userId)
    {
        _mock.Setup(x => x.Approved(userId, null)).Returns(true);
        return this;
    }

    /// <summary>
    /// Настраивает мок для отклонения пользователя
    /// <tags>builders, user-manager, reject, fluent-api</tags>
    /// </summary>
    public UserManagerMockBuilder ThatRejectsUser(long userId)
    {
        _mock.Setup(x => x.Approved(userId, null)).Returns(false);
        return this;
    }

    /// <summary>
    /// Настраивает мок для отсутствия пользователя в банлисте
    /// <tags>builders, user-manager, not-banned, fluent-api</tags>
    /// </summary>
    public UserManagerMockBuilder ThatIsNotInBanlist(long userId)
    {
        _mock.Setup(x => x.InBanlist(userId)).ReturnsAsync(false);
        return this;
    }

    /// <summary>
    /// Настраивает мок для наличия пользователя в банлисте
    /// <tags>builders, user-manager, banned, fluent-api</tags>
    /// </summary>
    public UserManagerMockBuilder ThatIsInBanlist(long userId)
    {
        _mock.Setup(x => x.InBanlist(userId)).ReturnsAsync(true);
        return this;
    }

    /// <summary>
    /// Создает настроенный мок
    /// <tags>builders, user-manager, build, fluent-api</tags>
    /// </summary>
    public Mock<IUserManager> Build() => _mock;

    /// <summary>
    /// Создает объект сервиса из мока
    /// <tags>builders, user-manager, object, fluent-api</tags>
    /// </summary>
    public IUserManager BuildObject() => Build().Object;
}

/// <summary>
/// Билдер для мока ICaptchaService
/// <tags>builders, captcha-service, mocks, fluent-api</tags>
/// </summary>
public class CaptchaServiceMockBuilder
{
    private readonly Mock<ICaptchaService> _mock = new();

    /// <summary>
    /// Настраивает мок для успешной капчи
    /// <tags>builders, captcha-service, success, fluent-api</tags>
    /// </summary>
    public CaptchaServiceMockBuilder ThatSucceeds()
    {
        _mock.Setup(x => x.CreateCaptchaAsync(It.IsAny<CreateCaptchaRequest>()))
            .ReturnsAsync(new CaptchaInfo(123, "Test Chat", DateTime.UtcNow, new User { Id = 456 }, 1, new CancellationTokenSource(), null));
        return this;
    }

    /// <summary>
    /// Настраивает мок для неудачной капчи
    /// <tags>builders, captcha-service, failure, fluent-api</tags>
    /// </summary>
    public CaptchaServiceMockBuilder ThatFails()
    {
        _mock.Setup(x => x.CreateCaptchaAsync(It.IsAny<CreateCaptchaRequest>()))
            .ReturnsAsync((CaptchaInfo?)null);
        return this;
    }

    /// <summary>
    /// Создает настроенный мок
    /// <tags>builders, captcha-service, build, fluent-api</tags>
    /// </summary>
    public Mock<ICaptchaService> Build() => _mock;

    /// <summary>
    /// Создает объект сервиса из мока
    /// <tags>builders, captcha-service, object, fluent-api</tags>
    /// </summary>
    public ICaptchaService BuildObject() => Build().Object;
}

/// <summary>
/// Билдер для мока IAiChecks
/// <tags>builders, ai-checks, mocks, fluent-api</tags>
/// </summary>
public class AiChecksMockBuilder
{
    private readonly Mock<IAiChecks> _mock = new();

    /// <summary>
    /// Настраивает мок для одобрения фото
    /// <tags>builders, ai-checks, photo-approval, fluent-api</tags>
    /// </summary>
    public AiChecksMockBuilder ThatApprovesPhoto()
    {
        _mock.Setup(x => x.GetSpamProbability(It.IsAny<Message>()))
            .ReturnsAsync(new SpamProbability { Probability = 0.1, Reason = "Approved" });
        return this;
    }

    /// <summary>
    /// Настраивает мок для отклонения фото
    /// <tags>builders, ai-checks, photo-rejection, fluent-api</tags>
    /// </summary>
    public AiChecksMockBuilder ThatRejectsPhoto()
    {
        _mock.Setup(x => x.GetSpamProbability(It.IsAny<Message>()))
            .ReturnsAsync(new SpamProbability { Probability = 0.9, Reason = "Rejected" });
        return this;
    }

    /// <summary>
    /// Создает настроенный мок
    /// <tags>builders, ai-checks, build, fluent-api</tags>
    /// </summary>
    public Mock<IAiChecks> Build() => _mock;

    /// <summary>
    /// Создает объект сервиса из мока
    /// <tags>builders, ai-checks, object, fluent-api</tags>
    /// </summary>
    public IAiChecks BuildObject() => Build().Object;
}

/// <summary>
/// Билдер для мока ITelegramBotClientWrapper
/// <tags>builders, telegram-bot, mocks, fluent-api</tags>
/// </summary>
public class TelegramBotMockBuilder
{
    private readonly Mock<ITelegramBotClientWrapper> _mock = new();

    /// <summary>
    /// Настраивает мок для успешной отправки сообщения
    /// <tags>builders, telegram-bot, send-message, fluent-api</tags>
    /// </summary>
    public TelegramBotMockBuilder ThatSendsMessageSuccessfully()
    {
        _mock.Setup(x => x.SendMessageAsync(
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
    /// Создает настроенный мок
    /// <tags>builders, telegram-bot, build, fluent-api</tags>
    /// </summary>
    public Mock<ITelegramBotClientWrapper> Build() => _mock;

    /// <summary>
    /// Создает объект сервиса из мока
    /// <tags>builders, telegram-bot, object, fluent-api</tags>
    /// </summary>
    public ITelegramBotClientWrapper BuildObject() => Build().Object;
} 
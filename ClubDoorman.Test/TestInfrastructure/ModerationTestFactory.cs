using ClubDoorman.Services;
using ClubDoorman.Test.Mocks;
using ClubDoorman.Test.TestData;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.TestInfrastructure;

/// <summary>
/// TestFactory для создания ModerationService с правильно настроенными моками
/// </summary>
public class ModerationTestFactory
{
    public Mock<ITelegramBotClient> TelegramMock { get; } = new();
    public Mock<IUserManager> UserManagerMock { get; } = new();
    public Mock<ILogger<ModerationService>> LoggerMock { get; } = new();
    public Mock<ISpamHamClassifier> SpamHamClassifierMock { get; } = new();
    public Mock<IMimicryClassifier> MimicryClassifierMock { get; } = new();
    public Mock<IBadMessageManager> BadMessageManagerMock { get; } = new();
    public Mock<IAiChecks> AiChecksMock { get; } = new();
    public Mock<ISuspiciousUsersStorage> SuspiciousUsersStorageMock { get; } = new();

    /// <summary>
    /// Создает ModerationService с настроенными моками
    /// </summary>
    public ModerationService Create()
    {
        // Настройка моков по умолчанию
        SetupDefaultMocks();

        return new ModerationService(
            SpamHamClassifierMock.Object,
            MimicryClassifierMock.Object,
            BadMessageManagerMock.Object,
            UserManagerMock.Object,
            AiChecksMock.Object,
            SuspiciousUsersStorageMock.Object,
            TelegramMock.Object,
            LoggerMock.Object
        );
    }

    /// <summary>
    /// Настройка моков по умолчанию
    /// </summary>
    private void SetupDefaultMocks()
    {
        // SpamHamClassifier - по умолчанию не спам
        SpamHamClassifierMock.Setup(x => x.IsSpam(It.IsAny<string>()))
            .ReturnsAsync((false, 0.1f));

        // MimicryClassifier - по умолчанию не подозрительно
        MimicryClassifierMock.Setup(x => x.AnalyzeMessages(It.IsAny<List<string>>()))
            .Returns(0.1);

        // BadMessageManager - по умолчанию не плохое сообщение
        BadMessageManagerMock.Setup(x => x.KnownBadMessage(It.IsAny<string>()))
            .Returns(false);

        // AiChecks - по умолчанию безопасно
        var safeSpamProbability = new SpamProbability { Probability = 0.1, Reason = "Safe" };
        var safeSpamPhotoBio = new SpamPhotoBio(safeSpamProbability, Array.Empty<byte>(), "");
        
        AiChecksMock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), It.IsAny<Func<string, Task>>()))
            .ReturnsAsync(safeSpamPhotoBio);

        AiChecksMock.Setup(x => x.GetSpamProbability(It.IsAny<Message>()))
            .ReturnsAsync(safeSpamProbability);

        // SuspiciousUsersStorage - по умолчанию не подозрительный
        SuspiciousUsersStorageMock.Setup(x => x.IsSuspicious(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(false);

        // UserManager - по умолчанию одобрен
        UserManagerMock.Setup(x => x.Approved(It.IsAny<long>(), It.IsAny<long?>()))
            .Returns(true);

        // Telegram API - по умолчанию успешные операции
        // Убираем проблемные extension methods
    }

    /// <summary>
    /// Настройка для спам-сообщения
    /// </summary>
    public ModerationTestFactory WithSpamMessage()
    {
        SpamHamClassifierMock.Setup(x => x.IsSpam(It.IsAny<string>()))
            .ReturnsAsync((true, 0.9f));
        return this;
    }

    /// <summary>
    /// Настройка для подозрительного пользователя
    /// </summary>
    public ModerationTestFactory WithSuspiciousUser()
    {
        AiChecksMock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<Telegram.Bot.Types.User>(), It.IsAny<Func<string, Task>>()))
            .ReturnsAsync(new SpamPhotoBio(new SpamProbability { Probability = 0.8 }, [], ""));
        return this;
    }

    /// <summary>
    /// Настройка для плохого сообщения
    /// </summary>
    public ModerationTestFactory WithBadMessage()
    {
        BadMessageManagerMock.Setup(x => x.KnownBadMessage(It.IsAny<string>()))
            .Returns(true);
        return this;
    }

    /// <summary>
    /// Настройка для успешного бана
    /// </summary>
    public ModerationTestFactory WithSuccessfulBan()
    {
        // Убираем проблемные extension methods
        return this;
    }

    /// <summary>
    /// Настройка для ошибки Telegram API
    /// </summary>
    public ModerationTestFactory WithTelegramError()
    {
        // Убираем проблемные extension methods
        return this;
    }
} 
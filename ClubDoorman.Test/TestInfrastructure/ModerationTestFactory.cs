using ClubDoorman.Models;
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Threading;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Фабрика для создания тестовых экземпляров ModerationService с правильно настроенными моками
/// </summary>
public class ModerationTestFactory
{
    public Mock<ITelegramBotClient> TelegramBotClient { get; }
    public Mock<IUserManager> UserManager { get; }
    public Mock<ILogger<ModerationService>> Logger { get; }
    public Mock<ISpamHamClassifier> SpamHamClassifier { get; }
    public Mock<IMimicryClassifier> MimicryClassifier { get; }
    public Mock<IBadMessageManager> BadMessageManager { get; }
    public Mock<IAiChecks> AiChecks { get; }
    public Mock<ISuspiciousUsersStorage> SuspiciousUsersStorage { get; }

    public ModerationTestFactory()
    {
        TelegramBotClient = new Mock<ITelegramBotClient>();
        UserManager = new Mock<IUserManager>();
        Logger = new Mock<ILogger<ModerationService>>();
        SpamHamClassifier = new Mock<ISpamHamClassifier>();
        MimicryClassifier = new Mock<IMimicryClassifier>();
        BadMessageManager = new Mock<IBadMessageManager>();
        AiChecks = new Mock<IAiChecks>();
        SuspiciousUsersStorage = new Mock<ISuspiciousUsersStorage>();

        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        // Настройка UserManager
        UserManager.Setup(x => x.InBanlist(It.IsAny<long>()))
            .ReturnsAsync(false);
        UserManager.Setup(x => x.Approved(It.IsAny<long>(), It.IsAny<long?>()))
            .Returns(true);

        // Настройка SpamHamClassifier - по умолчанию не спам (float, не double!)
        SpamHamClassifier.Setup(x => x.IsSpam(It.IsAny<string>()))
            .ReturnsAsync((false, -0.5f));

        // Настройка MimicryClassifier - по умолчанию не мимикрия (используем AnalyzeMessages)
        MimicryClassifier.Setup(x => x.AnalyzeMessages(It.IsAny<List<string>>()))
            .Returns(0.1);

        // Настройка BadMessageManager - по умолчанию не известное плохое сообщение
        BadMessageManager.Setup(x => x.KnownBadMessage(It.IsAny<string>()))
            .Returns(false);

        // Настройка AiChecks (правильные типы параметров)
        AiChecks.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), It.IsAny<Func<string, Task>>()))
            .ReturnsAsync(new SpamPhotoBio(new SpamProbability { Probability = 0.1 }, new byte[0], ""));

        // Настройка SuspiciousUsersStorage
        SuspiciousUsersStorage.Setup(x => x.IsSuspicious(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(false);

        // TelegramBotClient extension methods (BanChatMember, DeleteMessage) 
        // не могут быть замоканы через Moq, поэтому оставляем без настройки
        // Эти методы будут тестироваться в интеграционных тестах
    }

    /// <summary>
    /// Создает экземпляр ModerationService с настроенными моками
    /// </summary>
    public ModerationService CreateModerationService()
    {
        return new ModerationService(
            SpamHamClassifier.Object,
            MimicryClassifier.Object,
            BadMessageManager.Object,
            UserManager.Object,
            AiChecks.Object,
            SuspiciousUsersStorage.Object,
            TelegramBotClient.Object,
            Logger.Object
        );
    }

    /// <summary>
    /// Настраивает мок для спам-сообщения
    /// </summary>
    public void SetupSpamMessage()
    {
        SpamHamClassifier.Setup(x => x.IsSpam(It.IsAny<string>()))
            .ReturnsAsync((true, 0.9f));
    }

    /// <summary>
    /// Настраивает мок для известного плохого сообщения
    /// </summary>
    public void SetupBadMessage()
    {
        BadMessageManager.Setup(x => x.KnownBadMessage(It.IsAny<string>()))
            .Returns(true);
    }

    /// <summary>
    /// Настраивает мок для пользователя в блэклисте
    /// </summary>
    public void SetupBannedUser()
    {
        UserManager.Setup(x => x.InBanlist(It.IsAny<long>()))
            .ReturnsAsync(true);
    }

    /// <summary>
    /// Настраивает мок для исключения в классификаторе
    /// </summary>
    public void SetupClassifierException()
    {
        SpamHamClassifier.Setup(x => x.IsSpam(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Classifier error"));
    }
} 
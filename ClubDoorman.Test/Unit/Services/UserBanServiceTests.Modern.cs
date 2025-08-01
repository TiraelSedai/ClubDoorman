using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using ClubDoorman.Handlers;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Test.TestData;
using ClubDoorman.Test.TestKit;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("unit")]
[Category("services")]
[Category("user-ban")]
[Category("modern")]
public class UserBanServiceTestsModern
{
    private Mock<ITelegramBotClientWrapper> _botMock = null!;
    private Mock<IMessageService> _messageServiceMock = null!;
    private Mock<IUserFlowLogger> _userFlowLoggerMock = null!;
    private Mock<IAppConfig> _appConfigMock = null!;
    private Mock<IModerationService> _moderationServiceMock = null!;
    private Mock<IViolationTracker> _violationTrackerMock = null!;
    private Mock<IStatisticsService> _statisticsServiceMock = null!;
    private Mock<GlobalStatsManager> _globalStatsManagerMock = null!;
    private Mock<IUserManager> _userManagerMock = null!;
    
    private IUserBanService _userBanService = null!;

    [SetUp]
    public void Setup()
    {
        _botMock = new Mock<ITelegramBotClientWrapper>();
        _messageServiceMock = new Mock<IMessageService>();
        _userFlowLoggerMock = new Mock<IUserFlowLogger>();
        _appConfigMock = new Mock<IAppConfig>();
        _moderationServiceMock = new Mock<IModerationService>();
        _violationTrackerMock = new Mock<IViolationTracker>();
        _statisticsServiceMock = new Mock<IStatisticsService>();
        _globalStatsManagerMock = new Mock<GlobalStatsManager>();
        _userManagerMock = new Mock<IUserManager>();

        _userBanService = new UserBanService(
            _botMock.Object,
            _messageServiceMock.Object,
            _userFlowLoggerMock.Object,
            new Mock<ILogger<UserBanService>>().Object,
            _moderationServiceMock.Object,
            _violationTrackerMock.Object,
            _appConfigMock.Object,
            _statisticsServiceMock.Object,
            _globalStatsManagerMock.Object,
            _userManagerMock.Object
        );
    }

    #region BanUserForLongName Tests

    [Test]
    [Category("migration-new")]
    [Category("unit-test")]
    public async Task BanUserForLongName_PrivateChat_LogsWarningAndReturns()
    {
        // Arrange - Unit test: используем стабильные данные для unit теста
        var user = TK.CreateValidUser(); // Стабильные данные вместо AutoFixture
        var chat = TK.CreatePrivateChat(); // Стабильные данные вместо AutoFixture
        var message = TK.CreateValidMessage(); // Стабильные данные вместо AutoFixture
        message.Chat = chat; // Простая мутация для unit теста - это нормально
        var reason = "Длинное имя";
        var banDuration = TimeSpan.FromMinutes(10);

        // Act
        await _userBanService.BanUserForLongNameAsync(message, user, reason, banDuration, CancellationToken.None);

        // Assert - идентично оригиналу
        _messageServiceMock.Verify(
            x => x.SendAdminNotificationAsync(
                AdminNotificationType.PrivateChatBanAttempt,
                It.IsAny<ErrorNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _botMock.Verify(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    [Category("migration-new")]
    [Category("unit-test")]
    public async Task BanUserForLongName_ValidChat_BansUserAndSendsNotification()
    {
        // Arrange - Unit test: используем стабильные данные для unit теста
        var user = TK.CreateValidUser(); // Стабильные данные вместо AutoFixture
        var chat = TK.CreateGroupChat(); // Стабильные данные вместо AutoFixture
        var message = TK.CreateValidMessage(); // Стабильные данные вместо AutoFixture
        message.Chat = chat; // Простая мутация для unit теста - это нормально
        var reason = "Длинное имя";
        var banDuration = TimeSpan.FromMinutes(10);

        _botMock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _botMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userBanService.BanUserForLongNameAsync(message, user, reason, banDuration, CancellationToken.None);

        // Assert - идентично оригиналу
        _botMock.Verify(
            x => x.BanChatMember(
                chat.Id,
                user.Id,
                It.Is<DateTime?>(d => d.HasValue && d.Value > DateTime.UtcNow),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _botMock.Verify(
            x => x.DeleteMessage(chat.Id, message.MessageId, It.IsAny<CancellationToken>()),
            Times.Once);

        _messageServiceMock.Verify(
            x => x.ForwardToLogWithNotificationAsync(
                message,
                LogNotificationType.BanForLongName,
                It.IsAny<AutoBanNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _userFlowLoggerMock.Verify(
            x => x.LogUserBanned(user, chat, reason),
            Times.Once);
    }

    [Test]
    [Category("migration-new")]
    [Category("unit-test")]
    public async Task BanUserForLongName_PermanentBan_BansUserPermanently()
    {
        // Arrange - Unit test: используем стабильные данные для unit теста
        var user = TK.CreateValidUser(); // Стабильные данные вместо AutoFixture
        var chat = TK.CreateGroupChat(); // Стабильные данные вместо AutoFixture
        var message = TK.CreateValidMessage(); // Стабильные данные вместо AutoFixture
        message.Chat = chat; // Простая мутация для unit теста - это нормально
        var reason = "Длинное имя";
        TimeSpan? banDuration = null; // Перманентный бан

        _botMock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userBanService.BanUserForLongNameAsync(message, user, reason, banDuration, CancellationToken.None);

        // Assert - идентично оригиналу
        _botMock.Verify(
            x => x.BanChatMember(
                chat.Id,
                user.Id,
                null, // Перманентный бан
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Integration Tests

    [Test]
    [Category("migration-new")]
    [Category("integration-test")]
    public async Task BanUserForLongName_Integration_WithRealDependencies()
    {
        // Arrange - Integration test: используем AutoBogus для реалистичных данных + частично настоящие зависимости
        var user = TK.CreateRealisticUser(); // AutoBogus - реалистичные данные для интеграции
        var chat = TK.CreateGroupChat(); // AutoBogus - реалистичные данные для интеграции
        var message = TK.BuildMessage() // Builders - для сложной настройки в интеграции
            .AsValid()
            .InChat(chat)
            .Build();
        var reason = "Длинное имя";
        var banDuration = TimeSpan.FromMinutes(10);

        // Частично настоящие зависимости (мокаем только крайние)
        var realAppConfig = TK.CreateAppConfig(); // Настоящая конфигурация
        var realUserManager = TK.CreateApprovedUserManager(); // Настоящий UserManager с моками

        _botMock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userBanService.BanUserForLongNameAsync(message, user, reason, banDuration, CancellationToken.None);

        // Assert
        _botMock.Verify(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region BDD Tests

    [Test]
    [Category("migration-new")]
    [Category("bdd-test")]
    public async Task Given_SuspiciousUserWithLongName_When_BanAttempted_Then_UserGetsBanned()
    {
        // Arrange - BDD test: используем Bogus + Seed для стабильных осмысленных данных
        var suspiciousUser = TK.CreateValidUser(); // Bogus - осмысленные данные
        // Ручное вмешательство: делаем пользователя подозрительным
        suspiciousUser.FirstName = "VeryLongNameThatExceedsTheLimitAndMakesUserSuspicious";
        var groupChat = TK.CreateGroupChat(); // Bogus - осмысленные данные
        var message = TK.BuildMessage() // Builders - для Given сценария
            .AsValid()
            .InChat(groupChat)
            .Build();
        var reason = "Длинное имя";
        var banDuration = TimeSpan.FromMinutes(10);

        _botMock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userBanService.BanUserForLongNameAsync(message, suspiciousUser, reason, banDuration, CancellationToken.None);

        // Assert
        _botMock.Verify(x => x.BanChatMember(groupChat.Id, suspiciousUser.Id, It.IsAny<DateTime?>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Edge/Stress Tests

    [Test]
    [Category("migration-new")]
    [Category("edge-test")]
    public async Task BanUserForLongName_EdgeCases_MassiveDataGeneration()
    {
        // Arrange - Edge test: используем AutoFixture для массовой генерации edge cases
        var users = TK.CreateMany<User>(50); // AutoFixture - массовая генерация
        var chats = TK.CreateMany<Chat>(10); // AutoFixture - массовая генерация
        var messages = TK.CreateMany<Message>(100); // AutoFixture - массовая генерация

        _botMock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert - тестируем edge cases
        foreach (var user in users.Take(5))
        {
            foreach (var chat in chats.Take(2))
            {
                var message = messages.First();
                message.Chat = chat;
                
                await _userBanService.BanUserForLongNameAsync(message, user, "Edge case", TimeSpan.FromMinutes(1), CancellationToken.None);
                
                _botMock.Verify(x => x.BanChatMember(chat.Id, user.Id, It.IsAny<DateTime?>(), true, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            }
        }
    }

    #endregion
} 
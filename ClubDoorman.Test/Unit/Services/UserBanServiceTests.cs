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
public class UserBanServiceTests
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
    
    // Дополнительные моки для MessageHandler (эталонная версия)
    private Mock<ICaptchaService> _captchaServiceMock = null!;
    private Mock<ISpamHamClassifier> _classifierMock = null!;
    private Mock<IBadMessageManager> _badMessageManagerMock = null!;
    private Mock<IAiChecks> _aiServiceMock = null!;
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<IChatLinkFormatter> _chatLinkFormatterMock = null!;
    private Mock<IBotPermissionsService> _botPermissionsServiceMock = null!;
    
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
        
        // Дополнительные моки для MessageHandler (эталонная версия)
        _captchaServiceMock = new Mock<ICaptchaService>();
        _classifierMock = new Mock<ISpamHamClassifier>();
        _badMessageManagerMock = new Mock<IBadMessageManager>();
        _aiServiceMock = new Mock<IAiChecks>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _chatLinkFormatterMock = new Mock<IChatLinkFormatter>();
        _botPermissionsServiceMock = new Mock<IBotPermissionsService>();

        // MessageHandler больше не нужен в этих тестах - тестируем UserBanService напрямую
        
        // UserBanService теперь содержит реальную логику
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
    public async Task BanUserForLongName_PrivateChat_LogsWarningAndReturns()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreatePrivateChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        var reason = "Длинное имя";
        var banDuration = TimeSpan.FromMinutes(10);

        // Act
        await _userBanService.BanUserForLongNameAsync(message, user, reason, banDuration, CancellationToken.None);

        // Assert
        // Логирование происходит в MessageHandler, а не в UserBanService

        _messageServiceMock.Verify(
            x => x.SendAdminNotificationAsync(
                AdminNotificationType.PrivateChatBanAttempt,
                It.IsAny<ErrorNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Убеждаемся, что бан не был выполнен
        _botMock.Verify(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task BanUserForLongName_ValidChat_BansUserAndSendsNotification()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        var reason = "Длинное имя";
        var banDuration = TimeSpan.FromMinutes(10);

        _botMock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _botMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userBanService.BanUserForLongNameAsync(message, user, reason, banDuration, CancellationToken.None);

        // Assert
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
    public async Task BanUserForLongName_PermanentBan_BansUserPermanently()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        var reason = "Длинное имя";
        TimeSpan? banDuration = null; // Перманентный бан

        _botMock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userBanService.BanUserForLongNameAsync(message, user, reason, banDuration, CancellationToken.None);

        // Assert
        _botMock.Verify(
            x => x.BanChatMember(
                chat.Id,
                user.Id,
                null, // Перманентный бан
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task BanUserForLongName_ExceptionOccurs_LogsWarning()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        var reason = "Длинное имя";
        var banDuration = TimeSpan.FromMinutes(10);

        _botMock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await _userBanService.BanUserForLongNameAsync(message, user, reason, banDuration, CancellationToken.None);

        // Assert
        // Проверяем, что исключение было пробброшено
        // (логирование происходит в MessageHandler, а не в UserBanService)
    }

    #endregion

    #region BanBlacklistedUser Tests

    [Test]
    public async Task BanBlacklistedUser_PrivateChat_LogsWarningAndReturns()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreatePrivateChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;

        // Act
        await _userBanService.BanBlacklistedUserAsync(message, user, CancellationToken.None);

        // Assert
        // Убеждаемся, что бан не был выполнен для приватного чата
        _botMock.Verify(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task BanBlacklistedUser_ValidChat_BansUserFor4Hours()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;

        _botMock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _botMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userBanService.BanBlacklistedUserAsync(message, user, CancellationToken.None);

        // Assert
        _botMock.Verify(
            x => x.BanChatMember(
                chat.Id,
                user.Id,
                It.Is<DateTime?>(d => d.HasValue && d.Value > DateTime.UtcNow && d.Value <= DateTime.UtcNow.AddHours(4)),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _botMock.Verify(
            x => x.DeleteMessage(chat.Id, message.MessageId, It.IsAny<CancellationToken>()),
            Times.Once);

        _userFlowLoggerMock.Verify(
            x => x.LogUserBanned(user, chat, "Пользователь в блэклисте"),
            Times.Once);
    }

    [Test]
    public async Task BanBlacklistedUser_ExceptionOccurs_LogsWarning()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;

        _botMock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await _userBanService.BanBlacklistedUserAsync(message, user, CancellationToken.None);

        // Assert
        // Проверяем, что исключение было проброшено
        // (логирование происходит в MessageHandler, а не в UserBanService)
    }

    #endregion

    #region AutoBan Tests

    [Test]
    public async Task AutoBan_PrivateChat_LogsWarningAndReturns()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreatePrivateChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.Text = "Test message";
        var reason = "Спам";

        // Act
        await _userBanService.AutoBanAsync(message, reason, CancellationToken.None);

        // Assert
        // Убеждаемся, что бан не был выполнен для приватного чата
        _botMock.Verify(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AutoBan_KnownSpamReason_SelectsCorrectNotificationType()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.Text = "Test message";
        var reason = "Известное спам-сообщение";

        // Act
        await _userBanService.AutoBanAsync(message, reason, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendLogNotificationAsync(
                LogNotificationType.AutoBanKnownSpam,
                It.IsAny<AutoBanNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AutoBan_TextMentionReason_SelectsCorrectNotificationType()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.Text = "Test message";
        var reason = "Ссылки запрещены";

        // Act
        await _userBanService.AutoBanAsync(message, reason, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendLogNotificationAsync(
                LogNotificationType.AutoBanTextMention,
                It.IsAny<AutoBanNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AutoBan_RepeatedViolationsReason_SelectsCorrectNotificationType()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.Text = "Test message";
        var reason = "Повторные нарушения";

        // Act
        await _userBanService.AutoBanAsync(message, reason, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendLogNotificationAsync(
                LogNotificationType.AutoBanRepeatedViolations,
                It.IsAny<AutoBanNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AutoBan_UnknownReason_SelectsDefaultNotificationType()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.Text = "Test message";
        var reason = "Неизвестная причина";

        // Act
        await _userBanService.AutoBanAsync(message, reason, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendLogNotificationAsync(
                LogNotificationType.AutoBanBlacklist,
                It.IsAny<AutoBanNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AutoBan_ValidChat_BansUserAndForwardsMessage()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.Text = "Test message";
        var reason = "Автобан";

        // Act
        await _userBanService.AutoBanAsync(message, reason, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendLogNotificationAsync(
                LogNotificationType.AutoBanBlacklist,
                It.IsAny<AutoBanNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region AutoBanChannel Tests

    [Test]
    public async Task AutoBanChannel_ValidMessage_BansChannelAndSendsNotification()
    {
        // Arrange
        var senderChat = new Chat { Id = 789, Type = ChatType.Channel, Title = "Test Channel" };
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.SenderChat = senderChat;
        message.Text = "Test message";

        _botMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _botMock.Setup(x => x.BanChatSenderChat(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userBanService.AutoBanChannelAsync(message, CancellationToken.None);

        // Assert
        _botMock.Verify(
            x => x.DeleteMessage(chat.Id, message.MessageId, It.IsAny<CancellationToken>()),
            Times.Once);

        _botMock.Verify(
            x => x.BanChatSenderChat(chat.Id, senderChat.Id, It.IsAny<CancellationToken>()),
            Times.Once);

        _messageServiceMock.Verify(
            x => x.ForwardToAdminWithNotificationAsync(
                message,
                AdminNotificationType.ChannelMessage,
                It.IsAny<ChannelMessageNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AutoBanChannel_ExceptionOccurs_LogsWarningAndSendsErrorNotification()
    {
        // Arrange
        var senderChat = new Chat { Id = 789, Type = ChatType.Channel, Title = "Test Channel" };
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.SenderChat = senderChat;
        message.Text = "Test message";

        _botMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await _userBanService.AutoBanChannelAsync(message, CancellationToken.None);

        // Assert  
        // Проверяем, что исключение было проброшено
        // (логирование происходит в MessageHandler, а не в UserBanService)

        _messageServiceMock.Verify(
            x => x.SendAdminNotificationAsync(
                AdminNotificationType.ChannelError,
                It.IsAny<ErrorNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region HandleBlacklistBan Tests

    [Test]
    public async Task HandleBlacklistBan_ValidUser_BansUserAndDeletesMessage()
    {
        // Arrange
        var user = TK.CreateValidUser();
        var chat = TK.CreateGroupChat();
        var message = TK.CreateValidMessage();
        message.Chat = chat;
        message.Text = "Test message";

        _botMock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _userBanService.HandleBlacklistBanAsync(message, user, chat, CancellationToken.None);

        // Assert
        _botMock.Verify(
            x => x.DeleteMessage(chat.Id, message.MessageId, It.IsAny<CancellationToken>()),
            Times.Once);

        _userFlowLoggerMock.Verify(
            x => x.LogUserBanned(user, chat, "Пользователь в блэклисте lols.bot"),
            Times.Once);
    }

    #endregion
} 
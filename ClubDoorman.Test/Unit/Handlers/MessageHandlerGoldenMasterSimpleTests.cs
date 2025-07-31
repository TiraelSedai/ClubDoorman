using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Handlers;
using ClubDoorman.Test.TestKit;
using ClubDoorman.TestInfrastructure;
using NUnit.Framework;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Упрощенные Golden Master тесты для логики банов
/// Используют вспомогательный класс TestKitGoldenMaster для упрощения
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
[Category(TestCategories.Critical)]
[Category(TestCategories.GoldenMaster)]
public class MessageHandlerGoldenMasterSimpleTests
{
    private MessageHandler _messageHandler;
    private MessageHandlerTestFactory _factory;
    
    [SetUp]
    public void Setup()
    {
        // Создаем MessageHandler с Golden Master моками
        _factory = TK.CreateMessageHandlerFactory()
            .SetupGoldenMasterMocks();
            
        _messageHandler = _factory.CreateMessageHandler();
    }

    /// <summary>
    /// Простой Golden Master тест для временного бана
    /// </summary>
    [Test]
    public async Task BanUserForLongName_TemporaryBan_SimpleGoldenMaster()
    {
        // Arrange
        var scenario = TestKitGoldenMaster.CreateStandardBanScenario(
            userId: 11111,
            chatId: -100111111111,
            firstName: "TemporaryBanUser",
            username: "tempbanuser",
            messageText: "I have a very long name that should trigger temporary ban"
        );

        // Act
        await _messageHandler.BanUserForLongName(
            scenario.Message, 
            scenario.User, 
            "Временный бан за длинное имя", 
            TimeSpan.FromMinutes(30), 
            CancellationToken.None);

        // Assert: Golden Master snapshot
        await TestKitGoldenMaster.CreateBanScenarioSnapshot(
            scenario,
            TimeSpan.FromMinutes(30),
            "Временный бан за длинное имя",
            "BanUserForLongName_TemporaryBan_SimpleGoldenMaster",
            "TemporaryBan_Simple");
    }

    /// <summary>
    /// Простой Golden Master тест для перманентного бана
    /// </summary>
    [Test]
    public async Task BanUserForLongName_PermanentBan_SimpleGoldenMaster()
    {
        // Arrange
        var scenario = TestKitGoldenMaster.CreateStandardBanScenario(
            userId: 22222,
            chatId: -100222222222,
            firstName: "PermanentBanUser",
            username: "permbanuser",
            messageText: "I have an extremely long name that should trigger permanent ban"
        );

        // Act: Перманентный бан (banDuration = null)
        await _messageHandler.BanUserForLongName(
            scenario.Message, 
            scenario.User, 
            "Перманентный бан за длинное имя", 
            null, 
            CancellationToken.None);

        // Assert: Golden Master snapshot
        await TestKitGoldenMaster.CreateBanScenarioSnapshot(
            scenario,
            null,
            "Перманентный бан за длинное имя",
            "BanUserForLongName_PermanentBan_SimpleGoldenMaster",
            "PermanentBan_Simple");
    }

    /// <summary>
    /// Простой Golden Master тест для приватного чата
    /// </summary>
    [Test]
    public async Task BanUserForLongName_PrivateChat_SimpleGoldenMaster()
    {
        // Arrange
        var scenario = TestKitGoldenMaster.CreateStandardBanScenario(
            userId: 33333,
            chatId: 333333333,
            firstName: "PrivateChatUser",
            username: "privateuser",
            chatType: ChatType.Private,
            chatTitle: "Private Chat",
            messageText: "Message in private chat"
        );

        // Act
        await _messageHandler.BanUserForLongName(
            scenario.Message, 
            scenario.User, 
            "Попытка бана в приватном чате", 
            TimeSpan.FromMinutes(10), 
            CancellationToken.None);

        // Assert: Golden Master snapshot для приватного чата
        var input = new
        {
            User = new { Id = scenario.User.Id, FirstName = scenario.User.FirstName, Username = scenario.User.Username },
            Chat = new { Id = scenario.Chat.Id, Type = scenario.Chat.Type.ToString(), Title = scenario.Chat.Title },
            Message = new { Id = scenario.Message!.MessageId, Text = scenario.Message.Text },
            BanDuration = 10.0,
            Reason = "Попытка бана в приватном чате"
        };

        var expectedBehavior = new
        {
            ShouldNotCallBanChatMember = true,
            ShouldLogWarning = true,
            ShouldSendAdminNotification = true,
            WarningMessage = "Попытка бана за длинное имя в приватном чате 333333333 - операция невозможна"
        };

        await TestKitGoldenMaster.CreateGoldenMasterSnapshot(
            "BanUserForLongName_PrivateChat_SimpleGoldenMaster",
            input,
            expectedBehavior,
            "PrivateChat_Simple");
    }

    /// <summary>
    /// Простой Golden Master тест для бана без сообщения
    /// </summary>
    [Test]
    public async Task BanUserForLongName_NullMessage_SimpleGoldenMaster()
    {
        // Arrange
        var scenario = TestKitGoldenMaster.CreateStandardBanScenario(
            userId: 44444,
            chatId: -100444444444,
            firstName: "NullMessageUser",
            username: "nullmsguser"
        );

        // Act: Передаем null в качестве userJoinMessage
        await _messageHandler.BanUserForLongName(
            null, 
            scenario.User, 
            "Бан без исходного сообщения", 
            TimeSpan.FromMinutes(15), 
            CancellationToken.None);

        // Assert: Golden Master snapshot для null сообщения
        var input = new
        {
            User = new { Id = scenario.User.Id, FirstName = scenario.User.FirstName, Username = scenario.User.Username },
            Chat = new { Id = scenario.Chat.Id, Type = scenario.Chat.Type.ToString(), Title = scenario.Chat.Title },
            Message = (object)null,
            BanDuration = 15.0,
            Reason = "Бан без исходного сообщения"
        };

        var expectedBehavior = new
        {
            ShouldCallBanChatMember = true,
            ShouldNotCallDeleteMessage = true,
            ShouldCallSendLogNotification = true,
            BanType = "Автобан на 15 минут"
        };

        await TestKitGoldenMaster.CreateGoldenMasterSnapshot(
            "BanUserForLongName_NullMessage_SimpleGoldenMaster",
            input,
            expectedBehavior,
            "NullMessage_Simple");
    }

    /// <summary>
    /// Простой Golden Master тест для обработки исключений
    /// </summary>
    [Test]
    public async Task BanUserForLongName_ExceptionHandling_SimpleGoldenMaster()
    {
        // Arrange
        var scenario = TestKitGoldenMaster.CreateStandardBanScenario(
            userId: 55555,
            chatId: -100555555555,
            firstName: "ExceptionUser",
            username: "exceptionuser",
            messageText: "Message that will cause exception"
        );

        // Настраиваем исключение
        var factoryWithException = _factory.SetupExceptionScenario(
            new InvalidOperationException("Bot API error during ban"));
        var messageHandlerWithException = factoryWithException.CreateMessageHandler();

        // Act
        await messageHandlerWithException.BanUserForLongName(
            scenario.Message, 
            scenario.User, 
            "Тест обработки исключений", 
            TimeSpan.FromMinutes(20), 
            CancellationToken.None);

        // Assert: Golden Master snapshot для обработки исключений
        var input = new
        {
            User = new { Id = scenario.User.Id, FirstName = scenario.User.FirstName, Username = scenario.User.Username },
            Chat = new { Id = scenario.Chat.Id, Type = scenario.Chat.Type.ToString(), Title = scenario.Chat.Title },
            Message = new { Id = scenario.Message!.MessageId, Text = scenario.Message.Text },
            BanDuration = 20.0,
            Reason = "Тест обработки исключений"
        };

        var expectedExceptionHandling = new
        {
            ShouldLogWarning = true,
            WarningMessage = "Не удалось забанить пользователя за длинное имя",
            ExceptionType = "InvalidOperationException",
            ExceptionMessage = "Bot API error during ban"
        };

        await TestKitGoldenMaster.CreateGoldenMasterSnapshot(
            "BanUserForLongName_ExceptionHandling_SimpleGoldenMaster",
            input,
            expectedExceptionHandling,
            "ExceptionHandling_Simple");
    }

    /// <summary>
    /// Простой Golden Master тест для бана черного списка
    /// </summary>
    [Test]
    public async Task BanBlacklistedUser_SimpleGoldenMaster()
    {
        // Arrange
        var scenario = TestKitGoldenMaster.CreateStandardBanScenario(
            userId: 66666,
            chatId: -100666666666,
            firstName: "BlacklistedUser",
            username: "blacklisteduser",
            messageText: "Message from blacklisted user"
        );

        // Act
        await _messageHandler.BanBlacklistedUser(scenario.Message, scenario.User, CancellationToken.None);

        // Assert: Golden Master snapshot для черного списка
        var input = new
        {
            User = new { Id = scenario.User.Id, FirstName = scenario.User.FirstName, Username = scenario.User.Username },
            Chat = new { Id = scenario.Chat.Id, Type = scenario.Chat.Type.ToString(), Title = scenario.Chat.Title },
            Message = new { Id = scenario.Message!.MessageId, Text = scenario.Message.Text }
        };

        var expectedBehavior = new
        {
            ShouldCallBanChatMember = true,
            ShouldCallDeleteMessage = true,
            ShouldCallForwardToLogWithNotification = true,
            LogNotificationType = "BanForBlacklist",
            BanType = "🚫 Перманентный бан",
            Reason = "Пользователь в черном списке"
        };

        await TestKitGoldenMaster.CreateGoldenMasterSnapshot(
            "BanBlacklistedUser_SimpleGoldenMaster",
            input,
            expectedBehavior,
            "BlacklistedUser_Simple");
    }

    /// <summary>
    /// Простой Golden Master тест для автоматического бана канала
    /// </summary>
    [Test]
    public async Task AutoBanChannel_SimpleGoldenMaster()
    {
        // Arrange
        var user = TestKitBuilders.CreateUser()
            .WithId(77777)
            .WithFirstName("ChannelBot")
            .WithUsername("channelbot")
            .AsBot()
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(-100777777777)
            .WithType(ChatType.Supergroup)
            .WithTitle("Group With Channel Ban")
            .Build();

        var message = TestKitBuilders.CreateMessage()
            .FromUser(user)
            .InChat(chat)
            .WithMessageId(77777)
            .WithText("Message from channel bot")
            .Build();

        // Act
        await _messageHandler.AutoBanChannel(message, CancellationToken.None);

        // Assert: Golden Master snapshot для бана канала
        var input = new
        {
            User = new { Id = user.Id, FirstName = user.FirstName, Username = user.Username, IsBot = user.IsBot },
            Chat = new { Id = chat.Id, Type = chat.Type.ToString(), Title = chat.Title },
            Message = new { Id = message.MessageId, Text = message.Text }
        };

        var expectedBehavior = new
        {
            ShouldCallBanChatMember = true,
            ShouldCallDeleteMessage = true,
            ShouldCallForwardToLogWithNotification = true,
            LogNotificationType = "BanForChannel",
            BanType = "🚫 Перманентный бан",
            Reason = "Автоматический бан канала"
        };

        await TestKitGoldenMaster.CreateGoldenMasterSnapshot(
            "AutoBanChannel_SimpleGoldenMaster",
            input,
            expectedBehavior,
            "AutoBanChannel_Simple");
    }
} 
using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Handlers;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using ClubDoorman.Test.TestKit;
using ClubDoorman.TestInfrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Тесты для покрытия критических строк, выявленных Mutation Testing
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
[Category(TestCategories.Critical)]
public class MessageHandlerMutationCoverageTests
{
    private MessageHandler _messageHandler;
    private Mock<ITelegramBotClientWrapper> _botMock;
    private Mock<IMessageService> _messageServiceMock;
    private Mock<ILogger<MessageHandler>> _loggerMock;
    private MessageHandlerTestFactory _factory;
    
    [SetUp]
    public void Setup()
    {
        // Создаем MessageHandler с моками, используя тегированную систему TestKit
        _factory = TK.CreateMessageHandlerFactory()
            .WithBotSetup(mock => _botMock = mock)
            .WithMessageServiceSetup(mock => _messageServiceMock = mock)
            .WithLoggerSetup(mock => _loggerMock = mock);
            
        _messageHandler = _factory.CreateMessageHandler();
    }

    /// <summary>
    /// Тест для покрытия строк 856-857: Exception handling при определении типа чата
    /// Мутант: Boolean mutation не покрыт - строка 857: return false
    /// </summary>
    [Test]
    [Category(TestCategories.MutationCoverage)]
    public async Task BanUserForLongName_WhenChatTypeCheckThrowsException_ShouldReturnFalseAndLogWarning()
    {
        // Arrange
        var user = TestKitBuilders.CreateUser()
            .WithFirstName("VeryLongNameThatExceeds64CharactersAndTriggersLongNameBanLogic")
            .Build();
        var chat = TestKitBuilders.CreateChat()
            .WithType(ChatType.Supergroup)
            .Build();
        var message = TestKitBuilders.CreateMessage()
            .FromUser(user)
            .InChat(chat)
            .Build();
        
        // Настраиваем мок чата для выброса исключения при проверке типа
        var problematicChat = new Mock<Chat>();
        // Имитируем ошибку при доступе к свойству Type
        problematicChat.Setup(x => x.Type).Throws(new InvalidOperationException("Chat type access error"));
        problematicChat.Setup(x => x.Id).Returns(chat.Id);
        
        var messageWithProblematicChat = TestKitBuilders.CreateMessage()
            .FromUser(user)
            .InChat(problematicChat.Object)
            .Build();

        // Act & Assert
        // Вызываем internal метод - он не должен выбрасывать исключение
        await _messageHandler.Invoking(h => 
                h.BanUserForLongName(messageWithProblematicChat, user, "Test ban", 
                    TimeSpan.FromMinutes(10), CancellationToken.None))
            .Should().NotThrowAsync();

        // Verify: должно быть залогировано предупреждение (строка 856-857)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Не удалось определить тип чата")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once,
            "Должно быть залогировано предупреждение при ошибке определения типа чата");
    }

    /// <summary>
    /// Тест для покрытия строки 870: LogWarning при попытке бана в приватном чате
    /// Мутант: Statement mutation - строка 870 может быть удалена без провала тестов
    /// </summary>
    [Test]
    [Category(TestCategories.MutationCoverage)]
    public async Task BanUserForLongName_WhenPrivateChat_ShouldLogWarningAndSendAdminNotification()
    {
        // Arrange
        var user = TestKitBuilders.CreateUser()
            .WithFirstName("VeryLongNameThatExceeds64CharactersAndTriggersLongNameBanLogic")
            .Build();
        var privateChat = TestKitBuilders.CreateChat()
            .WithType(ChatType.Private)
            .Build();
        var message = TestKitBuilders.CreateMessage()
            .FromUser(user)
            .InChat(privateChat)
            .Build();

        // Act
        await _messageHandler.BanUserForLongName(message, user, "Long name ban", TimeSpan.FromMinutes(10), CancellationToken.None);

        // Assert: Проверяем логирование предупреждения (строка 870)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Попытка бана за длинное имя в приватном чате") && v.ToString().Contains("операция невозможна")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once,
            "Должно быть залогировано предупреждение о попытке бана в приватном чате");

        // Assert: Проверяем отправку уведомления администратору
        _messageServiceMock.Verify(
            x => x.SendAdminNotificationAsync(
                AdminNotificationType.PrivateChatBanAttempt,
                It.IsAny<ErrorNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть отправлено уведомление администратору о попытке бана в приватном чате");

        // Assert: НЕ должно быть вызова бана 
        _botMock.Verify(
            x => x.BanChatMember(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<DateTime?>(), true, It.IsAny<CancellationToken>()),
            Times.Never,
            "Не должно быть вызова BanChatMember для приватного чата");
    }

    /// <summary>
    /// Тест для покрытия строки 893: Условное определение типа бана (временный vs перманентный)
    /// Мутант: Conditional (true) mutation - строка 893 может всегда возвращать одно значение
    /// </summary>
    [Test]
    [Category(TestCategories.MutationCoverage)]
    public async Task BanUserForLongName_WhenTemporaryBan_ShouldUseCorrectBanTypeMessage()
    {
        // Arrange
        var user = TestKitBuilders.CreateUser()
            .WithFirstName("VeryLongNameThatExceeds64CharactersAndTriggersLongNameBanLogic")
            .Build();
        var chat = TestKitBuilders.CreateChat()
            .WithType(ChatType.Supergroup)
            .Build();
        var message = TestKitBuilders.CreateMessage()
            .FromUser(user)
            .InChat(chat)
            .Build();

        // Act: Временный бан (с указанием времени)
        await _messageHandler.BanUserForLongName(message, user, "Long name ban", TimeSpan.FromMinutes(10), CancellationToken.None);

        // Assert: Проверяем, что использован правильный тип бана для временного (строка 893)
        _messageServiceMock.Verify(
            x => x.ForwardToLogWithNotificationAsync(
                It.IsAny<Message>(),
                LogNotificationType.BanForLongName,
                It.Is<AutoBanNotificationData>(data => data.BanType == "Автобан на 10 минут"),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Для временного бана должен использоваться тип 'Автобан на 10 минут'");
    }

    /// <summary>
    /// Тест для покрытия строки 893: Условное определение типа бана (перманентный)
    /// </summary>
    [Test]
    [Category(TestCategories.MutationCoverage)]
    public async Task BanUserForLongName_WhenPermanentBan_ShouldUseCorrectBanTypeMessage()
    {
        // Arrange
        var user = TestKitBuilders.CreateUser()
            .WithFirstName("VeryLongNameThatExceeds64CharactersAndTriggersLongNameBanLogic")
            .Build();
        var chat = TestKitBuilders.CreateChat()
            .WithType(ChatType.Supergroup)
            .Build();
        var message = TestKitBuilders.CreateMessage()
            .FromUser(user)
            .InChat(chat)
            .Build();

        // Act: Перманентный бан (без указания времени)
        await _messageHandler.BanUserForLongName(message, user, "Long name ban", null, CancellationToken.None);

        // Assert: Проверяем, что использован правильный тип бана для перманентного (строка 893)
        _messageServiceMock.Verify(
            x => x.ForwardToLogWithNotificationAsync(
                It.IsAny<Message>(),
                LogNotificationType.BanForLongName,
                It.Is<AutoBanNotificationData>(data => data.BanType == "🚫 Перманентный бан"),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Для перманентного бана должен использоваться тип '🚫 Перманентный бан'");
    }

    /// <summary>
    /// Тест для покрытия строки 903: SendLogNotificationAsync когда userJoinMessage == null
    /// Мутант: Statement mutation - строка 903 может быть удалена без провала тестов
    /// </summary>
    [Test]
    [Category(TestCategories.MutationCoverage)]
    public async Task BanUserForLongName_WhenUserJoinMessageIsNull_ShouldSendLogNotification()
    {
        // Arrange
        var user = TestKitBuilders.CreateUser()
            .WithFirstName("VeryLongNameThatExceeds64CharactersAndTriggersLongNameBanLogic")
            .Build();
        var chat = TestKitBuilders.CreateChat()
            .WithType(ChatType.Supergroup)
            .Build();

        // Act: Передаем null в качестве userJoinMessage
        await _messageHandler.BanUserForLongName(null, user, "Long name ban", TimeSpan.FromMinutes(10), CancellationToken.None);

        // Assert: Проверяем, что вызван SendLogNotificationAsync (строка 903)
        _messageServiceMock.Verify(
            x => x.SendLogNotificationAsync(
                LogNotificationType.BanForLongName,
                It.Is<AutoBanNotificationData>(data => 
                    data.User == user && 
                    data.Chat == chat && 
                    data.BanType == "Автобан на 10 минут"),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Должен быть вызван SendLogNotificationAsync когда userJoinMessage == null");

        // Assert: НЕ должен быть вызван ForwardToLogWithNotificationAsync
        _messageServiceMock.Verify(
            x => x.ForwardToLogWithNotificationAsync(
                It.IsAny<Message>(),
                It.IsAny<LogNotificationType>(),
                It.IsAny<AutoBanNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "Не должен быть вызван ForwardToLogWithNotificationAsync когда userJoinMessage == null");
    }

    /// <summary>
    /// Дополнительный тест для покрытия граничного случая: проверка удаления сообщения
    /// </summary>
    [Test]
    [Category(TestCategories.MutationCoverage)]
    public async Task BanUserForLongName_WhenUserJoinMessageExists_ShouldDeleteMessage()
    {
        // Arrange
        var user = TestKitBuilders.CreateUser()
            .WithFirstName("VeryLongNameThatExceeds64CharactersAndTriggersLongNameBanLogic")
            .Build();
        var chat = TestKitBuilders.CreateChat()
            .WithType(ChatType.Supergroup)
            .Build();
        var message = TestKitBuilders.CreateMessage()
            .FromUser(user)
            .InChat(chat)
            .WithMessageId(12345)
            .Build();

        // Act
        await _messageHandler.BanUserForLongName(message, user, "Long name ban", TimeSpan.FromMinutes(10), CancellationToken.None);

        // Assert: Проверяем, что сообщение удалено
        _botMock.Verify(
            x => x.DeleteMessage(chat.Id, 12345, It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно быть удалено сообщение пользователя при бане");
    }
}
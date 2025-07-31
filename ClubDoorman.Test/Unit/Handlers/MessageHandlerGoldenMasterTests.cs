using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Handlers;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Test.TestKit;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// POC Golden Master тест для логики банов
/// Проверяет реальные вызовы моков с использованием Verify
/// <tags>golden-master, poc, ban-logic, verify</tags>
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
[Category(TestCategories.Critical)]
[Category(TestCategories.GoldenMaster)]
public class MessageHandlerGoldenMasterTests
{
    private MessageHandler _messageHandler;
    private MessageHandlerTestFactory _factory;
    
    [SetUp]
    public void Setup()
    {
        _factory = new MessageHandlerTestFactory();
        _messageHandler = _factory.CreateMessageHandler();
    }

    /// <summary>
    /// POC тест для временного бана с проверкой всех внешних вызовов
    /// Демонстрирует правильный подход к Golden Master тестированию
    /// <tags>golden-master, poc, temporary-ban, verify-calls</tags>
    /// </summary>
    [Test]
    public async Task BanUserForLongName_TemporaryBan_VerifiesAllCalls()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message, banDuration, reason) = TK.Specialized.BanTests.TemporaryBanScenario();

        // Act: Вызываем метод напрямую
        await _messageHandler.BanUserForLongName(
            message, 
            user, 
            reason, 
            banDuration, 
            CancellationToken.None);

        // Assert: Проверяем все внешние вызовы
        _factory.BotMock.Verify(
            x => x.BanChatMember(
                chat.Id,
                user.Id,
                It.Is<DateTime?>(dt => dt.HasValue && dt.Value > DateTime.UtcNow),
                true, // revokeMessages
                CancellationToken.None),
            Times.Once,
            "Должен вызваться BanChatMember с правильными параметрами");

        _factory.BotMock.Verify(
            x => x.DeleteMessage(
                chat.Id,
                message.MessageId,
                CancellationToken.None),
            Times.Once,
            "Должно удалиться сообщение о присоединении");

        _factory.MessageServiceMock.Verify(
            x => x.ForwardToLogWithNotificationAsync(
                message,
                LogNotificationType.BanForLongName,
                It.Is<AutoBanNotificationData>(data => 
                    data.User.Id == user.Id && 
                    data.Chat.Id == chat.Id &&
                    data.Reason == reason),
                CancellationToken.None),
            Times.Once,
            "Должно отправиться уведомление в лог-чат");

        _factory.UserFlowLoggerMock.Verify(
            x => x.LogUserBanned(
                user,
                chat,
                reason),
            Times.Once,
            "Должен залогироваться бан пользователя");
    }

    /// <summary>
    /// Боевой тест для перманентного бана с проверкой всех внешних вызовов
    /// Проверяет сценарий без временного ограничения
    /// <tags>golden-master, production, permanent-ban, verify-calls</tags>
    /// </summary>
    [Test]
    public async Task BanUserForLongName_PermanentBan_VerifiesAllCalls()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message, banDuration, reason) = TK.Specialized.BanTests.PermanentBanScenario();

        // Act: Вызываем метод напрямую
        await _messageHandler.BanUserForLongName(
            message, 
            user, 
            reason, 
            banDuration, 
            CancellationToken.None);

        // Assert: Проверяем все внешние вызовы
        _factory.BotMock.Verify(
            x => x.BanChatMember(
                chat.Id,
                user.Id,
                null, // Перманентный бан - без времени
                true, // revokeMessages
                CancellationToken.None),
            Times.Once,
            "Должен вызваться BanChatMember с null для перманентного бана");

        _factory.BotMock.Verify(
            x => x.DeleteMessage(
                chat.Id,
                message.MessageId,
                CancellationToken.None),
            Times.Once,
            "Должно удалиться сообщение о присоединении");

        _factory.MessageServiceMock.Verify(
            x => x.ForwardToLogWithNotificationAsync(
                message,
                LogNotificationType.BanForLongName,
                It.Is<AutoBanNotificationData>(data => 
                    data.User.Id == user.Id && 
                    data.Chat.Id == chat.Id &&
                    data.Reason == reason),
                CancellationToken.None),
            Times.Once,
            "Должно отправиться уведомление в лог-чат");

        _factory.UserFlowLoggerMock.Verify(
            x => x.LogUserBanned(
                user,
                chat,
                reason),
            Times.Once,
            "Должен залогироваться бан пользователя");
    }

    /// <summary>
    /// Боевой тест для попытки бана в приватном чате
    /// Проверяет обработку случая, когда бан невозможен
    /// <tags>golden-master, production, private-chat, error-handling</tags>
    /// </summary>
    [Test]
    public async Task BanUserForLongName_PrivateChat_LogsWarningAndSendsAdminNotification()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message, banDuration, reason) = TK.Specialized.BanTests.PrivateChatBanScenario();

        // Act: Вызываем метод напрямую
        await _messageHandler.BanUserForLongName(
            message, 
            user, 
            reason, 
            banDuration, 
            CancellationToken.None);

        // Assert: Проверяем, что бан НЕ вызывался
        _factory.BotMock.Verify(
            x => x.BanChatMember(
                It.IsAny<ChatId>(),
                It.IsAny<long>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "BanChatMember не должен вызываться в приватном чате");

        // Assert: Проверяем отправку уведомления администратору
        _factory.MessageServiceMock.Verify(
            x => x.SendAdminNotificationAsync(
                AdminNotificationType.PrivateChatBanAttempt,
                It.Is<ErrorNotificationData>(data => 
                    data.User.Id == user.Id && 
                    data.Chat.Id == chat.Id &&
                    data.Context == "Бан за длинное имя"),
                CancellationToken.None),
            Times.Once,
            "Должно отправиться уведомление администратору о попытке бана в приватном чате");

        // Assert: Проверяем логирование предупреждения
        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Попытка бана за длинное имя в приватном чате")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться предупреждение о попытке бана в приватном чате");
    }

    /// <summary>
    /// Тест для бана пользователя из блэклиста
    /// Демонстрирует экстраполяцию подхода на другой метод банов
    /// <tags>golden-master, production, blacklist-ban, extrapolation</tags>
    /// </summary>
    [Test]
    public async Task BanBlacklistedUser_GroupChat_VerifiesAllCalls()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message) = TK.Specialized.BanTests.BlacklistBanScenario();

        // Act: Вызываем метод напрямую
        await _messageHandler.BanBlacklistedUser(message, user, CancellationToken.None);

        // Assert: Проверяем все внешние вызовы
        _factory.BotMock.Verify(
            x => x.BanChatMember(
                chat.Id,
                user.Id,
                It.Is<DateTime?>(dt => dt.HasValue && dt.Value > DateTime.UtcNow),
                true, // revokeMessages
                CancellationToken.None),
            Times.Once,
            "Должен вызваться BanChatMember с временным баном (240 минут)");

        _factory.BotMock.Verify(
            x => x.DeleteMessage(
                chat.Id,
                message.MessageId,
                CancellationToken.None),
            Times.Once,
            "Должно удалиться сообщение о присоединении");

        _factory.UserFlowLoggerMock.Verify(
            x => x.LogUserBanned(
                user,
                chat,
                "Пользователь в блэклисте"),
            Times.Once,
            "Должен залогироваться бан пользователя из блэклиста");
    }
} 
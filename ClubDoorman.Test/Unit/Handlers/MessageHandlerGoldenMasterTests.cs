using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Handlers;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using ClubDoorman.Test.TestKit;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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
        
        // Настраиваем AppConfig для тестов
        _factory.AppConfigMock.Setup(x => x.LogAdminChatId).Returns(123456789L);
        _factory.AppConfigMock.Setup(x => x.RepeatedViolationsBanToAdminChat).Returns(false);
        
        // Настраиваем UserManager для HandleBlacklistBan
        _factory.UserManagerMock.Setup(x => x.RemoveApproval(It.IsAny<long>(), It.IsAny<long?>(), It.IsAny<bool>())).Returns(false);
        
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

    /// <summary>
    /// Тест для бана канала
    /// Проверяет AutoBanChannel метод
    /// <tags>golden-master, production, channel-ban, extrapolation</tags>
    /// </summary>
    [Test]
    public async Task AutoBanChannel_ChannelMessage_VerifiesAllCalls()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (targetChat, senderChat, message) = TK.Specialized.BanTests.ChannelBanScenario();

        // Act: Вызываем метод напрямую
        await _messageHandler.AutoBanChannel(message, CancellationToken.None);

        // Assert: Проверяем все внешние вызовы
        _factory.BotMock.Verify(
            x => x.DeleteMessage(
                targetChat,
                message.MessageId,
                CancellationToken.None),
            Times.Once,
            "Должно удалиться сообщение от канала");

        _factory.BotMock.Verify(
            x => x.BanChatSenderChat(
                targetChat,
                senderChat.Id,
                CancellationToken.None),
            Times.Once,
            "Должен заблокироваться канал-отправитель");

        _factory.MessageServiceMock.Verify(
            x => x.ForwardToAdminWithNotificationAsync(
                message,
                AdminNotificationType.ChannelMessage,
                It.Is<ChannelMessageNotificationData>(data => 
                    data.SenderChat.Id == senderChat.Id && 
                    data.Chat.Id == targetChat.Id),
                CancellationToken.None),
            Times.Once,
            "Должно отправиться уведомление администратору о сообщении от канала");
    }

    /// <summary>
    /// Тест для автобана пользователя
    /// Проверяет AutoBan метод
    /// <tags>golden-master, production, auto-ban, extrapolation</tags>
    /// </summary>
    [Test]
    public async Task AutoBan_GroupChat_VerifiesAllCalls()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message, reason) = TK.Specialized.BanTests.AutoBanScenario();

        // Act: Вызываем метод напрямую
        await _messageHandler.AutoBan(message, reason, CancellationToken.None);

        // Assert: Проверяем все внешние вызовы
        _factory.BotMock.Verify(
            x => x.BanChatMember(
                chat,
                user.Id,
                null, // Перманентный бан
                false, // revokeMessages = false
                CancellationToken.None),
            Times.Once,
            "Должен заблокироваться пользователь");

        _factory.BotMock.Verify(
            x => x.DeleteMessage(
                chat,
                message.MessageId,
                CancellationToken.None),
            Times.Once,
            "Должно удалиться сообщение");

        _factory.MessageServiceMock.Verify(
            x => x.SendLogNotificationAsync(
                LogNotificationType.AutoBanKnownSpam,
                It.Is<AutoBanNotificationData>(data => 
                    data.User.Id == user.Id && 
                    data.Chat.Id == chat.Id &&
                    data.Reason == reason),
                CancellationToken.None),
            Times.Once,
            "Должно отправиться уведомление в лог-чат");
    }

    /// <summary>
    /// Тест для HandleBlacklistBan в групповом чате
    /// Проверяет все внешние вызовы при бане пользователя из блэклиста
    /// <tags>golden-master, production, handle-blacklist, extrapolation</tags>
    /// </summary>
    [Test]
    public async Task HandleBlacklistBan_GroupChat_VerifiesAllCalls()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message) = TK.Specialized.BanTests.HandleBlacklistBanScenario();

        // Act: Вызываем метод напрямую
        var cancellationToken = CancellationToken.None;
        await _messageHandler.HandleBlacklistBan(message, user, chat, cancellationToken);

        // Assert: Проверяем все внешние вызовы
        _factory.UserFlowLoggerMock.Verify(
            x => x.LogUserBanned(
                user,
                chat,
                "Пользователь в блэклисте lols.bot"),
            Times.Once,
            "Должен залогироваться бан пользователя из блэклиста");

        _factory.BotMock.Verify(
            x => x.ForwardMessage(
                It.IsAny<ChatId>(),
                chat.Id,
                message.MessageId,
                It.Is<CancellationToken>(_ => true)),
            Times.Once,
            "Должно переслаться сообщение в лог-чат");

        _factory.BotMock.Verify(
            x => x.DeleteMessage(
                chat,
                message.MessageId,
                It.Is<CancellationToken>(_ => true)),
            Times.Once,
            "Должно удалиться сообщение");

        _factory.BotMock.Verify(
            x => x.BanChatMember(
                chat.Id,
                user.Id,
                It.Is<DateTime?>(dt => dt.HasValue && dt.Value > DateTime.UtcNow),
                true, // revokeMessages
                It.Is<CancellationToken>(_ => true)),
            Times.Once,
            "Должен заблокироваться пользователь на 4 часа");

        _factory.StatisticsServiceMock.Verify(
            x => x.IncrementBlacklistBan(chat.Id),
            Times.Once,
            "Должна обновиться статистика банов из блэклиста");
    }

    /// <summary>
    /// Тест для HandleBlacklistBan с проверкой логирования
    /// Проверяет, что метод логирует информацию о бане
    /// <tags>golden-master, production, handle-blacklist, logging, extrapolation</tags>
    /// </summary>
    [Test]
    public async Task HandleBlacklistBan_LogsBanInformation()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message) = TK.Specialized.BanTests.HandleBlacklistBanScenario();

        // Act: Вызываем метод напрямую
        var cancellationToken = CancellationToken.None;
        await _messageHandler.HandleBlacklistBan(message, user, chat, cancellationToken);

        // Assert: Проверяем логирование
        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("БЛЭКЛИСТ LOLS.BOT")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "Должен залогироваться предупреждение о блэклисте");

        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("АВТОБАН ЗАВЕРШЕН")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должен залогироваться успешное завершение бана");
    }

    /// <summary>
    /// Тест для HandleBlacklistBan с длинным текстом
    /// Проверяет обрезку длинного текста в логах
    /// <tags>golden-master, production, handle-blacklist, long-text, extrapolation</tags>
    /// </summary>
    [Test]
    public async Task HandleBlacklistBan_LongText_TruncatesTextInLogs()
    {
        // Arrange: Используем сценарий с длинным текстом
        var (user, chat, message) = TK.Specialized.BanTests.HandleBlacklistBanLongTextScenario();

        // Act: Вызываем метод напрямую
        var cancellationToken = CancellationToken.None;
        await _messageHandler.HandleBlacklistBan(message, user, chat, cancellationToken);

        // Assert: Проверяем, что текст обрезается в логах
        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("...")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "Должен залогироваться предупреждение с обрезанным текстом");

        // Проверяем остальные вызовы
        _factory.UserFlowLoggerMock.Verify(
            x => x.LogUserBanned(user, chat, "Пользователь в блэклисте lols.bot"),
            Times.Once);
    }

    /// <summary>
    /// Тест для HandleBlacklistBan с медиа сообщением
    /// Проверяет обработку сообщений без текста
    /// <tags>golden-master, production, handle-blacklist, media-message, extrapolation</tags>
    /// </summary>
    [Test]
    public async Task HandleBlacklistBan_MediaMessage_HandlesCaptionCorrectly()
    {
        // Arrange: Используем сценарий с медиа сообщением
        var (user, chat, message) = TK.Specialized.BanTests.HandleBlacklistBanMediaScenario();

        // Act: Вызываем метод напрямую
        var cancellationToken = CancellationToken.None;
        await _messageHandler.HandleBlacklistBan(message, user, chat, cancellationToken);

        // Assert: Проверяем, что используется caption вместо text
        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Подпись к медиа от пользователя из блэклиста")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "Должен залогироваться предупреждение с caption");

        // Проверяем остальные вызовы
        _factory.UserFlowLoggerMock.Verify(
            x => x.LogUserBanned(user, chat, "Пользователь в блэклисте lols.bot"),
            Times.Once);
    }

    /// <summary>
    /// Тест для HandleBlacklistBan с пользователем из одобренных
    /// Проверяет удаление из списка одобренных
    /// <tags>golden-master, production, handle-blacklist, approved-user, extrapolation</tags>
    /// </summary>
    [Test]
    public async Task HandleBlacklistBan_ApprovedUser_RemovesFromApprovedList()
    {
        // Arrange: Используем сценарий с пользователем из одобренных
        var (user, chat, message) = TK.Specialized.BanTests.HandleBlacklistBanApprovedUserScenario();
        
        // Настраиваем UserManager для возврата true при удалении из одобренных
        _factory.UserManagerMock.Setup(x => x.RemoveApproval(user.Id, null, false)).Returns(true);

        // Act: Вызываем метод напрямую
        var cancellationToken = CancellationToken.None;
        await _messageHandler.HandleBlacklistBan(message, user, chat, cancellationToken);

        // Assert: Проверяем удаление из одобренных
        _factory.UserManagerMock.Verify(
            x => x.RemoveApproval(user.Id, null, false),
            Times.Once,
            "Должен удалиться пользователь из списка одобренных");

        // Проверяем отправку уведомления об удалении
        _factory.MessageServiceMock.Verify(
            x => x.SendAdminNotificationAsync(
                AdminNotificationType.RemovedFromApproved,
                It.Is<SimpleNotificationData>(d => d.User == user && d.Chat == chat),
                cancellationToken),
            Times.Once,
            "Должно отправиться уведомление об удалении из одобренных");
    }

    /// <summary>
    /// Тест для HandleBlacklistBan с исключением при пересылке в лог-чат
    /// Проверяет обработку ошибки при пересылке сообщения
    /// <tags>golden-master, production, handle-blacklist, exception-handling, extrapolation</tags>
    /// </summary>
    [Test]
    public async Task HandleBlacklistBan_ForwardMessageException_LogsWarningAndContinues()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message) = TK.Specialized.BanTests.HandleBlacklistBanScenario();
        
        // Настраиваем логгер для вывода Debug сообщений (помогает покрытию)
        _factory.LoggerMock.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);
        _factory.LoggerMock.Setup(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception, Delegate>((level, id, state, ex, formatter) =>
            {
                Console.WriteLine($"[DEBUG] {state}");
            });
        
        // Настраиваем MessageService для выброса исключения при получении шаблона
        _factory.MessageServiceMock.Setup(x => x.GetTemplates())
            .Throws(new Exception("Template error"));

        // Act: Вызываем метод напрямую
        var cancellationToken = CancellationToken.None;
        await _messageHandler.HandleBlacklistBan(message, user, chat, cancellationToken);

        // Assert: Проверяем логирование исключения
        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Не удалось переслать сообщение в лог-чат")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться предупреждение о неудачной пересылке");

        // Проверяем, что остальные операции выполняются
        _factory.BotMock.Verify(
            x => x.DeleteMessage(chat, message.MessageId, It.IsAny<CancellationToken>()),
            Times.Once,
            "Должно удалиться сообщение несмотря на ошибку пересылки");

        _factory.BotMock.Verify(
            x => x.BanChatMember(
                chat.Id,
                user.Id,
                It.Is<DateTime?>(dt => dt.HasValue && dt.Value > DateTime.UtcNow),
                true, // revokeMessages
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Должен заблокироваться пользователь несмотря на ошибку пересылки");
    }

    /// <summary>
    /// Тест для BanUserForLongName с исключением при бане
    /// Проверяет обработку ошибки при попытке бана пользователя
    /// <tags>golden-master, production, ban-exception, error-handling, extrapolation</tags>
    /// </summary>
    [Test]
    public async Task BanUserForLongName_BanException_LogsWarningAndContinues()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message, banDuration, reason) = TK.Specialized.BanTests.TemporaryBanScenario();
        
        // Настраиваем Bot для выброса исключения при бане
        _factory.BotMock.Setup(x => x.BanChatMember(
            It.IsAny<ChatId>(),
            It.IsAny<long>(),
            It.IsAny<DateTime?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error during ban"));

        // Act: Вызываем метод напрямую
        var cancellationToken = CancellationToken.None;
        await _messageHandler.BanUserForLongName(message, user, reason, banDuration, cancellationToken);

        // Assert: Проверяем логирование исключения
        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Не удалось забанить пользователя за длинное имя")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться предупреждение о неудачном бане");

        // Проверяем, что остальные операции НЕ выполняются из-за исключения
        _factory.BotMock.Verify(
            x => x.DeleteMessage(
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "Сообщение не должно удаляться при ошибке бана");

        _factory.MessageServiceMock.Verify(
            x => x.ForwardToLogWithNotificationAsync(
                It.IsAny<Message>(),
                It.IsAny<LogNotificationType>(),
                It.IsAny<AutoBanNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "Уведомление не должно отправляться при ошибке бана");

        _factory.UserFlowLoggerMock.Verify(
            x => x.LogUserBanned(
                It.IsAny<User>(),
                It.IsAny<Chat>(),
                It.IsAny<string>()),
            Times.Never,
            "Логирование бана не должно происходить при ошибке");
    }

    /// <summary>
    /// Тест для BanUserForLongName с исключением при удалении сообщения
    /// Проверяет обработку ошибки при удалении сообщения
    /// <tags>golden-master, production, delete-exception, error-handling, extrapolation</tags>
    /// </summary>
    [Test]
    public async Task BanUserForLongName_DeleteMessageException_LogsWarningAndContinues()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message, banDuration, reason) = TK.Specialized.BanTests.TemporaryBanScenario();
        
        // Настраиваем Bot для выброса исключения при удалении сообщения
        _factory.BotMock.Setup(x => x.DeleteMessage(
            It.IsAny<ChatId>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Message not found"));

        // Act: Вызываем метод напрямую
        var cancellationToken = CancellationToken.None;
        await _messageHandler.BanUserForLongName(message, user, reason, banDuration, cancellationToken);

        // Assert: Проверяем бан пользователя (должен выполниться)
        _factory.BotMock.Verify(
            x => x.BanChatMember(
                chat.Id,
                user.Id,
                It.Is<DateTime?>(dt => dt.HasValue && dt.Value > DateTime.UtcNow),
                true, // revokeMessages
                CancellationToken.None),
            Times.Once,
            "Должен вызваться BanChatMember с правильными параметрами");

        // Проверяем логирование исключения
        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Не удалось забанить пользователя за длинное имя")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться предупреждение о неудачном бане");

        // Проверяем, что уведомление НЕ отправляется из-за исключения
        _factory.MessageServiceMock.Verify(
            x => x.ForwardToLogWithNotificationAsync(
                It.IsAny<Message>(),
                It.IsAny<LogNotificationType>(),
                It.IsAny<AutoBanNotificationData>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "Уведомление не должно отправляться при ошибке удаления");

        _factory.UserFlowLoggerMock.Verify(
            x => x.LogUserBanned(
                It.IsAny<User>(),
                It.IsAny<Chat>(),
                It.IsAny<string>()),
            Times.Never,
            "Логирование бана не должно происходить при ошибке");
    }

    /// <summary>
    /// Тест для AutoBan с исключениями в различных операциях
    /// Проверяет обработку ошибок при отправке уведомлений, удалении сообщения и бане
    /// <tags>golden-master, production, auto-ban-exception, error-handling, extrapolation</tags>
    /// </summary>
    [Test]
    public async Task AutoBan_ExceptionsInOperations_LogsErrorsAndContinues()
    {
        // Arrange: Используем готовый сценарий из TestKit
        var (user, chat, message, reason) = TK.Specialized.BanTests.AutoBanScenario();
        
        // Настраиваем исключения для различных операций
        _factory.MessageServiceMock.Setup(x => x.SendLogNotificationAsync(
            It.IsAny<LogNotificationType>(),
            It.IsAny<AutoBanNotificationData>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Notification service error"));

        _factory.BotMock.Setup(x => x.DeleteMessage(
            It.IsAny<ChatId>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Message already deleted"));

        _factory.BotMock.Setup(x => x.BanChatMember(
            It.IsAny<ChatId>(),
            It.IsAny<long>(),
            It.IsAny<DateTime?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("User already banned"));

        // Act: Вызываем метод напрямую
        var cancellationToken = CancellationToken.None;
        await _messageHandler.AutoBan(message, reason, cancellationToken);

        // Assert: Проверяем логирование ошибок
        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ошибка при отправке уведомления о бане типа")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться ошибка при отправке уведомления");

        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Не удалось удалить сообщение")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться предупреждение при ошибке удаления сообщения");

        _factory.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ошибка при бане пользователя")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться ошибка при бане пользователя");

        // Проверяем, что очистка пользователя все равно выполняется
        _factory.ModerationServiceMock.Verify(
            x => x.CleanupUserFromAllLists(
                user.Id,
                chat.Id),
            Times.Once,
            "Должна выполниться очистка пользователя из всех списков");

        // Проверяем сброс счетчиков нарушений
        _factory.ViolationTrackerMock.Verify(
            x => x.ResetViolations(user.Id, chat.Id, ViolationType.MlSpam),
            Times.Once,
            "Должен сброситься счетчик ML спама");

        _factory.ViolationTrackerMock.Verify(
            x => x.ResetViolations(user.Id, chat.Id, ViolationType.StopWords),
            Times.Once,
            "Должен сброситься счетчик стоп-слов");

        _factory.ViolationTrackerMock.Verify(
            x => x.ResetViolations(user.Id, chat.Id, ViolationType.TooManyEmojis),
            Times.Once,
            "Должен сброситься счетчик эмодзи");

        _factory.ViolationTrackerMock.Verify(
            x => x.ResetViolations(user.Id, chat.Id, ViolationType.LookalikeSymbols),
            Times.Once,
            "Должен сброситься счетчик похожих символов");
    }
} 
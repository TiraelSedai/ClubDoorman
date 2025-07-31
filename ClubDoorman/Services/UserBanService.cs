using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Handlers;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Infrastructure;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для управления банами пользователей
/// </summary>
public class UserBanService : IUserBanService
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IMessageService _messageService;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly ILogger<UserBanService> _logger;
    private readonly IModerationService _moderationService;
    private readonly IViolationTracker _violationTracker;
    private readonly IAppConfig _appConfig;
    private readonly IStatisticsService _statisticsService;
    private readonly GlobalStatsManager _globalStatsManager;
    private readonly IUserManager _userManager;

    public UserBanService(
        ITelegramBotClientWrapper bot,
        IMessageService messageService,
        IUserFlowLogger userFlowLogger,
        ILogger<UserBanService> logger,
        IModerationService moderationService,
        IViolationTracker violationTracker,
        IAppConfig appConfig,
        IStatisticsService statisticsService,
        GlobalStatsManager globalStatsManager,
        IUserManager userManager)
    {
        _bot = bot;
        _messageService = messageService;
        _userFlowLogger = userFlowLogger;
        _logger = logger;
        _moderationService = moderationService;
        _violationTracker = violationTracker;
        _appConfig = appConfig;
        _statisticsService = statisticsService;
        _globalStatsManager = globalStatsManager;
        _userManager = userManager;
    }

    public async Task BanUserForLongNameAsync(Message? userJoinMessage, User user, string reason, TimeSpan? banDuration, CancellationToken cancellationToken)
    {
        try
        {
            var chat = userJoinMessage?.Chat!;
            
            if (!await ValidateBanOperationAsync(chat, user, "Бан за длинное имя", cancellationToken))
                return;

            await _bot.BanChatMember(
                chat.Id, 
                user.Id,
                banDuration.HasValue ? DateTime.UtcNow + banDuration.Value : null,
                revokeMessages: true
            );
            
            if (userJoinMessage != null)
            {
                await _bot.DeleteMessage(userJoinMessage.Chat.Id, userJoinMessage.MessageId, cancellationToken);
            }

            var banType = banDuration.HasValue ? "Автобан на 10 минут" : "🚫 Перманентный бан";
            var banData = new AutoBanNotificationData(user, chat, banType, reason, userJoinMessage?.MessageId);
            
            // Отправляем уведомление только в лог-чат
            if (userJoinMessage != null)
            {
                await _messageService.ForwardToLogWithNotificationAsync(userJoinMessage, LogNotificationType.BanForLongName, banData, cancellationToken);
            }
            else
            {
                await _messageService.SendLogNotificationAsync(LogNotificationType.BanForLongName, banData, cancellationToken);
            }
            
            _userFlowLogger.LogUserBanned(user, chat, reason);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя за длинное имя");
        }
    }

    public async Task BanBlacklistedUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken)
    {
        try
        {
            var chat = userJoinMessage.Chat;
            
            if (!await ValidateBanOperationAsync(chat, user, "Бан из блэклиста", cancellationToken))
                return;
            
            await BanUserTemporarilyAsync(chat, user, TimeSpan.FromMinutes(240), cancellationToken);
            await DeleteMessageSafelyAsync(userJoinMessage, cancellationToken);
            
            _userFlowLogger.LogUserBanned(user, chat, "Пользователь в блэклисте");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя из блэклиста");
        }
    }

    public async Task AutoBanAsync(Message message, string reason, CancellationToken cancellationToken)
    {
        var user = message.From;
        var chat = message.Chat;
        
        if (!await ValidateBanOperationAsync(chat, user, reason, cancellationToken))
            return;
        
        // Форвардим сообщение в лог-чат с уведомлением
        var autoBanData = new AutoBanNotificationData(
            user, 
            message.Chat, 
            "Автобан", 
            reason, 
            message.MessageId, 
            LinkToMessage(message.Chat, message.MessageId)
        );
        
        // Выбираем правильный тип уведомления в зависимости от причины
        var logNotificationType = reason switch
        {
            var r when r.Contains("Известное спам-сообщение") => LogNotificationType.AutoBanKnownSpam,
            var r when r.Contains("Ссылки запрещены") => LogNotificationType.AutoBanTextMention,
            var r when r.Contains("Повторные нарушения") => LogNotificationType.AutoBanRepeatedViolations,
            _ => LogNotificationType.AutoBanBlacklist
        };
            
        try
        {
            // Отправляем уведомление в зависимости от настройки
            if (_appConfig.RepeatedViolationsBanToAdminChat)
            {
                // Отправляем в админ-чат
                await _messageService.SendAdminNotificationAsync(AdminNotificationType.AutoBan, autoBanData, cancellationToken);
            }
            else
            {
                // Отправляем в лог-чат без пересылки сообщения (так как оно уже удалено)
                await _messageService.SendLogNotificationAsync(logNotificationType, autoBanData, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке уведомления о бане типа {NotificationType}", logNotificationType);
        }
        
        try
        {
            // Пытаемся удалить сообщение (может не получиться, если уже удалено)
            await _bot.DeleteMessage(message.Chat, message.MessageId, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось удалить сообщение {MessageId} из чата {ChatId} (возможно, уже удалено)", message.MessageId, message.Chat.Id);
        }
        
        try
        {
            // Баним пользователя
            await _bot.BanChatMember(message.Chat, user.Id, revokeMessages: false, cancellationToken: cancellationToken);
            _logger.LogInformation("✅ Пользователь {UserId} успешно забанен в чате {ChatId}", user.Id, message.Chat.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при бане пользователя {UserId} в чате {ChatId}", user.Id, message.Chat.Id);
        }
        
        // Полностью очищаем пользователя из всех списков
        _moderationService.CleanupUserFromAllLists(user.Id, message.Chat.Id);
        
        // Сбрасываем счетчики нарушений для всех типов
        _violationTracker.ResetViolations(user.Id, message.Chat.Id, ViolationType.MlSpam);
        _violationTracker.ResetViolations(user.Id, message.Chat.Id, ViolationType.StopWords);
        _violationTracker.ResetViolations(user.Id, message.Chat.Id, ViolationType.TooManyEmojis);
        _violationTracker.ResetViolations(user.Id, message.Chat.Id, ViolationType.LookalikeSymbols);
        
        _logger.LogInformation("🧹 Счетчики нарушений сброшены для пользователя {UserId} в чате {ChatId}", user.Id, message.Chat.Id);
    }

    public async Task AutoBanChannelAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            var chat = message.Chat;
            var senderChat = message.SenderChat!;
            
            await _bot.DeleteMessage(chat, message.MessageId, cancellationToken);
            await _bot.BanChatSenderChat(chat, senderChat.Id, cancellationToken);
            
            var channelData = new ChannelMessageNotificationData(senderChat, chat, message.Text ?? "[медиа]");
            await _messageService.ForwardToAdminWithNotificationAsync(message, AdminNotificationType.ChannelMessage, channelData, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить канал");
            var errorData = new ErrorNotificationData(
                new InvalidOperationException("Не удалось забанить канал"),
                "Не хватает могущества",
                null,
                message.Chat
            );
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.ChannelError, errorData, cancellationToken);
        }
    }

    public async Task HandleBlacklistBanAsync(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        var userMessageText = message.Text ?? message.Caption ?? "[медиа/стикер/файл]";
        _logger.LogWarning("🚫 БЛЭКЛИСТ LOLS.BOT: {UserName} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) написал: {MessageText}", 
            FullName(user.FirstName, user.LastName), user.Id, chat.Title, chat.Id, 
            userMessageText.Length > 100 ? userMessageText.Substring(0, 100) + "..." : userMessageText);
        
        _userFlowLogger.LogUserBanned(user, chat, "Пользователь в блэклисте lols.bot");
        
        // Пересылаем сообщение в лог-чат с уведомлением
        try
        {
            var blacklistData = new AutoBanNotificationData(
                user, 
                message.Chat, 
                "Автобан по блэклисту lols.bot", 
                "первое сообщение", 
                message.MessageId, 
                LinkToMessage(message.Chat, message.MessageId)
            );
            
            // Пересылаем сообщение и отправляем уведомление как реплай
            var forwardedMessage = await _bot.ForwardMessage(
                new ChatId(Config.LogAdminChatId),
                message.Chat.Id,
                message.MessageId,
                cancellationToken: cancellationToken
            );
            
            var template = _messageService.GetTemplates().GetLogTemplate(LogNotificationType.AutoBanBlacklist);
            var messageText = _messageService.GetTemplates().FormatNotificationTemplate(template, blacklistData);
            
            await _bot.SendMessage(
                Config.LogAdminChatId,
                messageText,
                parseMode: ParseMode.Html,
                replyParameters: forwardedMessage,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось переслать сообщение в лог-чат");
        }
        
        // Удаляем сообщение
        try
        {
            await _bot.DeleteMessage(message.Chat, message.MessageId, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось удалить сообщение пользователя из блэклиста");
        }
        
        // Баним пользователя на 4 часа (как в IntroFlowService)
        try
        {
            var banUntil = DateTime.UtcNow + TimeSpan.FromMinutes(240);
            await _bot.BanChatMember(chat.Id, user.Id, untilDate: banUntil, revokeMessages: true, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя из блэклиста");
        }
        
        // Обновляем статистику
        _statisticsService.IncrementBlacklistBan(message.Chat.Id);
        _globalStatsManager.IncBan(message.Chat.Id, message.Chat.Title ?? "");
        
        // Удаляем из списка одобренных
        if (_userManager.RemoveApproval(user.Id))
        {
            try
            {
                var removedData = new SimpleNotificationData(user, message.Chat, "удален из списка одобренных после автобана по блэклисту");
                await _messageService.SendAdminNotificationAsync(AdminNotificationType.RemovedFromApproved, removedData, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Не удалось отправить уведомление об удалении из одобренных");
            }
        }
        
        _logger.LogInformation("✅ АВТОБАН ЗАВЕРШЕН: пользователь {User} (id={UserId}) забанен на 4 часа в чате '{ChatTitle}' (id={ChatId}) по блэклисту lols.bot", 
            FullName(user.FirstName, user.LastName), user.Id, message.Chat.Title, message.Chat.Id);
    }

    private static string LinkToMessage(Chat chat, long messageId) =>
        chat.Type switch
        {
            ChatType.Supergroup => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}",
            ChatType.Group when !string.IsNullOrEmpty(chat.Username) => $"https://t.me/{chat.Username}/{messageId}",
            _ => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}"
        };

    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";

    // Приватные вспомогательные методы

    private async Task<bool> ValidateBanOperationAsync(Chat chat, User user, string operation, CancellationToken cancellationToken)
    {
        if (chat.Type == ChatType.Private)
        {
            // Сохраняем оригинальные сообщения для совместимости с тестами
            var logMessage = operation switch
            {
                "Бан за длинное имя" => $"Попытка бана за длинное имя в приватном чате {chat.Id} - операция невозможна",
                "Бан из блэклиста" => $"Попытка бана из блэклиста в приватном чате {chat.Id} - операция невозможна",
                _ => $"Попытка бана в приватном чате {chat.Id} - операция невозможна"
            };
            
            _logger.LogWarning(logMessage);
            var errorData = new ErrorNotificationData(
                new InvalidOperationException("Попытка бана в приватном чате"),
                operation,
                user,
                chat
            );
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.PrivateChatBanAttempt, errorData, cancellationToken);
            return false;
        }
        return true;
    }

    private async Task BanUserTemporarilyAsync(Chat chat, User user, TimeSpan banDuration, CancellationToken cancellationToken)
    {
        var banUntil = DateTime.UtcNow + banDuration;
        await _bot.BanChatMember(chat.Id, user.Id, banUntil, revokeMessages: true, cancellationToken: cancellationToken);
    }

    private async Task DeleteMessageSafelyAsync(Message? message, CancellationToken cancellationToken)
    {
        if (message != null)
        {
            await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken);
        }
    }
} 
using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для отправки уведомлений в Telegram
/// </summary>
public class MessageService : IMessageService
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<MessageService> _logger;
    private readonly MessageTemplates _templates;
    
    public MessageService(
        ITelegramBotClientWrapper bot,
        ILogger<MessageService> logger,
        MessageTemplates templates)
    {
        _bot = bot;
        _logger = logger;
        _templates = templates;
    }
    
    public async Task SendAdminNotificationAsync(AdminNotificationType type, NotificationData data, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = _templates.GetAdminTemplate(type);
            var message = _templates.FormatNotificationTemplate(template, data);
            
            await _bot.SendMessage(
                Config.AdminChatId,
                message,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
            
            _logger.LogDebug("Отправлено админское уведомление типа {Type} для пользователя {User}", 
                type, Utils.FullName(data.User));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке админского уведомления типа {Type}", type);
        }
    }
    
    public async Task SendLogNotificationAsync(LogNotificationType type, NotificationData data, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = _templates.GetLogTemplate(type);
            var message = _templates.FormatNotificationTemplate(template, data);
            
            await _bot.SendMessage(
                Config.LogAdminChatId,
                message,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
            
            _logger.LogDebug("Отправлено лог-уведомление типа {Type} для пользователя {User}", 
                type, Utils.FullName(data.User));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке лог-уведомления типа {Type}", type);
        }
    }
    
    public async Task SendUserNotificationAsync(User user, Chat chat, UserNotificationType type, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = _templates.GetUserTemplate(type);
            var message = _templates.FormatTemplate(template, data);
            
            await _bot.SendMessage(
                chat.Id,
                message,
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken
            );
            
            _logger.LogDebug("Отправлено пользовательское уведомление типа {Type} пользователю {User} в чате {Chat}", 
                type, Utils.FullName(user), chat.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке пользовательского уведомления типа {Type} пользователю {User}", 
                type, Utils.FullName(user));
        }
    }
    
    public async Task<Message?> ForwardToAdminWithNotificationAsync(Message originalMessage, AdminNotificationType type, NotificationData data, CancellationToken cancellationToken = default)
    {
        try
        {
            // Пересылаем оригинальное сообщение
            var forward = await _bot.ForwardMessage(
                new ChatId(Config.AdminChatId),
                originalMessage.Chat.Id,
                originalMessage.MessageId,
                cancellationToken: cancellationToken
            );
            
            // Отправляем уведомление с реплаем
            var template = _templates.GetAdminTemplate(type);
            var message = _templates.FormatNotificationTemplate(template, data);
            
            var notification = await _bot.SendMessage(
                Config.AdminChatId,
                message,
                parseMode: ParseMode.Markdown,
                replyParameters: forward,
                cancellationToken: cancellationToken
            );
            
            _logger.LogDebug("Переслано сообщение в админский чат с уведомлением типа {Type}", type);
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при пересылке сообщения в админский чат с уведомлением типа {Type}", type);
            return null;
        }
    }
    
    public async Task<Message?> ForwardToLogWithNotificationAsync(Message originalMessage, LogNotificationType type, NotificationData data, CancellationToken cancellationToken = default)
    {
        try
        {
            // Пересылаем оригинальное сообщение
            var forward = await _bot.ForwardMessage(
                new ChatId(Config.LogAdminChatId),
                originalMessage.Chat.Id,
                originalMessage.MessageId,
                cancellationToken: cancellationToken
            );
            
            // Отправляем уведомление с реплаем
            var template = _templates.GetLogTemplate(type);
            var message = _templates.FormatNotificationTemplate(template, data);
            
            var notification = await _bot.SendMessage(
                Config.LogAdminChatId,
                message,
                parseMode: ParseMode.Markdown,
                replyParameters: forward,
                cancellationToken: cancellationToken
            );
            
            _logger.LogDebug("Переслано сообщение в лог-чат с уведомлением типа {Type}", type);
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при пересылке сообщения в лог-чат с уведомлением типа {Type}", type);
            return null;
        }
    }
    
    public async Task SendErrorNotificationAsync(Exception ex, string context, User? user = null, Chat? chat = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var errorData = new ErrorNotificationData(ex, context, user, chat);
            
            // Отправляем в админский чат
            await SendAdminNotificationAsync(AdminNotificationType.SystemError, errorData, cancellationToken);
            
            // Отправляем в лог-чат
            await SendLogNotificationAsync(LogNotificationType.CriticalError, errorData, cancellationToken);
            
            _logger.LogError(ex, "Отправлено уведомление об ошибке: {Context}", context);
        }
        catch (Exception notificationEx)
        {
            _logger.LogError(notificationEx, "Ошибка при отправке уведомления об ошибке: {Context}", context);
        }
    }
} 
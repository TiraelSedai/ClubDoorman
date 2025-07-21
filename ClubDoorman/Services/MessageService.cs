using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Runtime.Caching;

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
            
            // Для команды /start используем HTML разметку, для системной информации - Markdown
            var parseMode = type switch
            {
                UserNotificationType.Welcome => ParseMode.Html,
                UserNotificationType.SystemInfo => ParseMode.Markdown,
                _ => ParseMode.MarkdownV2
            };
            
            await _bot.SendMessage(
                chat.Id,
                message,
                parseMode: parseMode,
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
    
    public async Task SendAiProfileAnalysisAsync(AiProfileAnalysisData data, CancellationToken cancellationToken = default)
    {
        try
        {
            var displayName = !string.IsNullOrEmpty(data.User.FirstName)
                ? Utils.FullName(data.User.FirstName, data.User.LastName)
                : (!string.IsNullOrEmpty(data.User.Username) ? "@" + data.User.Username : "гость");

            var userProfileLink = data.User.Username != null ? $"@{data.User.Username}" : displayName;
            
            // Ограничиваем длину Reason от AI
            var reasonText = data.Reason;
            if (reasonText.Length > 500)
            {
                reasonText = reasonText.Substring(0, 497) + "...";
            }

            // Создаем кнопки для админ-чата
            var callbackDataBan = $"banprofile_{data.Chat.Id}_{data.User.Id}";
            var callbackDataOk = $"aiOk_{data.Chat.Id}_{data.User.Id}";
            
            MemoryCache.Default.Add(callbackDataBan, data, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });

            var buttons = new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton("❌❌❌ ban") { CallbackData = callbackDataBan },
                new InlineKeyboardButton("✅✅✅ ok") { CallbackData = callbackDataOk }
            });

            ReplyParameters? replyParams = null;
            
            // 1. Если есть фото - отправляем его отдельно с краткой подписью
            if (data.PhotoBytes?.Length > 0)
            {
                var photoCaption = $"{data.NameBio}\nСообщение:\n{data.MessageText}";
                // Обрезаем caption если слишком длинный
                if (photoCaption.Length > 1024)
                {
                    photoCaption = photoCaption.Substring(0, 1021) + "...";
                }
                
                await using var stream = new MemoryStream(data.PhotoBytes);
                var inputFile = InputFile.FromStream(stream, "profile.jpg");
                
                var photoMsg = await _bot.SendPhoto(
                    Config.AdminChatId,
                    inputFile,
                    caption: photoCaption,
                    cancellationToken: cancellationToken
                );
                replyParams = photoMsg;
            }
            
            // 2. Основное сообщение с анализом
            var template = _templates.GetAdminTemplate(AdminNotificationType.AiProfileAnalysis);
            var message = _templates.FormatNotificationTemplate(template, data);
            
            await _bot.SendMessage(
                Config.AdminChatId,
                message,
                replyMarkup: buttons,
                replyParameters: replyParams,
                cancellationToken: cancellationToken
            );
            
            _logger.LogDebug("Отправлено AI уведомление о профиле для пользователя {User}", Utils.FullName(data.User));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке AI уведомления о профиле для пользователя {User}", Utils.FullName(data.User));
        }
    }
} 
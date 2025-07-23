using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Models.Logging;
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
    private readonly ILoggingConfigurationService _configService;
    private readonly IServiceChatDispatcher _serviceChatDispatcher;
    
    public MessageService(
        ITelegramBotClientWrapper bot,
        ILogger<MessageService> logger,
        MessageTemplates templates,
        ILoggingConfigurationService configService,
        IServiceChatDispatcher serviceChatDispatcher)
    {
        _bot = bot;
        _logger = logger;
        _templates = templates;
        _configService = configService;
        _serviceChatDispatcher = serviceChatDispatcher;
    }
    
    public async Task SendAdminNotificationAsync(AdminNotificationType type, NotificationData data, CancellationToken cancellationToken = default)
    {
        try
        {
            var destinations = _configService.GetAdminNotificationDestinations(type);
            
            // Проверяем, нужно ли отправлять в админский чат
            if (destinations.HasFlag(NotificationDestination.AdminChat) && _configService.ShouldSendNotification(type.ToString(), NotificationDestination.AdminChat))
            {
                // Проверяем, что админский чат настроен корректно
                var adminChatEnv = Environment.GetEnvironmentVariable("DOORMAN_ADMIN_CHAT");
                if (string.IsNullOrEmpty(adminChatEnv))
                {
                    _logger.LogWarning("Админский чат не настроен (переменная DOORMAN_ADMIN_CHAT не установлена)");
                    return;
                }
                
                // Диагностика: проверяем доступность чата
                try
                {
                    var chatInfo = await _bot.GetChat(Config.AdminChatId, cancellationToken);
                    _logger.LogDebug("Админский чат доступен: {ChatTitle} (ID: {ChatId})", chatInfo.Title, chatInfo.Id);
                }
                catch (Exception chatEx)
                {
                    _logger.LogError(chatEx, "Не удается получить информацию об админском чате {ChatId}", Config.AdminChatId);
                    return;
                }
                
                // Используем диспетчер для определения типа чата
                if (_serviceChatDispatcher.ShouldSendToAdminChat(data))
                {
                    await _serviceChatDispatcher.SendToAdminChatAsync(data, cancellationToken);
                }
                else
                {
                    await _serviceChatDispatcher.SendToLogChatAsync(data, cancellationToken);
                }
                
                _logger.LogDebug("Отправлено уведомление типа {Type} для пользователя {User} через диспетчер", 
                    type, Utils.FullName(data.User));
            }
            else
            {
                _logger.LogDebug("Админское уведомление типа {Type} пропущено согласно конфигурации", type);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке админского уведомления типа {Type} в чат {ChatId}", type, Config.AdminChatId);
        }
    }
    
    public async Task SendLogNotificationAsync(LogNotificationType type, NotificationData data, CancellationToken cancellationToken = default)
    {
        try
        {
            var destinations = _configService.GetLogNotificationDestinations(type);
            
            // Проверяем, нужно ли отправлять в лог-чат
            if (destinations.HasFlag(NotificationDestination.LogChat) && _configService.ShouldSendNotification(type.ToString(), NotificationDestination.LogChat))
            {
                // Проверяем, что лог-чат настроен корректно
                var logChatEnv = Environment.GetEnvironmentVariable("DOORMAN_LOG_ADMIN_CHAT");
                if (string.IsNullOrEmpty(logChatEnv))
                {
                    _logger.LogWarning("Лог-чат не настроен (переменная DOORMAN_LOG_ADMIN_CHAT не установлена)");
                    return;
                }
                
                // Диагностика: проверяем доступность чата
                try
                {
                    var chatInfo = await _bot.GetChat(Config.LogAdminChatId, cancellationToken);
                    _logger.LogDebug("Лог-чат доступен: {ChatTitle} (ID: {ChatId})", chatInfo.Title, chatInfo.Id);
                }
                catch (Exception chatEx)
                {
                    _logger.LogError(chatEx, "Не удается получить информацию о лог-чате {ChatId}", Config.LogAdminChatId);
                    return;
                }
                
                // Используем диспетчер для отправки в лог-чат
                await _serviceChatDispatcher.SendToLogChatAsync(data, cancellationToken);
                
                _logger.LogDebug("Отправлено лог-уведомление типа {Type} для пользователя {User} через диспетчер", 
                    type, Utils.FullName(data.User));
            }
            else
            {
                _logger.LogDebug("Лог-уведомление типа {Type} пропущено согласно конфигурации", type);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке лог-уведомления типа {Type} в чат {ChatId}", type, Config.LogAdminChatId);
        }
    }
    
    public async Task SendUserNotificationAsync(User user, Chat chat, UserNotificationType type, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = _templates.GetUserTemplate(type);
            var message = _templates.FormatTemplate(template, data);
            
            // Для команды /start используем HTML разметку, для системной информации - Markdown, для капчи - HTML
            var parseMode = type switch
            {
                UserNotificationType.Welcome => ParseMode.Html,
                UserNotificationType.SystemInfo => ParseMode.Markdown,
                UserNotificationType.CaptchaWelcome => ParseMode.Html,
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
    
    public async Task<Message> SendUserNotificationWithReplyAsync(User user, Chat chat, UserNotificationType type, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = _templates.GetUserTemplate(type);
            string message;
            
            // Если data является NotificationData, используем FormatNotificationTemplate
            if (data is NotificationData notificationData)
            {
                message = _templates.FormatNotificationTemplate(template, notificationData);
            }
            else
            {
                message = _templates.FormatTemplate(template, data);
            }
            
            // Для команды /start используем HTML разметку, для системной информации - Markdown, для капчи - HTML
            var parseMode = type switch
            {
                UserNotificationType.Welcome => ParseMode.Html,
                UserNotificationType.SystemInfo => ParseMode.Markdown,
                UserNotificationType.CaptchaWelcome => ParseMode.Html,
                _ => ParseMode.MarkdownV2
            };
            
            var sentMessage = await _bot.SendMessage(
                chat.Id,
                message,
                parseMode: parseMode,
                cancellationToken: cancellationToken
            );
            
            _logger.LogDebug("Отправлено пользовательское уведомление типа {Type} пользователю {User} в чате {Chat}", 
                type, Utils.FullName(user), chat.Title);
            
            return sentMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке пользовательского уведомления типа {Type} пользователю {User}", 
                type, Utils.FullName(user));
            throw;
        }
    }

    /// <summary>
    /// Отправляет приветственное сообщение и автоматически удаляет его через 20 секунд
    /// </summary>
    public async Task<Message> SendWelcomeMessageAsync(User user, Chat chat, string reason = "приветствие", CancellationToken cancellationToken = default)
    {
        // Создаем приветственное сообщение (логика перенесена из CallbackQueryHandler)
        var displayName = !string.IsNullOrEmpty(user.FirstName)
            ? System.Net.WebUtility.HtmlEncode(Utils.FullName(user))
            : (!string.IsNullOrEmpty(user.Username) ? "@" + user.Username : "гость");
        
        var mention = $"<a href=\"tg://user?id={user.Id}\">{displayName}</a>";
        
        // Заглушка для рекламы (если группа не в исключениях)
        var isNoAdGroup = IsNoAdGroup(chat.Id);
        var vpnAd = isNoAdGroup ? "" : "\n\n\n📍 <b>Место для рекламы</b> \n <i>...</i>";
        
        string greetMsg;
        string mediaWarning;
        if (ChatSettingsManager.GetChatType(chat.Id) == "announcement")
        {
            mediaWarning = "";
            greetMsg = $"👋 {mention}\n\n<b>Внимание:</b> первые три сообщения проходят антиспам-проверку, сообщения со стоп-словами и спамом будут удалены. Не просите писать в ЛС!{vpnAd}";
        }
        else
        {
            mediaWarning = Config.IsMediaFilteringDisabledForChat(chat.Id) ? ", стикеры, документы" : ", изображения, стикеры, документы";
            greetMsg = $"👋 {mention}\n\n<b>Внимание!</b> первые три сообщения проходят антиспам-проверку, эмодзи{mediaWarning} и реклама запрещены — они могут удаляться автоматически. Не просите писать в ЛС!{vpnAd}";
        }

        var captchaWelcomeData = new CaptchaWelcomeNotificationData(
            user, chat, reason, 0, mediaWarning, vpnAd);
        var sent = await SendUserNotificationWithReplyAsync(
            user, chat, UserNotificationType.CaptchaWelcome, captchaWelcomeData, cancellationToken);
        
        // Удаляем приветствие через 20 секунд
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
                await _bot.DeleteMessage(chat.Id, sent.MessageId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить приветственное сообщение");
            }
        }, cancellationToken);

        return sent;
    }

    /// <summary>
    /// Проверяет, является ли группа исключением для рекламы VPN
    /// </summary>
    private static bool IsNoAdGroup(long chatId)
    {
        return Config.NoVpnAdGroups.Contains(chatId);
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
            _logger.LogDebug("🤖 MessageService.SendAiProfileAnalysisAsync: начало обработки для пользователя {UserId}, PhotoBytes: {PhotoBytesLength}", 
                data.User.Id, data.PhotoBytes?.Length ?? 0);
            
            // Используем диспетчер для определения типа чата
            if (_serviceChatDispatcher.ShouldSendToAdminChat(data))
            {
                _logger.LogDebug("🤖 MessageService: отправляем в админ-чат");
                await _serviceChatDispatcher.SendToAdminChatAsync(data, cancellationToken);
            }
            else
            {
                _logger.LogDebug("🤖 MessageService: отправляем в лог-чат");
                await _serviceChatDispatcher.SendToLogChatAsync(data, cancellationToken);
            }
            
            _logger.LogDebug("Отправлено AI уведомление о профиле для пользователя {User} через диспетчер", Utils.FullName(data.User));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке AI уведомления о профиле для пользователя {User}", Utils.FullName(data.User));
        }
    }
    
    public async Task<Message> SendCaptchaMessageAsync(Chat chat, string message, ReplyParameters? replyParameters, InlineKeyboardMarkup replyMarkup, CancellationToken cancellationToken = default)
    {
        try
        {
            var sentMessage = await _bot.SendMessage(
                chat.Id,
                message,
                parseMode: ParseMode.Html,
                replyParameters: replyParameters,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken
            );
            
            _logger.LogDebug("Отправлено сообщение капчи в чат {Chat}", chat.Title);
            
            return sentMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке сообщения капчи в чат {Chat}", chat.Title);
            throw;
        }
    }
    
    public MessageTemplates GetTemplates()
    {
        return _templates;
    }
} 
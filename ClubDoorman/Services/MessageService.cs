using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Models.Requests;
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
    private readonly IAppConfig _appConfig;
    
    public MessageService(
        ITelegramBotClientWrapper bot,
        ILogger<MessageService> logger,
        MessageTemplates templates,
        ILoggingConfigurationService configService,
        IServiceChatDispatcher serviceChatDispatcher,
        IAppConfig appConfig)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _serviceChatDispatcher = serviceChatDispatcher ?? throw new ArgumentNullException(nameof(serviceChatDispatcher));
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
    }
    
    /// <summary>
    /// Отправить уведомление в админский чат
    /// </summary>
    public async Task SendAdminNotificationAsync(AdminNotificationType type, NotificationData data, CancellationToken cancellationToken = default)
    {
        try
        {
            await _serviceChatDispatcher.SendToAdminChatAsync(data, cancellationToken);
            _logger.LogDebug("Отправлено уведомление в админский чат типа {Type}", type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке уведомления в админский чат типа {Type}", type);
            throw;
        }
    }
    
    /// <summary>
    /// Отправить уведомление в лог-чат
    /// </summary>
    public async Task SendLogNotificationAsync(LogNotificationType type, NotificationData data, CancellationToken cancellationToken = default)
    {
        try
        {
            await _serviceChatDispatcher.SendToLogChatAsync(data, cancellationToken);
            _logger.LogDebug("Отправлено уведомление в лог-чат типа {Type}", type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке уведомления в лог-чат типа {Type}", type);
            throw;
        }
    }
    
    /// <summary>
    /// Отправить уведомление пользователю
    /// </summary>
    public async Task SendUserNotificationAsync(User user, Chat chat, UserNotificationType type, object data, CancellationToken cancellationToken = default)
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
            
            await _bot.SendMessage(
                chat.Id,
                message,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
            
            _logger.LogDebug("Отправлено уведомление пользователю {UserId} в чате {ChatId} типа {Type}", user.Id, chat.Id, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке уведомления пользователю {UserId} в чате {ChatId} типа {Type}", user.Id, chat.Id, type);
            throw;
        }
    }
    
    /// <summary>
    /// Отправляет пользовательское уведомление и возвращает отправленное сообщение
    /// </summary>
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
            
            var sent = await _bot.SendMessage(
                chat.Id,
                message,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
            
            _logger.LogDebug("Отправлено уведомление с реплаем пользователю {UserId} в чате {ChatId} типа {Type}", user.Id, chat.Id, type);
            return sent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке уведомления с реплаем пользователю {UserId} в чате {ChatId} типа {Type}", user.Id, chat.Id, type);
            throw;
        }
    }

    /// <summary>
    /// Отправляет пользовательское уведомление как реплай на сообщение и возвращает отправленное сообщение
    /// </summary>
    public async Task<Message> SendUserNotificationWithReplyAsync(User user, Chat chat, UserNotificationType type, object data, ReplyParameters replyParameters, CancellationToken cancellationToken = default)
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
            
            var sent = await _bot.SendMessage(
                chat.Id,
                message,
                parseMode: ParseMode.Html,
                replyParameters: replyParameters,
                cancellationToken: cancellationToken
            );
            
            _logger.LogDebug("Отправлено уведомление с реплаем на сообщение {ReplyMessageId} пользователю {UserId} в чате {ChatId} типа {Type}", 
                replyParameters.MessageId, user.Id, chat.Id, type);
            return sent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке уведомления с реплаем на сообщение {ReplyMessageId} пользователю {UserId} в чате {ChatId} типа {Type}", 
                replyParameters.MessageId, user.Id, chat.Id, type);
            throw;
        }
    }

    /// <summary>
    /// Отправляет приветственное сообщение используя Request объект
    /// </summary>
    public async Task<Message?> SendWelcomeMessageAsync(SendWelcomeMessageRequest request)
    {
        // Проверяем, отключены ли приветствия
        if (Config.DisableWelcome)
        {
            _logger.LogDebug("Приветственные сообщения отключены (DOORMAN_DISABLE_WELCOME=true)");
            return null;
        }

        // Создаем приветственное сообщение (логика перенесена из CallbackQueryHandler)
        var displayName = !string.IsNullOrEmpty(request.User.FirstName)
            ? System.Net.WebUtility.HtmlEncode(Utils.FullName(request.User))
            : (!string.IsNullOrEmpty(request.User.Username) ? "@" + request.User.Username : "гость");
        
        var mention = $"<a href=\"tg://user?id={request.User.Id}\">{displayName}</a>";
        
        // Заглушка для рекламы (если группа не в исключениях)
        var isNoAdGroup = IsNoAdGroup(request.Chat.Id);
        var vpnAd = isNoAdGroup ? "" : "\n\n\n📍 <b>Место для рекламы</b> \n <i>...</i>";
        
        string greetMsg;
        string mediaWarning;
        if (ChatSettingsManager.GetChatType(request.Chat.Id) == "announcement")
        {
            mediaWarning = "";
            greetMsg = $"👋 {mention}\n\n<b>Внимание:</b> первые три сообщения проходят антиспам-проверку, сообщения со стоп-словами и спамом будут удалены. Не просите писать в ЛС!{vpnAd}";
        }
        else
        {
            mediaWarning = Config.IsMediaFilteringDisabledForChat(request.Chat.Id) ? ", стикеры, документы" : ", изображения, стикеры, документы";
            greetMsg = $"👋 {mention}\n\n<b>Внимание!</b> первые три сообщения проходят антиспам-проверку, эмодзи{mediaWarning} и реклама запрещены — они могут удаляться автоматически. Не просите писать в ЛС!{vpnAd}";
        }

        var captchaWelcomeData = new CaptchaWelcomeNotificationData(
            request.User, request.Chat, request.Reason, 0, mediaWarning, vpnAd);
        var sent = await SendUserNotificationWithReplyAsync(
            request.User, request.Chat, UserNotificationType.CaptchaWelcome, captchaWelcomeData, request.CancellationToken);
        
        // Удаляем приветствие через 20 секунд
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(20), request.CancellationToken);
                await _bot.DeleteMessage(request.Chat.Id, sent.MessageId, cancellationToken: request.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить приветственное сообщение");
            }
        }, request.CancellationToken);

        return sent;
    }

    /// <summary>
    /// Отправляет приветственное сообщение (новая версия без Request объекта)
    /// </summary>
    public async Task<Message?> SendWelcomeMessageAsync(User user, Chat chat, string reason = "приветствие", CancellationToken cancellationToken = default)
    {
        // Проверяем, отключены ли приветствия
        if (Config.DisableWelcome)
        {
            _logger.LogDebug("Приветственные сообщения отключены (DOORMAN_DISABLE_WELCOME=true)");
            return null;
        }

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
            greetMsg = $"👋 {mention}\n\n<b>Внимание:</b> первые три сообщения проходят антиспам-проверку, сообщения со стоп-словами и спамом будут удалены.\n\n⚠️ <b>Важно:</b> банальные приветствия без цели удаляются автоматически. Пишите конкретные вопросы!\n\nНе просите писать в ЛС!{vpnAd}";
        }
        else
        {
            mediaWarning = Config.IsMediaFilteringDisabledForChat(chat.Id) ? ", стикеры, документы" : ", изображения, стикеры, документы";
            greetMsg = $"👋 {mention}\n\n<b>Внимание!</b> первые три сообщения проходят антиспам-проверку, эмодзи{mediaWarning} и реклама запрещены — они могут удаляться автоматически.\n\n⚠️ <b>Важно:</b> банальные приветствия без цели удаляются автоматически. Пишите конкретные вопросы!\n\nНе просите писать в ЛС!{vpnAd}";
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
    private bool IsNoAdGroup(long chatId)
    {
        return _appConfig.NoVpnAdGroups.Contains(chatId);
    }
    
    public async Task<Message?> ForwardToAdminWithNotificationAsync(Message originalMessage, AdminNotificationType type, NotificationData data, CancellationToken cancellationToken = default)
    {
        try
        {
            // Пересылаем оригинальное сообщение
            var forward = await _bot.ForwardMessage(
                new ChatId(_appConfig.AdminChatId),
                originalMessage.Chat.Id,
                originalMessage.MessageId,
                cancellationToken: cancellationToken
            );
            
            // Отправляем уведомление с реплаем
            var template = _templates.GetAdminTemplate(type);
            var message = _templates.FormatNotificationTemplate(template, data);
            
            var notification = await _bot.SendMessage(
                _appConfig.AdminChatId,
                message,
                parseMode: ParseMode.Html,
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
                new ChatId(_appConfig.LogAdminChatId),
                originalMessage.Chat.Id,
                originalMessage.MessageId,
                cancellationToken: cancellationToken
            );
            
            // Отправляем уведомление с реплаем
            var template = _templates.GetLogTemplate(type);
            var message = _templates.FormatNotificationTemplate(template, data);
            
            var notification = await _bot.SendMessage(
                _appConfig.LogAdminChatId,
                message,
                parseMode: ParseMode.Html,
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
    
        /// <summary>
    /// Отправить уведомление об ошибке используя Request объект
    /// </summary>
    public async Task SendErrorNotificationAsync(SendErrorNotificationRequest request)
    {
        try
        {
            var errorData = new ErrorNotificationData(
                request.Exception, 
                request.Context, 
                request.User, 
                request.Chat);
            
            await SendAdminNotificationAsync(
                AdminNotificationType.SystemError, 
                errorData, 
                request.CancellationToken);
            
            _logger.LogDebug("Отправлено уведомление об ошибке в контексте {Context}", request.Context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке уведомления об ошибке в контексте {Context}", request.Context);
            throw;
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
    
    /// <summary>
    /// Отправляет сообщение капчи используя Request объект
    /// </summary>
    public async Task<Message> SendCaptchaMessageAsync(SendCaptchaMessageRequest request)
    {
        try
        {
            var sent = await _bot.SendMessage(
                request.Chat.Id,
                request.Message,
                parseMode: ParseMode.Html,
                replyParameters: request.ReplyParameters,
                replyMarkup: request.ReplyMarkup,
                cancellationToken: request.CancellationToken
            );
            
            _logger.LogDebug("Отправлено сообщение капчи в чат {ChatId}", request.Chat.Id);
            return sent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке сообщения капчи в чат {ChatId}", request.Chat.Id);
            throw;
        }
    }
    
    /// <summary>
    /// Получить доступ к шаблонам сообщений
    /// </summary>
    public MessageTemplates GetTemplates()
    {
        return _templates;
    }
} 
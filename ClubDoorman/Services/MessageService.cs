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
    
    public MessageService(
        ITelegramBotClientWrapper bot,
        ILogger<MessageService> logger,
        MessageTemplates templates,
        ILoggingConfigurationService configService,
        IServiceChatDispatcher serviceChatDispatcher)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _serviceChatDispatcher = serviceChatDispatcher ?? throw new ArgumentNullException(nameof(serviceChatDispatcher));
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
                parseMode: ParseMode.Markdown,
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
                parseMode: ParseMode.Markdown,
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
    /// Отправляет приветственное сообщение используя Request объект
    /// </summary>
    public async Task<Message> SendWelcomeMessageAsync(SendWelcomeMessageRequest request)
    {
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
    
    /// <summary>
    /// Отправить уведомление об ошибке
    /// </summary>
    public async Task SendErrorNotificationAsync(Exception ex, string context, User? user = null, Chat? chat = null, CancellationToken cancellationToken = default)
    {
        var request = new SendErrorNotificationRequest(ex, context, user, chat, cancellationToken);
        await SendErrorNotificationAsync(request);
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
    
    /// <summary>
    /// Отправить уведомление о AI анализе профиля с фото
    /// </summary>
    public async Task SendAiProfileAnalysisAsync(AiProfileAnalysisData data, CancellationToken cancellationToken = default)
    {
        try
        {
            await SendAdminNotificationAsync(AdminNotificationType.AiProfileAnalysis, data, cancellationToken);
            _logger.LogDebug("Отправлено уведомление о AI анализе профиля");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке уведомления о AI анализе профиля");
            throw;
        }
    }
    
    /// <summary>
    /// Отправляет сообщение капчи с кнопками
    /// </summary>
    public async Task<Message> SendCaptchaMessageAsync(Chat chat, string message, ReplyParameters? replyParameters, InlineKeyboardMarkup replyMarkup, CancellationToken cancellationToken = default)
    {
        var request = new SendCaptchaMessageRequest(chat, message, replyParameters, replyMarkup, cancellationToken);
        return await SendCaptchaMessageAsync(request);
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
                parseMode: ParseMode.Markdown,
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
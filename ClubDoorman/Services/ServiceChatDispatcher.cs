using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.Services;

/// <summary>
/// Реализация диспетчера сервис-чатов для разделения сообщений по админ-чату и лог-чату
/// </summary>
public class ServiceChatDispatcher : IServiceChatDispatcher
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<ServiceChatDispatcher> _logger;

    /// <summary>
    /// Создает экземпляр диспетчера сервис-чатов
    /// </summary>
    /// <param name="bot">Клиент Telegram бота</param>
    /// <param name="logger">Логгер</param>
    public ServiceChatDispatcher(
        ITelegramBotClientWrapper bot,
        ILogger<ServiceChatDispatcher> logger)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Отправляет уведомление в админ-чат (требует реакции через кнопки)
    /// </summary>
    public async Task SendToAdminChatAsync(NotificationData notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = FormatNotificationForAdminChat(notification);
            await _bot.SendMessageAsync(
                Config.AdminChatId,
                message,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                replyMarkup: GetAdminChatReplyMarkup(notification),
                cancellationToken: cancellationToken);

            _logger.LogDebug("✅ Уведомление отправлено в админ-чат: {NotificationType}", notification.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при отправке уведомления в админ-чат: {NotificationType}", notification.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// Отправляет уведомление в лог-чат (для анализа и корректировки фильтров)
    /// </summary>
    public async Task SendToLogChatAsync(NotificationData notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = FormatNotificationForLogChat(notification);
            var chatId = Config.LogAdminChatId != Config.AdminChatId ? Config.LogAdminChatId : Config.AdminChatId;
            
            await _bot.SendMessageAsync(
                chatId,
                message,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken: cancellationToken);

            _logger.LogDebug("📝 Уведомление отправлено в лог-чат: {NotificationType}", notification.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при отправке уведомления в лог-чат: {NotificationType}", notification.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// Определяет, куда отправить уведомление на основе его типа и содержимого
    /// </summary>
    public bool ShouldSendToAdminChat(NotificationData notification)
    {
        return notification switch
        {
            // Требуют реакции через кнопки - админ-чат
            SuspiciousMessageNotificationData => true,
            SuspiciousUserNotificationData => true,
            AiDetectNotificationData aiDetect => !aiDetect.IsAutoDelete, // Если не автоудаление - требует проверки
            
            // Редкие уведомления, полезные даже без реакции - админ-чат
            PrivateChatBanAttemptData => true,
            ChannelMessageNotificationData => true,
            UserRestrictedNotificationData => true,
            UserRemovedFromApprovedNotificationData => true,
            
            // Ошибки, требующие внимания - админ-чат
            ErrorNotificationData => true,
            
            // Всё остальное - лог-чат
            _ => false
        };
    }

    /// <summary>
    /// Форматирует уведомление для админ-чата
    /// </summary>
    private string FormatNotificationForAdminChat(NotificationData notification)
    {
        return notification switch
        {
            SuspiciousMessageNotificationData suspicious => FormatSuspiciousMessage(suspicious),
            SuspiciousUserNotificationData suspicious => FormatSuspiciousUser(suspicious),
            AiDetectNotificationData aiDetect => FormatAiDetect(aiDetect),
            PrivateChatBanAttemptData privateBan => FormatPrivateChatBanAttempt(privateBan),
            ChannelMessageNotificationData channel => FormatChannelMessage(channel),
            UserRestrictedNotificationData restricted => FormatUserRestricted(restricted),
            UserRemovedFromApprovedNotificationData removed => FormatUserRemovedFromApproved(removed),
            ErrorNotificationData error => FormatError(error),
            _ => FormatGenericNotification(notification)
        };
    }

    /// <summary>
    /// Форматирует уведомление для лог-чата
    /// </summary>
    private string FormatNotificationForLogChat(NotificationData notification)
    {
        return notification switch
        {
            AutoBanNotificationData autoBan => FormatAutoBanLog(autoBan),
            AiDetectNotificationData aiDetect when aiDetect.IsAutoDelete => FormatAiDetectLog(aiDetect),
            _ => FormatGenericLogNotification(notification)
        };
    }

    /// <summary>
    /// Получает разметку кнопок для админ-чата
    /// </summary>
    private InlineKeyboardMarkup? GetAdminChatReplyMarkup(NotificationData notification)
    {
        return notification switch
        {
            SuspiciousMessageNotificationData => new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("✅ Одобрить", "approve_message") },
                new[] { InlineKeyboardButton.WithCallbackData("❌ Спам", "spam_message") },
                new[] { InlineKeyboardButton.WithCallbackData("🚫 Бан", "ban_user") }
            }),
            SuspiciousUserNotificationData => new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("✅ Одобрить", "approve_user") },
                new[] { InlineKeyboardButton.WithCallbackData("🚫 Бан", "ban_user") }
            }),
            AiDetectNotificationData aiDetect when !aiDetect.IsAutoDelete => new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("✅ OK", "approve_ai_detect") },
                new[] { InlineKeyboardButton.WithCallbackData("❌ Спам", "spam_ai_detect") }
            }),
            _ => null
        };
    }

    // Методы форматирования для админ-чата
    private string FormatSuspiciousMessage(SuspiciousMessageNotificationData notification)
    {
        return $"🚨 <b>Подозрительное сообщение</b>\n\n" +
               $"👤 Пользователь: {FormatUser(notification.User)}\n" +
               $"💬 Чат: {FormatChat(notification.Chat)}\n" +
               $"📝 Сообщение: {notification.MessageText}\n" +
               $"🔗 Ссылка: {notification.MessageLink ?? "Нет"}";
    }

    private string FormatSuspiciousUser(SuspiciousUserNotificationData notification)
    {
        return $"🤔 <b>Подозрительный пользователь</b>\n\n" +
               $"👤 Пользователь: {FormatUser(notification.User)}\n" +
               $"💬 Чат: {FormatChat(notification.Chat)}\n" +
               $"🎭 Оценка мимикрии: {notification.MimicryScore:F2}\n" +
               $"📝 Первые сообщения:\n{string.Join("\n", notification.FirstMessages.Take(3))}";
    }

    private string FormatAiDetect(AiDetectNotificationData notification)
    {
        return $"🤖 <b>AI детект</b>\n\n" +
               $"👤 Пользователь: {FormatUser(notification.User)}\n" +
               $"💬 Чат: {FormatChat(notification.Chat)}\n" +
               $"🎭 Мимикрия: {notification.MimicryScore:F2}\n" +
               $"🤖 AI: {notification.AiScore:F2}\n" +
               $"📊 ML: {notification.MlScore:F2}\n" +
               $"📝 Сообщение: {notification.MessageText}\n" +
               $"🔗 Ссылка: {FormatMessageLink(notification.Chat, notification.MessageId)}";
    }

    private string FormatPrivateChatBanAttempt(PrivateChatBanAttemptData notification)
    {
        return $"⚠️ <b>Попытка бана в приватном чате</b>\n\n" +
               $"👤 Пользователь: {FormatUser(notification.User)}\n" +
               $"💬 Чат: {FormatChat(notification.Chat)}\n" +
               $"📝 Причина: {notification.Reason}";
    }

    private string FormatChannelMessage(ChannelMessageNotificationData notification)
    {
        return $"📢 <b>Сообщение от канала</b>\n\n" +
               $"📺 Канал: {notification.SenderChat.Title}\n" +
               $"💬 Чат: {FormatChat(notification.Chat)}\n" +
               $"📝 Сообщение: {notification.MessageText}";
    }

    private string FormatUserRestricted(UserRestrictedNotificationData notification)
    {
        return $"🚫 <b>Пользователь получил ограничения</b>\n\n" +
               $"👤 Пользователь: {FormatUser(notification.User)}\n" +
               $"💬 Чат: {notification.ChatTitle}\n" +
               $"📝 Причина: {notification.Reason}\n" +
               $"💬 Последнее сообщение: {notification.LastMessage}";
    }

    private string FormatUserRemovedFromApproved(UserRemovedFromApprovedNotificationData notification)
    {
        return $"❌ <b>Пользователь удален из списка одобренных</b>\n\n" +
               $"👤 Пользователь: {FormatUser(notification.User)}\n" +
               $"💬 Чат: {notification.ChatTitle}\n" +
               $"📝 Причина: {notification.Reason}";
    }

    private string FormatError(ErrorNotificationData notification)
    {
        return $"💥 <b>Ошибка</b>\n\n" +
               $"📝 Контекст: {notification.Context}\n" +
               $"❌ Ошибка: {notification.Exception.Message}\n" +
               $"👤 Пользователь: {FormatUser(notification.User)}\n" +
               $"💬 Чат: {FormatChat(notification.Chat)}";
    }

    // Методы форматирования для лог-чата
    private string FormatAutoBanLog(AutoBanNotificationData notification)
    {
        return $"🔨 <b>Автобан</b>\n\n" +
               $"👤 Пользователь: {FormatUser(notification.User)}\n" +
               $"💬 Чат: {FormatChat(notification.Chat)}\n" +
               $"📝 Тип: {notification.BanType}\n" +
               $"📝 Причина: {notification.Reason}\n" +
               $"🔗 Ссылка: {notification.MessageLink ?? "Нет"}";
    }

    private string FormatAiDetectLog(AiDetectNotificationData notification)
    {
        return $"🤖 <b>AI автоудаление</b>\n\n" +
               $"👤 Пользователь: {FormatUser(notification.User)}\n" +
               $"💬 Чат: {FormatChat(notification.Chat)}\n" +
               $"🎭 Мимикрия: {notification.MimicryScore:F2}\n" +
               $"🤖 AI: {notification.AiScore:F2}\n" +
               $"📊 ML: {notification.MlScore:F2}\n" +
               $"📝 Сообщение: {notification.MessageText}\n" +
               $"🔗 Ссылка: {FormatMessageLink(notification.Chat, notification.MessageId)}";
    }

    private string FormatGenericLogNotification(NotificationData notification)
    {
        return $"📝 <b>Лог</b>\n\n" +
               $"👤 Пользователь: {FormatUser(notification.User)}\n" +
               $"💬 Чат: {FormatChat(notification.Chat)}\n" +
               $"📝 Причина: {notification.Reason}\n" +
               $"🔗 Ссылка: {FormatMessageLink(notification.Chat, notification.MessageId)}";
    }

    private string FormatGenericNotification(NotificationData notification)
    {
        return $"ℹ️ <b>Уведомление</b>\n\n" +
               $"👤 Пользователь: {FormatUser(notification.User)}\n" +
               $"💬 Чат: {FormatChat(notification.Chat)}\n" +
               $"📝 Причина: {notification.Reason}\n" +
               $"🔗 Ссылка: {FormatMessageLink(notification.Chat, notification.MessageId)}";
    }

    // Вспомогательные методы форматирования
    private string FormatUser(User user)
    {
        var name = string.IsNullOrEmpty(user.FirstName) ? "Неизвестно" : user.FirstName;
        var lastName = string.IsNullOrEmpty(user.LastName) ? "" : $" {user.LastName}";
        var username = string.IsNullOrEmpty(user.Username) ? "" : $" (@{user.Username})";
        return $"{name}{lastName}{username} (ID: {user.Id})";
    }

    private string FormatChat(Chat chat)
    {
        var title = string.IsNullOrEmpty(chat.Title) ? "Неизвестно" : chat.Title;
        var username = string.IsNullOrEmpty(chat.Username) ? "" : $" (@{chat.Username})";
        return $"{title}{username} (ID: {chat.Id})";
    }

    private string FormatMessageLink(Chat chat, long? messageId)
    {
        if (!messageId.HasValue) return "Нет";
        
        return chat.Type switch
        {
            Telegram.Bot.Types.Enums.ChatType.Supergroup => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}",
            Telegram.Bot.Types.Enums.ChatType.Group when !string.IsNullOrEmpty(chat.Username) => $"https://t.me/{chat.Username}/{messageId}",
            _ => $"ID: {messageId}"
        };
    }
} 
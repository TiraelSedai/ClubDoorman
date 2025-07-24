using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using System.Runtime.Caching;
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
            _logger.LogDebug("🤖 ServiceChatDispatcher: отправляем уведомление типа {NotificationType}", notification.GetType().Name);
            
            // Специальная обработка для AI анализа профиля с фото
            if (notification is AiProfileAnalysisData aiProfileData)
            {
                _logger.LogDebug("🤖 ServiceChatDispatcher: используем специальную обработку для AI анализа профиля");
                await SendAiProfileAnalysisWithPhoto(aiProfileData, cancellationToken);
                return;
            }
            
            _logger.LogDebug("🤖 ServiceChatDispatcher: используем обычную обработку для типа {NotificationType}", notification.GetType().Name);

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
            AiProfileAnalysisData => true, // AI анализ профиля требует реакции
            
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
            AiProfileAnalysisData aiProfile => FormatAiProfileAnalysis(aiProfile),
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
            AiProfileAnalysisData aiProfile => new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("❌❌❌ ban", $"banprofile_{aiProfile.Chat.Id}_{aiProfile.User.Id}") },
                new[] { InlineKeyboardButton.WithCallbackData("✅✅✅ ok", $"aiOk_{aiProfile.Chat.Id}_{aiProfile.User.Id}") }
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

    private string FormatAiProfileAnalysis(AiProfileAnalysisData notification)
    {
        var reasonText = notification.Reason.Length > 350 ? 
            notification.Reason.Substring(0, 347) + "..." : 
            notification.Reason;
            
        var messageText = notification.MessageText.Length > 120 ? 
            notification.MessageText.Substring(0, 117) + "..." : 
            notification.MessageText;
            
        // Экранируем HTML символы
        var escapedUser = System.Net.WebUtility.HtmlEncode(FormatUser(notification.User));
        var escapedChat = System.Net.WebUtility.HtmlEncode(FormatChat(notification.Chat));
        var escapedReason = System.Net.WebUtility.HtmlEncode(reasonText);
        var escapedNameBio = System.Net.WebUtility.HtmlEncode(notification.NameBio);
        var escapedMessageText = System.Net.WebUtility.HtmlEncode(messageText);
            
        // РЕФАКТОРИНГ: Убираем информацию о пользователе, чате и профиле - она уже в фото
        var result = $"🤖 <b>AI анализ профиля</b>\n\n" +
                     $"📊 <b>Вероятность спама</b>: {notification.SpamProbability * 100:F1}%\n\n" +
                     $"🔍 <b>Причина</b>:\n<i>{escapedReason}</i>\n\n";
                     
        if (!string.IsNullOrEmpty(notification.AutomaticAction))
        {
            // Автоматическое действие не экранируем - мы его формируем сами и знаем что там безопасно
            result += $"⚡ <b>Автоматическое действие</b>:\n<b>{notification.AutomaticAction}</b>\n\n";
        }
        
        result += $"🔗 <b>Ссылка</b>: {FormatMessageLink(notification.Chat, notification.MessageId)}";
        
        return result;
    }

    private async Task SendAiProfileAnalysisWithPhoto(AiProfileAnalysisData data, CancellationToken cancellationToken)
    {
        _logger.LogDebug("🤖 SendAiProfileAnalysisWithPhoto: начало обработки для пользователя {UserId}", data.User.Id);
        
        // Кэшируем данные для кнопок
        var callbackDataBan = $"banprofile_{data.Chat.Id}_{data.User.Id}";
        MemoryCache.Default.Add(callbackDataBan, data, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });
        
        ReplyParameters? replyParams = null;
        
        // 1. Если есть фото - отправляем его отдельно с краткой подписью
        _logger.LogDebug("🤖 AI анализ профиля: проверяем фото для пользователя {UserId}, PhotoBytes: {PhotoBytesLength}", 
            data.User.Id, data.PhotoBytes?.Length ?? 0);
            
        if (data.PhotoBytes?.Length > 0)
        {
            _logger.LogDebug("🤖 AI анализ профиля: отправляем фото для пользователя {UserId}", data.User.Id);
            
            // РЕФАКТОРИНГ: Новый формат caption - добавляем информацию о чате, убираем лишнее
            var escapedUser = System.Net.WebUtility.HtmlEncode(FormatUser(data.User));
            var escapedChat = System.Net.WebUtility.HtmlEncode(FormatChat(data.Chat)); 
            var escapedNameBio = System.Net.WebUtility.HtmlEncode(data.NameBio);
            var escapedMessageText = System.Net.WebUtility.HtmlEncode(data.MessageText.Length > 120 ? 
                data.MessageText.Substring(0, 117) + "..." : data.MessageText);
            
            var photoCaption = $"<b>👤 Пользователь:</b> {escapedUser}\n" +
                              $"<b>💬 Чат:</b> {escapedChat}\n\n" +
                              $"<b>📋 Профиль:</b>\n{escapedNameBio}\n\n" +
                              $"<b>💬 Сообщение:</b>\n{escapedMessageText}";
                              
            // Обрезаем caption если слишком длинный (лимит Telegram 1024 символа)
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
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken: cancellationToken
            );
            replyParams = new ReplyParameters { MessageId = photoMsg.MessageId };
            
            _logger.LogDebug("🤖 AI анализ профиля: фото отправлено для пользователя {UserId}", data.User.Id);
        }
        else
        {
            _logger.LogDebug("🤖 AI анализ профиля: фото отсутствует для пользователя {UserId}", data.User.Id);
        }
        
        // 2. РЕФАКТОРИНГ: ВСЕГДА пересылаем сообщение пользователя между фото и AI анализом
        // Сообщение удаляется ПОСЛЕ отправки AI анализа, поэтому сейчас оно ещё доступно
        if (data.MessageId.HasValue)
        {
            try
            {
                var forwardedMsg = await _bot.ForwardMessage(
                    chatId: Config.AdminChatId,
                    fromChatId: data.Chat.Id,
                    messageId: (int)data.MessageId.Value,
                    cancellationToken: cancellationToken
                );
                // Если пересылка удалась, используем её как основу для ответа на AI анализ
                replyParams = new ReplyParameters { MessageId = forwardedMsg.MessageId };
                _logger.LogDebug("🤖 AI анализ профиля: сообщение пользователя переслано для пользователя {UserId}", data.User.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "🤖 AI анализ профиля: не удалось переслать сообщение пользователя {UserId}", data.User.Id);
            }
        }
        else
        {
            _logger.LogDebug("🤖 AI анализ профиля: сообщение пользователя не пересылается - MessageId отсутствует для пользователя {UserId}", data.User.Id);
        }
        
        // 3. Основное сообщение с анализом
        var message = FormatAiProfileAnalysis(data);
        
        var mainMessage = await _bot.SendMessageAsync(
            Config.AdminChatId,
            message,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            replyMarkup: GetAdminChatReplyMarkup(data),
            replyParameters: replyParams,
            cancellationToken: cancellationToken
        );
        

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
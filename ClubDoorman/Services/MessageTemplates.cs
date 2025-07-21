using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Система шаблонов сообщений для Telegram
/// </summary>
public class MessageTemplates
{
    private readonly Dictionary<AdminNotificationType, string> _adminTemplates = new()
    {
        [AdminNotificationType.AutoBanBlacklist] = 
            "🚫 Автобан по блэклисту lols.bot (первое сообщение)\n" +
            "Юзер {UserFullName} из чата {ChatTitle}\n" +
            "{MessageLink}",
            
        [AdminNotificationType.AutoBanFromBlacklist] = 
            "🚫 Автобан из блэклиста: {Reason}\n" +
            "Юзер {UserFullName} из чата {ChatTitle}\n" +
            "{MessageLink}",
            
        [AdminNotificationType.PrivateChatBanAttempt] = 
            "⚠️ Попытка бана в приватном чате: {Reason}\n" +
            "Юзер {UserFullName} из чата {ChatTitle}\n" +
            "Операция невозможна в приватных чатах",
            
        [AdminNotificationType.BanForLongName] = 
            "{BanType} в чате *{ChatTitle}*: {Reason}",
            
        [AdminNotificationType.BanChannel] = 
            "Сообщение удалено, в чате {ChatTitle} забанен канал {ChannelTitle}",
            
        [AdminNotificationType.RemovedFromApproved] = 
            "⚠️ Пользователь {UserFullName} удален из списка одобренных после автобана по блэклисту",
            
        [AdminNotificationType.ChannelMessage] = 
            "Сообщение от канала {ChannelTitle} в чате {ChatTitle} - репорт в админ-чат",
            
        [AdminNotificationType.SuspiciousUser] = 
            "🔍 *Подозрительный пользователь обнаружен*\n\n" +
            "👤 Пользователь: [{UserFullName}](tg://user?id={UserId})\n" +
            "🏠 Чат: *{ChatTitle}*\n" +
            "📊 Оценка мимикрии: *{MimicryScore:F2}*\n" +
            "🕐 Помечен как подозрительный: {SuspiciousAt:yyyy-MM-dd HH:mm}\n\n" +
            "📝 Первые сообщения:\n{FirstMessages}\n\n" +
            "✅ Для одобрения нужно ещё {RequiredMessages} хороших сообщений",
            
        [AdminNotificationType.AiProfileAnalysis] = 
            "🤖 *AI анализ профиля*\n\n" +
            "👤 Пользователь: [{UserFullName}](tg://user?id={UserId})\n" +
            "🏠 Чат: *{ChatTitle}*\n" +
            "📊 Вероятность спама: *{SpamProbability:F2}*\n" +
            "🔍 Причина: {AiReason}\n\n" +
            "📝 Имя и био:\n`{NameBio}`",
            
        [AdminNotificationType.ModerationError] = 
            "❌ Ошибка модерации: {Context}\n" +
            "Пользователь: {UserFullName}\n" +
            "Чат: {ChatTitle}\n" +
            "Ошибка: {ErrorMessage}",
            
        [AdminNotificationType.SystemError] = 
            "💥 Системная ошибка: {Context}\n" +
            "Ошибка: {ErrorMessage}",
            
        [AdminNotificationType.AutoBan] = 
            "Авто-бан: {Reason}\n" +
            "Юзер {UserFullName} из чата {ChatTitle}\n" +
            "{MessageLink}",
            
        [AdminNotificationType.SuspiciousMessage] = 
            "⚠️ *Подозрительное сообщение* - требует ручной проверки. Сообщение *НЕ удалено*.\n" +
            "Пользователь [{UserFullName}](tg://user?id={UserId}) в чате *{ChatTitle}*\n" +
            "Сообщение: {MessageText}",
            
        [AdminNotificationType.ChannelError] = 
            "⚠️ Не могу забанить канал в чате {ChatTitle}. Не хватает могущества?",
            
                    [AdminNotificationType.UserCleanup] = 
                "🧹 Пользователь {UserFullName} очищен из всех списков после автобана",
                
            [AdminNotificationType.AiProfileAnalysis] = 
                "🤖 AI: Вероятность что это профиль бейт спаммер {SpamProbability}%. Даём ридонли на 10 минут\n{Reason}\nЮзер {UserFullName} из чата {ChatTitle}"
    };
    
    private readonly Dictionary<LogNotificationType, string> _logTemplates = new()
    {
        [LogNotificationType.AutoBanBlacklist] = 
            "🚫 Автобан по блэклисту lols.bot (первое сообщение)\n" +
            "Юзер {UserFullName} из чата {ChatTitle}\n" +
            "{MessageLink}",
            
        [LogNotificationType.AutoBanFromBlacklist] = 
            "🚫 Автобан из блэклиста: {Reason}\n" +
            "Юзер {UserFullName} из чата {ChatTitle}\n" +
            "{MessageLink}",
            
        [LogNotificationType.BanForLongName] = 
            "🚫 Бан за длинное имя: {Reason}\n" +
            "Юзер {UserFullName} из чата {ChatTitle}",
            
        [LogNotificationType.BanChannel] = 
            "🚫 Бан канала {ChannelTitle} в чате {ChatTitle}",
            
        [LogNotificationType.SuspiciousUser] = 
            "🔍 Подозрительный пользователь: {UserFullName} в чате {ChatTitle}\n" +
            "Оценка мимикрии: {MimicryScore:F2}",
            
        [LogNotificationType.AiProfileAnalysis] = 
            "🤖 AI анализ профиля: {UserFullName} в чате {ChatTitle}\n" +
            "Вероятность спама: {SpamProbability:F2}",
            
        [LogNotificationType.CriticalError] = 
            "💥 Критическая ошибка: {Context}\n" +
            "Ошибка: {ErrorMessage}",
            
        [LogNotificationType.ChannelMessage] = 
            "📢 Сообщение от канала {ChannelTitle} в чате {ChatTitle}"
    };
    
    private readonly Dictionary<UserNotificationType, string> _userTemplates = new()
    {
        [UserNotificationType.ModerationWarning] = 
            "👋 {UserMention}, вы пока *новичок* в этом чате\\.\n\n" +
            "*Первые 3 сообщения* проходят антиспам\\-проверку:\n" +
            "• нельзя эмодзи, рекламу и *стоп\\-слова*\n" +
            "• работает ML\\-анализ",
            
        [UserNotificationType.MessageDeleted] = 
            "❌ Ваше сообщение удалено: {Reason}",
            
        [UserNotificationType.UserBanned] = 
            "🚫 Вы забанены в этом чате: {Reason}",
            
        [UserNotificationType.UserRestricted] = 
            "⚠️ Вы ограничены в этом чате: {Reason}",
            
        [UserNotificationType.CaptchaShown] = 
            "🧩 Пожалуйста, пройдите капчу для входа в чат",
            
        [UserNotificationType.Welcome] = 
            "👋 Добро пожаловать в чат!",
            
        [UserNotificationType.Warning] = 
            "⚠️ {Reason}",
            
                    [UserNotificationType.Success] = 
                "✅ {Reason}",
                
            [UserNotificationType.Welcome] = 
                "{Reason}"
    };
    
    /// <summary>
    /// Получить шаблон для админского уведомления
    /// </summary>
    public string GetAdminTemplate(AdminNotificationType type) => _adminTemplates[type];
    
    /// <summary>
    /// Получить шаблон для лог-уведомления
    /// </summary>
    public string GetLogTemplate(LogNotificationType type) => _logTemplates[type];
    
    /// <summary>
    /// Получить шаблон для пользовательского уведомления
    /// </summary>
    public string GetUserTemplate(UserNotificationType type) => _userTemplates[type];
    
    /// <summary>
    /// Форматировать шаблон с данными
    /// </summary>
    public string FormatTemplate(string template, object data)
    {
        var result = template;
        
        // Заменяем плейсхолдеры на значения из data
        var properties = data.GetType().GetProperties();
        foreach (var property in properties)
        {
            var placeholder = $"{{{property.Name}}}";
            var value = property.GetValue(data)?.ToString() ?? "";
            
            result = result.Replace(placeholder, value);
        }
        
        return result;
    }
    
    /// <summary>
    /// Форматировать шаблон с данными уведомления
    /// </summary>
    public string FormatNotificationTemplate(string template, NotificationData data)
    {
        var result = template;
        
        // Базовые поля
        result = result.Replace("{UserFullName}", Utils.FullName(data.User));
        result = result.Replace("{UserId}", data.User.Id.ToString());
        result = result.Replace("{ChatTitle}", data.Chat.Title ?? data.Chat.Id.ToString());
        result = result.Replace("{ChatId}", data.Chat.Id.ToString());
        result = result.Replace("{Reason}", data.Reason ?? "");
        result = result.Replace("{MessageId}", data.MessageId?.ToString() ?? "");
        
        // Специфичные поля для разных типов данных
        if (data is AutoBanNotificationData autoBanData)
        {
            result = result.Replace("{BanType}", autoBanData.BanType);
            result = result.Replace("{MessageLink}", autoBanData.MessageLink ?? "");
        }
        else if (data is SuspiciousUserNotificationData suspiciousData)
        {
            result = result.Replace("{MimicryScore}", suspiciousData.MimicryScore.ToString("F2"));
            result = result.Replace("{SuspiciousAt}", suspiciousData.SuspiciousAt.ToString("yyyy-MM-dd HH:mm"));
            result = result.Replace("{FirstMessages}", FormatFirstMessages(suspiciousData.FirstMessages));
            result = result.Replace("{RequiredMessages}", Config.SuspiciousToApprovedMessageCount.ToString());
        }

        else if (data is ErrorNotificationData errorData)
        {
            result = result.Replace("{Context}", errorData.Context);
            result = result.Replace("{ErrorMessage}", errorData.Exception.Message);
        }
        else if (data is ChannelMessageNotificationData channelData)
        {
            result = result.Replace("{ChannelTitle}", channelData.SenderChat.Title ?? channelData.SenderChat.Id.ToString());
            result = result.Replace("{MessageText}", channelData.MessageText);
        }
        else if (data is SuspiciousMessageNotificationData suspiciousMsgData)
        {
            result = result.Replace("{MessageText}", suspiciousMsgData.MessageText);
            result = result.Replace("{MessageLink}", suspiciousMsgData.MessageLink ?? "");
        }
                    else if (data is UserCleanupNotificationData cleanupData)
            {
                result = result.Replace("{CleanupReason}", cleanupData.CleanupReason);
            }
            else if (data is AiProfileAnalysisData aiProfileData)
            {
                result = result.Replace("{SpamProbability}", (aiProfileData.SpamProbability * 100).ToString("F1"));
                result = result.Replace("{Reason}", aiProfileData.Reason);
                result = result.Replace("{NameBio}", aiProfileData.NameBio);
                result = result.Replace("{MessageText}", aiProfileData.MessageText);
            }
        
        return result;
    }
    
    private string FormatFirstMessages(List<string> messages)
    {
        if (messages.Count == 0) return "Нет сообщений";
        
        var result = "";
        for (int i = 0; i < Math.Min(messages.Count, 5); i++)
        {
            var msg = messages[i];
            if (msg.Length > 50)
                msg = msg.Substring(0, 50) + "...";
            result += $"{i + 1}. `{msg}`\n";
        }
        
        return result;
    }
} 
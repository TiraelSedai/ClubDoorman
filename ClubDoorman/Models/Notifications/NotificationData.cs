using Telegram.Bot.Types;

namespace ClubDoorman.Models.Notifications;

/// <summary>
/// Базовый класс для данных уведомлений
/// </summary>
public abstract class NotificationData
{
    /// <summary>
    /// Пользователь
    /// </summary>
    public User User { get; set; }
    
    /// <summary>
    /// Чат
    /// </summary>
    public Chat Chat { get; set; }
    
    /// <summary>
    /// Причина
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// ID сообщения
    /// </summary>
    public long? MessageId { get; set; }
    
    protected NotificationData(User user, Chat chat, string? reason = null, long? messageId = null)
    {
        User = user;
        Chat = chat;
        Reason = reason;
        MessageId = messageId;
    }
}

/// <summary>
/// Данные для уведомления об автобане
/// </summary>
public class AutoBanNotificationData : NotificationData
{
    /// <summary>
    /// Тип автобана
    /// </summary>
    public string BanType { get; set; }
    
    /// <summary>
    /// Ссылка на сообщение
    /// </summary>
    public string? MessageLink { get; set; }
    
    public AutoBanNotificationData(User user, Chat chat, string banType, string? reason = null, long? messageId = null, string? messageLink = null) 
        : base(user, chat, reason, messageId)
    {
        BanType = banType;
        MessageLink = messageLink;
    }
}

/// <summary>
/// Данные для уведомления о попытке бана в приватном чате
/// </summary>
public class PrivateChatBanAttemptData : NotificationData
{
    public PrivateChatBanAttemptData(User user, Chat chat, string reason) 
        : base(user, chat, reason)
    {
    }
}

/// <summary>
/// Данные для уведомления о подозрительном пользователе
/// </summary>
public class SuspiciousUserNotificationData : NotificationData
{
    /// <summary>
    /// Оценка мимикрии
    /// </summary>
    public double MimicryScore { get; set; }
    
    /// <summary>
    /// Первые сообщения
    /// </summary>
    public List<string> FirstMessages { get; set; }
    
    /// <summary>
    /// Время пометки как подозрительный
    /// </summary>
    public DateTime SuspiciousAt { get; set; }
    
    public SuspiciousUserNotificationData(User user, Chat chat, double mimicryScore, List<string> firstMessages, DateTime suspiciousAt) 
        : base(user, chat)
    {
        MimicryScore = mimicryScore;
        FirstMessages = firstMessages;
        SuspiciousAt = suspiciousAt;
    }
}

/// <summary>
/// Данные для уведомления об AI анализе
/// </summary>
public class AiProfileAnalysisData : NotificationData
{
    /// <summary>
    /// Вероятность спама
    /// </summary>
    public double SpamProbability { get; set; }
    
    /// <summary>
    /// Причина
    /// </summary>
    public string AiReason { get; set; }
    
    /// <summary>
    /// Байты фото профиля
    /// </summary>
    public byte[]? PhotoBytes { get; set; }
    
    /// <summary>
    /// Имя и био
    /// </summary>
    public string NameBio { get; set; }
    
    public AiProfileAnalysisData(User user, Chat chat, double spamProbability, string aiReason, string nameBio, byte[]? photoBytes = null) 
        : base(user, chat)
    {
        SpamProbability = spamProbability;
        AiReason = aiReason;
        NameBio = nameBio;
        PhotoBytes = photoBytes;
    }
}

/// <summary>
/// Данные для уведомления об ошибке
/// </summary>
public class ErrorNotificationData : NotificationData
{
    /// <summary>
    /// Исключение
    /// </summary>
    public Exception Exception { get; set; }
    
    /// <summary>
    /// Контекст ошибки
    /// </summary>
    public string Context { get; set; }
    
    public ErrorNotificationData(Exception exception, string context, User? user = null, Chat? chat = null) 
        : base(user ?? new User(), chat ?? new Chat())
    {
        Exception = exception;
        Context = context;
    }
} 
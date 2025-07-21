using ClubDoorman.Infrastructure;
using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Централизованный логгер для отслеживания пользовательского флоу
/// </summary>
public class UserFlowLogger : IUserFlowLogger
{
    private readonly ILogger<UserFlowLogger> _logger;

    public UserFlowLogger(ILogger<UserFlowLogger> logger)
    {
        _logger = logger;
    }

    public void LogUserJoined(User user, Chat chat, string? joinReason = null)
    {
        var reasonText = !string.IsNullOrEmpty(joinReason) ? $" ({joinReason})" : "";
        _logger.LogInformation("🚪 ПОЛЬЗОВАТЕЛЬ ВОШЕЛ: {User} (id={UserId}) в чат '{ChatTitle}' (id={ChatId}){ReasonText}", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reasonText);
    }

    public void LogCaptchaShown(User user, Chat chat)
    {
        _logger.LogInformation("🧩 КАПЧА ПОКАЗАНА: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id);
    }

    public void LogCaptchaPassed(User user, Chat chat)
    {
        _logger.LogInformation("✅ КАПЧА ПРОЙДЕНА: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id);
    }

    public void LogCaptchaFailed(User user, Chat chat)
    {
        _logger.LogInformation("❌ КАПЧА НЕ ПРОЙДЕНА: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id);
    }

    public void LogWelcomeShown(User user, Chat chat)
    {
        _logger.LogInformation("👋 ПРИВЕТСТВИЕ ПОКАЗАНО: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id);
    }

    public void LogWelcomeRemoved(User user, Chat chat)
    {
        _logger.LogInformation("🗑️ ПРИВЕТСТВИЕ УДАЛЕНО: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id);
    }

    public void LogFirstMessage(User user, Chat chat, string messageText)
    {
        var truncatedText = messageText.Length > 100 ? messageText.Substring(0, 100) + "..." : messageText;
        _logger.LogInformation("📝 ПЕРВОЕ СООБЩЕНИЕ: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}): {MessageText}", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, truncatedText);
    }

    public void LogModerationStarted(User user, Chat chat, string messageText)
    {
        var truncatedText = messageText.Length > 100 ? messageText.Substring(0, 100) + "..." : messageText;
        _logger.LogInformation("🔍 МОДЕРАЦИЯ НАЧАТА: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}): {MessageText}", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, truncatedText);
    }

    public void LogSpamListCheck(User user, Chat chat, bool passed, string? reason = null)
    {
        var status = passed ? "✅ ПРОЙДЕНО" : "❌ ЗАБЛОКИРОВАНО";
        var reasonText = !string.IsNullOrEmpty(reason) ? $" - {reason}" : "";
        _logger.LogInformation("📋 СПАМ-СПИСКИ {Status}: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}){ReasonText}", 
            status, Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reasonText);
    }

    public void LogStopWordsCheck(User user, Chat chat, bool passed, string? reason = null)
    {
        var status = passed ? "✅ ПРОЙДЕНО" : "❌ ЗАБЛОКИРОВАНО";
        var reasonText = !string.IsNullOrEmpty(reason) ? $" - {reason}" : "";
        _logger.LogInformation("🚫 СТОП-СЛОВА {Status}: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}){ReasonText}", 
            status, Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reasonText);
    }

    public void LogKnownSpamCheck(User user, Chat chat, bool passed, string? reason = null)
    {
        var status = passed ? "✅ ПРОЙДЕНО" : "❌ ЗАБЛОКИРОВАНО";
        var reasonText = !string.IsNullOrEmpty(reason) ? $" - {reason}" : "";
        _logger.LogInformation("🎯 ИЗВЕСТНЫЙ СПАМ {Status}: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}){ReasonText}", 
            status, Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reasonText);
    }

    public void LogMlAnalysis(User user, Chat chat, bool isSpam, double score, string? reason = null)
    {
        var status = isSpam ? "❌ СПАМ" : "✅ НЕ СПАМ";
        var reasonText = !string.IsNullOrEmpty(reason) ? $" - {reason}" : "";
        _logger.LogInformation("🤖 ML-АНАЛИЗ {Status} (скор {Score:F3}): {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}){ReasonText}", 
            status, score, Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reasonText);
    }

    public void LogModerationResult(User user, Chat chat, string action, string reason, double? confidence = null)
    {
        var confidenceText = confidence.HasValue ? $" (уверенность: {confidence.Value:F3})" : "";
        _logger.LogInformation("🎯 РЕЗУЛЬТАТ МОДЕРАЦИИ: {Action} - {Reason}{ConfidenceText} | {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})", 
            action, reason, confidenceText, Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id);
    }

    public void LogUserApproved(User user, Chat chat, string reason)
    {
        _logger.LogInformation("✅ ПОЛЬЗОВАТЕЛЬ ОДОБРЕН: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) - {Reason}", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reason);
    }

    public void LogUserBanned(User user, Chat chat, string reason)
    {
        _logger.LogInformation("🚫 ПОЛЬЗОВАТЕЛЬ ЗАБАНЕН: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) - {Reason}", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reason);
    }

    public void LogUserRestricted(User user, Chat chat, string reason, TimeSpan? duration = null)
    {
        var durationText = duration.HasValue ? $" на {duration.Value.TotalMinutes:F0} минут" : "";
        _logger.LogInformation("⚠️ ПОЛЬЗОВАТЕЛЬ ОГРАНИЧЕН{durationText}: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) - {Reason}", 
            durationText, Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reason);
    }
} 
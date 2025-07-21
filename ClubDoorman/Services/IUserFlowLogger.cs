using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Централизованный логгер для отслеживания пользовательского флоу
/// </summary>
public interface IUserFlowLogger
{
    /// <summary>
    /// Логирует вход пользователя в чат
    /// </summary>
    void LogUserJoined(User user, Chat chat, string? joinReason = null);
    
    /// <summary>
    /// Логирует показ капчи пользователю
    /// </summary>
    void LogCaptchaShown(User user, Chat chat);
    
    /// <summary>
    /// Логирует прохождение капчи пользователем
    /// </summary>
    void LogCaptchaPassed(User user, Chat chat);
    
    /// <summary>
    /// Логирует неудачное прохождение капчи
    /// </summary>
    void LogCaptchaFailed(User user, Chat chat);
    
    /// <summary>
    /// Логирует показ приветствия пользователю
    /// </summary>
    void LogWelcomeShown(User user, Chat chat);
    
    /// <summary>
    /// Логирует удаление приветствия
    /// </summary>
    void LogWelcomeRemoved(User user, Chat chat);
    
    /// <summary>
    /// Логирует первое сообщение пользователя
    /// </summary>
    void LogFirstMessage(User user, Chat chat, string messageText);
    
    /// <summary>
    /// Логирует начало модерации сообщения
    /// </summary>
    void LogModerationStarted(User user, Chat chat, string messageText);
    
    /// <summary>
    /// Логирует результат проверки спам-списков
    /// </summary>
    void LogSpamListCheck(User user, Chat chat, bool passed, string? reason = null);
    
    /// <summary>
    /// Логирует результат проверки стоп-слов
    /// </summary>
    void LogStopWordsCheck(User user, Chat chat, bool passed, string? reason = null);
    
    /// <summary>
    /// Логирует результат проверки известного спама
    /// </summary>
    void LogKnownSpamCheck(User user, Chat chat, bool passed, string? reason = null);
    
    /// <summary>
    /// Логирует результат ML-анализа
    /// </summary>
    void LogMlAnalysis(User user, Chat chat, bool isSpam, double score, string? reason = null);
    
    /// <summary>
    /// Логирует финальный результат модерации
    /// </summary>
    void LogModerationResult(User user, Chat chat, string action, string reason, double? confidence = null);
    
    /// <summary>
    /// Логирует одобрение пользователя
    /// </summary>
    void LogUserApproved(User user, Chat chat, string reason);
    
    /// <summary>
    /// Логирует бан пользователя
    /// </summary>
    void LogUserBanned(User user, Chat chat, string reason);
    
    /// <summary>
    /// Логирует ограничение пользователя
    /// </summary>
    void LogUserRestricted(User user, Chat chat, string reason, TimeSpan? duration = null);
} 
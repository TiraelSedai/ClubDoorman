namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс для конфигурации приложения
/// Заменяет статические свойства в Config.cs для лучшей тестируемости
/// </summary>
public interface IAppConfig
{
    /// <summary>
    /// API токен для OpenRouter
    /// </summary>
    string? OpenRouterApi { get; }
    
    /// <summary>
    /// Включено ли обнаружение подозрительных пользователей
    /// </summary>
    bool SuspiciousDetectionEnabled { get; }
    
    /// <summary>
    /// Порог мимикрии для обнаружения подозрительных пользователей
    /// </summary>
    double MimicryThreshold { get; }
    
    /// <summary>
    /// Количество сообщений для перехода из подозрительных в одобренные
    /// </summary>
    int SuspiciousToApprovedMessageCount { get; }
    
    /// <summary>
    /// ID админского чата
    /// </summary>
    long AdminChatId { get; }
    
    /// <summary>
    /// ID чата для логирования
    /// </summary>
    long LogAdminChatId { get; }
    
    /// <summary>
    /// Список чатов с включенным AI
    /// </summary>
    HashSet<long> AiEnabledChats { get; }
    
    /// <summary>
    /// Включен ли AI для конкретного чата
    /// </summary>
    bool IsAiEnabledForChat(long chatId);
    
    /// <summary>
    /// Разрешён ли чат для работы бота
    /// </summary>
    bool IsChatAllowed(long chatId);
    
    /// <summary>
    /// Разрешён ли приватный старт
    /// </summary>
    bool IsPrivateStartAllowed();
    
    /// <summary>
    /// API токен бота Telegram
    /// </summary>
    string BotApi { get; }
    
    /// <summary>
    /// Токен сервиса клуба
    /// </summary>
    string? ClubServiceToken { get; }
    
    /// <summary>
    /// URL клуба
    /// </summary>
    string ClubUrl { get; }
} 
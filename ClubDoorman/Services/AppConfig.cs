using ClubDoorman.Infrastructure;

namespace ClubDoorman.Services;

/// <summary>
/// Реализация конфигурации приложения
/// Переносит логику из статического класса Config.cs для лучшей тестируемости
/// </summary>
public class AppConfig : IAppConfig
{
    /// <summary>
    /// API токен для OpenRouter
    /// </summary>
    public string? OpenRouterApi => Config.OpenRouterApi;
    
    /// <summary>
    /// Включено ли обнаружение подозрительных пользователей
    /// </summary>
    public bool SuspiciousDetectionEnabled => Config.SuspiciousDetectionEnabled;
    
    /// <summary>
    /// Порог мимикрии для обнаружения подозрительных пользователей
    /// </summary>
    public double MimicryThreshold => Config.MimicryThreshold;
    
    /// <summary>
    /// Количество сообщений для перехода из подозрительных в одобренные
    /// </summary>
    public int SuspiciousToApprovedMessageCount => Config.SuspiciousToApprovedMessageCount;
    
    /// <summary>
    /// ID админского чата
    /// </summary>
    public long AdminChatId => Config.AdminChatId;
    
    /// <summary>
    /// ID чата для логирования
    /// </summary>
    public long LogAdminChatId => Config.LogAdminChatId;
    
    /// <summary>
    /// Список чатов с включенным AI
    /// </summary>
    public HashSet<long> AiEnabledChats => Config.AiEnabledChats;
    
    /// <summary>
    /// Включен ли AI для конкретного чата
    /// </summary>
    public bool IsAiEnabledForChat(long chatId) => Config.IsAiEnabledForChat(chatId);
    
    /// <summary>
    /// Разрешён ли чат для работы бота
    /// </summary>
    public bool IsChatAllowed(long chatId) => Config.IsChatAllowed(chatId);
    
    /// <summary>
    /// Разрешён ли приватный старт
    /// </summary>
    public bool IsPrivateStartAllowed() => Config.IsPrivateStartAllowed();
    
    /// <summary>
    /// API токен бота Telegram
    /// </summary>
    public string BotApi => Config.BotApi;
    
    /// <summary>
    /// Токен сервиса клуба
    /// </summary>
    public string? ClubServiceToken => Config.ClubServiceToken;
    
    /// <summary>
    /// URL клуба
    /// </summary>
    public string ClubUrl => Config.ClubUrl;
    
    /// <summary>
    /// Отключенные чаты
    /// </summary>
    public HashSet<long> DisabledChats => Config.DisabledChats;
    
    /// <summary>
    /// Whitelist групп - если указан, бот работает только в этих группах
    /// </summary>
    public HashSet<long> WhitelistChats => Config.WhitelistChats;
    
    /// <summary>
    /// Группы, где не показывать рекламу
    /// </summary>
    public HashSet<long> NoVpnAdGroups => Config.NoVpnAdGroups;
    
    /// <summary>
    /// Группы, в которых отключена капча
    /// </summary>
    public HashSet<long> NoCaptchaGroups => Config.NoCaptchaGroups;
    
    /// <summary>
    /// Включен ли фильтр ссылок
    /// </summary>
    public bool TextMentionFilterEnabled => Config.TextMentionFilterEnabled;
    
    /// <summary>
    /// Количество повторных нарушений ML фильтра перед баном
    /// </summary>
    public int MlViolationsBeforeBan => Config.MlViolationsBeforeBan;
    
    /// <summary>
    /// Количество повторных нарушений стоп-слов перед баном
    /// </summary>
    public int StopWordsViolationsBeforeBan => Config.StopWordsViolationsBeforeBan;
    
    /// <summary>
    /// Количество повторных нарушений эмодзи перед баном
    /// </summary>
    public int EmojiViolationsBeforeBan => Config.EmojiViolationsBeforeBan;
    
    /// <summary>
    /// Количество повторных нарушений lookalike символов перед баном
    /// </summary>
    public int LookalikeViolationsBeforeBan => Config.LookalikeViolationsBeforeBan;
    
    /// <summary>
    /// Количество повторных нарушений банальных приветствий перед баном
    /// </summary>
    public int BoringGreetingsViolationsBeforeBan => Config.BoringGreetingsViolationsBeforeBan;
    
            /// <summary>
        /// Отправлять уведомления о банах за повторные нарушения в админ-чат вместо лог-чата
        /// </summary>
        public bool RepeatedViolationsBanToAdminChat => Config.RepeatedViolationsBanToAdminChat;
    
} 
using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для управления банами пользователей (заглушка для совместимости с тестами)
/// </summary>
public interface IUserBanService
{
    /// <summary>
    /// Банит пользователя за длинное имя
    /// </summary>
    Task BanUserForLongNameAsync(Message? userJoinMessage, User user, string reason, TimeSpan? banDuration, CancellationToken cancellationToken);

    /// <summary>
    /// Банит пользователя из блэклиста
    /// </summary>
    Task BanBlacklistedUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken);

    /// <summary>
    /// Автоматически банит пользователя за нарушение
    /// </summary>
    Task AutoBanAsync(Message message, string reason, CancellationToken cancellationToken);

    /// <summary>
    /// Автоматически банит канал
    /// </summary>
    Task AutoBanChannelAsync(Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Обрабатывает бан пользователя из блэклиста
    /// </summary>
    Task HandleBlacklistBanAsync(Message message, User user, Chat chat, CancellationToken cancellationToken);

    /// <summary>
    /// Отслеживает нарушение и банит пользователя при достижении лимита
    /// </summary>
    Task TrackViolationAndBanIfNeededAsync(Message message, User user, string reason, CancellationToken cancellationToken);
} 
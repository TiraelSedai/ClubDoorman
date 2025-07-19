using ClubDoorman.Models;
using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис модерации сообщений
/// </summary>
public interface IModerationService
{
    /// <summary>
    /// Проверяет сообщение на соответствие правилам
    /// </summary>
    Task<ModerationResult> CheckMessageAsync(Message message);

    /// <summary>
    /// Проверяет пользователя на блокировку по имени
    /// </summary>
    Task<ModerationResult> CheckUserNameAsync(User user);

    /// <summary>
    /// Выполняет действие модерации
    /// </summary>
    Task ExecuteModerationActionAsync(Message message, ModerationResult result);

    /// <summary>
    /// Проверяет, одобрен ли пользователь в чате
    /// </summary>
    bool IsUserApproved(long userId, long? chatId = null);

    /// <summary>
    /// Увеличивает счетчик хороших сообщений пользователя
    /// </summary>
    Task IncrementGoodMessageCountAsync(User user, Chat chat, string messageText);

    /// <summary>
    /// Включает или выключает AI-детект для подозрительного пользователя
    /// </summary>
    bool SetAiDetectForSuspiciousUser(long userId, long chatId, bool enabled);

    /// <summary>
    /// Получает статистику по подозрительным пользователям
    /// </summary>
    (int TotalSuspicious, int WithAiDetect, int GroupsCount) GetSuspiciousUsersStats();

    /// <summary>
    /// Получает список подозрительных пользователей с включенным AI-детектом
    /// </summary>
    List<(long UserId, long ChatId)> GetAiDetectUsers();

    /// <summary>
    /// Проверяет, включен ли AI-детект для пользователя, и отправляет уведомление админам
    /// </summary>
    Task<bool> CheckAiDetectAndNotifyAdminsAsync(User user, Chat chat, Message message);

    /// <summary>
    /// Снимает ограничения с пользователя и одобряет его
    /// </summary>
    Task<bool> UnrestrictAndApproveUserAsync(long userId, long chatId);

    /// <summary>
    /// Полностью удаляет пользователя из всех списков (подозрительных, одобренных, кэшей)
    /// </summary>
    void CleanupUserFromAllLists(long userId, long chatId);

    /// <summary>
    /// Банит пользователя и удаляет его из всех списков
    /// </summary>
    Task<bool> BanAndCleanupUserAsync(long userId, long chatId, int? messageIdToDelete = null);
} 
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
    Task IncrementGoodMessageCountAsync(User user, Chat chat);
} 
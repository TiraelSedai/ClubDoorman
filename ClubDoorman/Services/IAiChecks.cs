using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс для AI проверок
/// </summary>
public interface IAiChecks
{
    /// <summary>
    /// Отмечает пользователя как проверенного и безопасного
    /// </summary>
    void MarkUserOkay(long userId);

    /// <summary>
    /// Получает вероятность того, что профиль создан для привлечения внимания/спама
    /// </summary>
    ValueTask<SpamPhotoBio> GetAttentionBaitProbability(User user, Func<string, Task>? ifChanged = default);
    
    /// <summary>
    /// Получает вероятность того, что профиль создан для привлечения внимания/спама (с учетом первого сообщения)
    /// </summary>
    ValueTask<SpamPhotoBio> GetAttentionBaitProbability(User user, string? messageText, Func<string, Task>? ifChanged = default);

    /// <summary>
    /// Получает вероятность того, что сообщение является спамом
    /// </summary>
    ValueTask<SpamProbability> GetSpamProbability(Message message);

    /// <summary>
    /// Получает вероятность того, что подозрительный пользователь отправил спам
    /// </summary>
    ValueTask<SpamProbability> GetSuspiciousUserSpamProbability(
        Message message, 
        User user, 
        List<string> firstMessages, 
        double mimicryScore);
} 
using ClubDoorman.Models;
using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для работы с капчей
/// </summary>
public interface ICaptchaService
{
    /// <summary>
    /// Создает капчу для нового пользователя в чате, либо возвращает null, если капча отключена для чата.
    /// </summary>
    /// <param name="chat">Чат, в котором создается капча</param>
    /// <param name="user">Пользователь, для которого создается капча</param>
    /// <param name="userJoinMessage">Сообщение о присоединении пользователя (опционально)</param>
    /// <returns>Информация о созданной капче или null, если капча отключена для чата</returns>
    Task<CaptchaInfo?> CreateCaptchaAsync(Chat chat, User user, Message? userJoinMessage = null);

    /// <summary>
    /// Проверяет ответ на капчу
    /// </summary>
    Task<bool> ValidateCaptchaAsync(string key, int answer);

    /// <summary>
    /// Получает информацию о капче по ключу
    /// </summary>
    CaptchaInfo? GetCaptchaInfo(string key);

    /// <summary>
    /// Удаляет капчу
    /// </summary>
    bool RemoveCaptcha(string key);

    /// <summary>
    /// Генерирует ключ для пользователя в чате
    /// </summary>
    string GenerateKey(long chatId, long userId);

    /// <summary>
    /// Банит пользователей, которые не прошли капчу вовремя
    /// </summary>
    Task BanExpiredCaptchaUsersAsync();
} 
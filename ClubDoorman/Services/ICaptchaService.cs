using ClubDoorman.Models;
using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для работы с капчей
/// </summary>
public interface ICaptchaService
{
    /// <summary>
    /// Создает капчу для пользователя
    /// </summary>
    Task<CaptchaInfo> CreateCaptchaAsync(Chat chat, User user, Message? userJoinMessage = null);

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
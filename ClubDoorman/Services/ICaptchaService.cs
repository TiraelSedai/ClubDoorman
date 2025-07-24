using ClubDoorman.Models;
using ClubDoorman.Models.Requests;
using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для работы с капчей
/// </summary>
public interface ICaptchaService
{
    /// <summary>
    /// Создает капчу используя Request объект
    /// </summary>
    /// <param name="request">Запрос на создание капчи</param>
    /// <returns>Информация о созданной капче или null, если капча отключена для чата</returns>
    Task<CaptchaInfo?> CreateCaptchaAsync(CreateCaptchaRequest request);

    /// <summary>
    /// Проверяет ответ на капчу
    /// </summary>
    /// <param name="callbackData">Данные callback'а</param>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Результат проверки капчи</returns>
    Task<CaptchaResult> CheckCaptchaAsync(string callbackData, long userId);

    /// <summary>
    /// Генерирует ключ для пользователя в чате
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Уникальный ключ капчи</returns>
    string GenerateKey(long chatId, long userId);

    /// <summary>
    /// Получает информацию о капче по ключу
    /// </summary>
    /// <param name="key">Ключ капчи</param>
    /// <returns>Информация о капче или null, если не найдена</returns>
    CaptchaInfo? GetCaptchaInfo(string key);

    /// <summary>
    /// Удаляет капчу по ключу
    /// </summary>
    /// <param name="key">Ключ капчи</param>
    /// <returns>true, если капча была удалена</returns>
    bool RemoveCaptcha(string key);

    /// <summary>
    /// Проверяет ответ на капчу (старый метод для совместимости)
    /// </summary>
    /// <param name="key">Ключ капчи</param>
    /// <param name="answer">Ответ пользователя</param>
    /// <returns>true, если ответ правильный</returns>
    Task<bool> ValidateCaptchaAsync(string key, int answer);

    /// <summary>
    /// Банит пользователей, которые не прошли капчу вовремя
    /// </summary>
    Task BanExpiredCaptchaUsersAsync();
} 
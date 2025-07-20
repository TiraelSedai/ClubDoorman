using ClubDoorman.Models;

namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс для управления списком подозрительных пользователей
/// </summary>
public interface ISuspiciousUsersStorage
{
    /// <summary>
    /// Проверка, является ли пользователь подозрительным в данном чате
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <returns>true, если пользователь подозрительный</returns>
    bool IsSuspicious(long userId, long chatId);

    /// <summary>
    /// Добавление пользователя в список подозрительных
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="info">Информация о подозрительном пользователе</param>
    /// <returns>true, если пользователь был добавлен впервые</returns>
    bool AddSuspicious(long userId, long chatId, SuspiciousUserInfo info);

    /// <summary>
    /// Удаление пользователя из списка подозрительных
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <returns>true, если пользователь был удален</returns>
    bool RemoveSuspicious(long userId, long chatId);

    /// <summary>
    /// Обновление счетчика сообщений для подозрительного пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="messageCount">Количество сообщений</param>
    /// <returns>true, если счетчик был обновлен</returns>
    bool UpdateMessageCount(long userId, long chatId, int messageCount);

    /// <summary>
    /// Включение/выключение AI детекции для пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <param name="enabled">Включить или выключить</param>
    /// <returns>true, если настройка была изменена</returns>
    bool SetAiDetectEnabled(long userId, long chatId, bool enabled);

    /// <summary>
    /// Получение информации о подозрительном пользователе
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <returns>Информация о пользователе или null</returns>
    SuspiciousUserInfo? GetSuspiciousInfo(long userId, long chatId);

    /// <summary>
    /// Получение списка пользователей с включенной AI детекцией
    /// </summary>
    /// <returns>Список пар (UserId, ChatId)</returns>
    List<(long UserId, long ChatId)> GetAiDetectUsers();

    /// <summary>
    /// Получение информации о подозрительном пользователе
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="chatId">ID чата</param>
    /// <returns>Информация о пользователе или null</returns>
    SuspiciousUserInfo? GetSuspiciousUser(long userId, long chatId);

    /// <summary>
    /// Получение статистики
    /// </summary>
    /// <returns>Кортеж (Всего подозрительных, С AI детекцией, Количество групп)</returns>
    (int TotalSuspicious, int WithAiDetect, int GroupsCount) GetStats();
} 
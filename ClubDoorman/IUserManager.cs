namespace ClubDoorman;

/// <summary>
/// Интерфейс для управления пользователями и их одобрениями
/// </summary>
public interface IUserManager
{
    /// <summary>
    /// Проверяет, одобрен ли пользователь
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы (для новой системы)</param>
    /// <returns>true, если пользователь одобрен</returns>
    bool Approved(long userId, long? groupId = null);

    /// <summary>
    /// Одобряет пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы (для новой системы)</param>
    ValueTask Approve(long userId, long? groupId = null);

    /// <summary>
    /// Удаляет одобрение пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы (для новой системы)</param>
    /// <param name="removeAll">Удалить все одобрения (для новой системы)</param>
    /// <returns>true, если одобрение было удалено</returns>
    bool RemoveApproval(long userId, long? groupId = null, bool removeAll = false);

    /// <summary>
    /// Проверяет, находится ли пользователь в банлисте
    /// </summary>
    ValueTask<bool> InBanlist(long userId);

    /// <summary>
    /// Получает имя пользователя из клуба
    /// </summary>
    ValueTask<string?> GetClubUsername(long userId);

    /// <summary>
    /// Обновляет банлист
    /// </summary>
    Task RefreshBanlist();
} 
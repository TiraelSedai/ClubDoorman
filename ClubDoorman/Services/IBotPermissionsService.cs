using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для проверки прав бота в чатах
/// </summary>
public interface IBotPermissionsService
{
    /// <summary>
    /// Проверяет, имеет ли бот права администратора в чате
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если бот является администратором</returns>
    Task<bool> IsBotAdminAsync(long chatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет, работает ли бот в тихом режиме в данном чате
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>true, если бот работает в тихом режиме</returns>
    Task<bool> IsSilentModeAsync(long chatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает информацию о правах бота в чате
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Информация о члене чата или null, если бот не в чате</returns>
    Task<ChatMember?> GetBotChatMemberAsync(long chatId, CancellationToken cancellationToken = default);
} 
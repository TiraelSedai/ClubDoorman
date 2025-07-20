using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Infrastructure;

internal static class Utils
{
    /// <summary>
    /// Формирует полное имя пользователя из объекта User.
    /// </summary>
    /// <param name="user">Пользователь Telegram</param>
    /// <returns>Полное имя пользователя</returns>
    /// <exception cref="ArgumentNullException">Если user равен null</exception>
    public static string FullName(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        var firstName = user.FirstName ?? "Unknown";
        return string.IsNullOrEmpty(user.LastName) ? firstName : $"{firstName} {user.LastName}";
    }
    
    /// <summary>
    /// Формирует полное имя из имени и фамилии.
    /// </summary>
    /// <param name="firstName">Имя</param>
    /// <param name="lastName">Фамилия (опционально)</param>
    /// <returns>Полное имя</returns>
    /// <exception cref="ArgumentNullException">Если firstName равен null</exception>
    public static string FullName(string firstName, string? lastName)
    {
        if (firstName == null) throw new ArgumentNullException(nameof(firstName));
        return string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";
    }
    
    /// <summary>
    /// Создает ссылку на сообщение в чате.
    /// </summary>
    /// <param name="chat">Чат</param>
    /// <param name="messageId">ID сообщения</param>
    /// <returns>Ссылка на сообщение или пустая строка, если ссылку создать нельзя</returns>
    /// <exception cref="ArgumentNullException">Если chat равен null</exception>
    public static string LinkToMessage(Chat chat, long messageId)
    {
        if (chat == null) throw new ArgumentNullException(nameof(chat));
        return chat.Type == ChatType.Supergroup ? LinkToSuperGroupMessage(chat, messageId)
            : chat.Username == null ? ""
            : LinkToGroupWithNameMessage(chat, messageId);
    }

    /// <summary>
    /// Создает ссылку на сообщение в супергруппе.
    /// </summary>
    /// <param name="chat">Чат</param>
    /// <param name="messageId">ID сообщения</param>
    /// <returns>Ссылка на сообщение в супергруппе</returns>
    private static string LinkToSuperGroupMessage(Chat chat, long messageId) => 
        $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}";

    /// <summary>
    /// Создает ссылку на сообщение в группе с именем пользователя.
    /// </summary>
    /// <param name="chat">Чат</param>
    /// <param name="messageId">ID сообщения</param>
    /// <returns>Ссылка на сообщение в группе</returns>
    private static string LinkToGroupWithNameMessage(Chat chat, long messageId) => 
        $"https://t.me/{chat.Username}/{messageId}";
} 
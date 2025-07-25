using Telegram.Bot.Types;

namespace ClubDoorman.Models.Requests;

/// <summary>
/// Запрос на создание капчи
/// </summary>
public class CreateCaptchaRequest
{
    /// <summary>
    /// Чат, для которого создается капча
    /// </summary>
    public Chat Chat { get; }
    
    /// <summary>
    /// Пользователь, для которого создается капча
    /// </summary>
    public User User { get; }
    
    /// <summary>
    /// Сообщение о присоединении пользователя (опционально)
    /// </summary>
    public Message? UserJoinMessage { get; }
    
    /// <summary>
    /// Создает новый запрос на создание капчи
    /// </summary>
    /// <param name="chat">Чат</param>
    /// <param name="user">Пользователь</param>
    /// <param name="userJoinMessage">Сообщение о присоединении (опционально)</param>
    public CreateCaptchaRequest(
        Chat chat,
        User user,
        Message? userJoinMessage = null)
    {
        Chat = chat ?? throw new ArgumentNullException(nameof(chat));
        User = user ?? throw new ArgumentNullException(nameof(user));
        UserJoinMessage = userJoinMessage;
    }
} 
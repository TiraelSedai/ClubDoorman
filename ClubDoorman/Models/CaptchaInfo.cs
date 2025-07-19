using Telegram.Bot.Types;

namespace ClubDoorman.Models;

/// <summary>
/// Информация о капче для пользователя
/// </summary>
public sealed record CaptchaInfo(
    /// <summary>
    /// ID чата, в котором создана капча
    /// </summary>
    long ChatId,
    
    /// <summary>
    /// Название чата
    /// </summary>
    string? ChatTitle,
    
    /// <summary>
    /// Время создания капчи
    /// </summary>
    DateTime Timestamp,
    
    /// <summary>
    /// Пользователь, для которого создана капча
    /// </summary>
    User User,
    
    /// <summary>
    /// Индекс правильного ответа в списке вариантов
    /// </summary>
    int CorrectAnswer,
    
    /// <summary>
    /// Токен отмены для автоматического удаления капчи
    /// </summary>
    CancellationTokenSource Cts,
    
    /// <summary>
    /// Сообщение о присоединении пользователя (опционально)
    /// </summary>
    Message? UserJoinedMessage
); 
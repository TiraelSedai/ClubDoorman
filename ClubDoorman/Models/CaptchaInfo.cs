using Telegram.Bot.Types;

namespace ClubDoorman.Models;

/// <summary>
/// Информация о капче для пользователя
/// </summary>
public sealed record CaptchaInfo(
    long ChatId,
    string? ChatTitle,
    DateTime Timestamp,
    User User,
    int CorrectAnswer,
    CancellationTokenSource Cts,
    Message? UserJoinedMessage
); 
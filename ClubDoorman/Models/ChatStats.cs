namespace ClubDoorman.Models;

/// <summary>
/// Статистика чата
/// </summary>
public sealed class ChatStats(string? title)
{
    public string? ChatTitle = title;
    public int StoppedCaptcha;
    public int BlacklistBanned;
    public int KnownBadMessage;
    public int LongNameBanned;
} 
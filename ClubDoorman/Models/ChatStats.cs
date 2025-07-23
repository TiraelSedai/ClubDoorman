namespace ClubDoorman.Models;

/// <summary>
/// Статистика чата
/// </summary>
public sealed class ChatStats(string? title)
{
    /// <summary>
    /// Название чата
    /// </summary>
    public string? ChatTitle = title;
    
    /// <summary>
    /// Количество остановленных капч
    /// </summary>
    public int StoppedCaptcha;
    
    /// <summary>
    /// Количество забаненных по черному списку
    /// </summary>
    public int BlacklistBanned;
    
    /// <summary>
    /// Количество известных плохих сообщений
    /// </summary>
    public int KnownBadMessage;
    
    /// <summary>
    /// Количество забаненных за длинное имя
    /// </summary>
    public int LongNameBanned;
} 
namespace ClubDoorman.Models;

/// <summary>
/// Результат проверки капчи
/// </summary>
public class CaptchaResult
{
    /// <summary>
    /// Успешно ли пройдена капча
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Причина неудачи (если Success = false)
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// Создает успешный результат
    /// </summary>
    public static CaptchaResult Ok() => new() { Success = true };
    
    /// <summary>
    /// Создает неуспешный результат с причиной
    /// </summary>
    /// <param name="reason">Причина неудачи</param>
    public static CaptchaResult Fail(string reason) => new() { Success = false, Reason = reason };
} 
namespace ClubDoorman.Models;

/// <summary>
/// Результат модерации сообщения
/// </summary>
public record ModerationResult(
    ModerationAction Action,
    string Reason,
    double? Confidence = null
);

/// <summary>
/// Действие модерации
/// </summary>
public enum ModerationAction
{
    Allow,
    Delete,
    Ban,
    Report,
    RequireManualReview
} 
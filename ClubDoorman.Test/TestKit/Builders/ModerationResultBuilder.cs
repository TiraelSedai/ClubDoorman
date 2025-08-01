using ClubDoorman.Models;

namespace ClubDoorman.Test.TestKit.Builders;

/// <summary>
/// Builder для создания результатов модерации
/// <tags>builders, moderation-result, moderation, fluent-api</tags>
/// </summary>
public class ModerationResultBuilder
{
    private ModerationAction _action = ModerationAction.Allow;
    private string _reason = "Valid message";
    private double? _confidence = null;
    
    /// <summary>
    /// Устанавливает действие модерации
    /// <tags>builders, moderation-result, action, fluent-api</tags>
    /// </summary>
    public ModerationResultBuilder WithAction(ModerationAction action)
    {
        _action = action;
        return this;
    }
    
    /// <summary>
    /// Устанавливает причину модерации
    /// <tags>builders, moderation-result, reason, fluent-api</tags>
    /// </summary>
    public ModerationResultBuilder WithReason(string reason)
    {
        _reason = reason;
        return this;
    }
    
    /// <summary>
    /// Устанавливает уверенность в результате
    /// <tags>builders, moderation-result, confidence, fluent-api</tags>
    /// </summary>
    public ModerationResultBuilder WithConfidence(double confidence)
    {
        _confidence = confidence;
        return this;
    }
    
    /// <summary>
    /// Устанавливает результат как разрешение
    /// <tags>builders, moderation-result, allow, fluent-api</tags>
    /// </summary>
    public ModerationResultBuilder AsAllow()
    {
        _action = ModerationAction.Allow;
        _reason = "Valid message";
        return this;
    }
    
    /// <summary>
    /// Устанавливает результат как удаление
    /// <tags>builders, moderation-result, delete, fluent-api</tags>
    /// </summary>
    public ModerationResultBuilder AsDelete()
    {
        _action = ModerationAction.Delete;
        _reason = "Spam detected";
        return this;
    }
    
    /// <summary>
    /// Устанавливает результат как бан
    /// <tags>builders, moderation-result, ban, fluent-api</tags>
    /// </summary>
    public ModerationResultBuilder AsBan()
    {
        _action = ModerationAction.Ban;
        _reason = "Spam detected";
        return this;
    }
    
    /// <summary>
    /// Строит результат модерации
    /// <tags>builders, moderation-result, build, fluent-api</tags>
    /// </summary>
    public ModerationResult Build() => new ModerationResult(_action, _reason, _confidence);
    
    /// <summary>
    /// Неявное преобразование в ModerationResult
    /// <tags>builders, moderation-result, conversion, fluent-api</tags>
    /// </summary>
    public static implicit operator ModerationResult(ModerationResultBuilder builder) => builder.Build();
} 
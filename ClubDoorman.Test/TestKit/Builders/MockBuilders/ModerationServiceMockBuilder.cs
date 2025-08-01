using ClubDoorman.Services;
using ClubDoorman.Models;
using Moq;

namespace ClubDoorman.Test.TestKit.Builders.MockBuilders;

/// <summary>
/// Билдер для мока IModerationService
/// <tags>builders, moderation-service, mocks, fluent-api</tags>
/// </summary>
public class ModerationServiceMockBuilder
{
    private readonly Mock<IModerationService> _mock = new();
    private ModerationAction _defaultAction = ModerationAction.Allow;
    private string _defaultReason = "Test moderation";
    private double? _defaultConfidence = null;

    /// <summary>
    /// Настраивает мок для возврата указанного действия
    /// <tags>builders, moderation-service, action, fluent-api</tags>
    /// </summary>
    public ModerationServiceMockBuilder ThatReturns(ModerationAction action)
    {
        _defaultAction = action;
        return this;
    }

    /// <summary>
    /// Настраивает мок для возврата указанной причины
    /// <tags>builders, moderation-service, reason, fluent-api</tags>
    /// </summary>
    public ModerationServiceMockBuilder WithReason(string reason)
    {
        _defaultReason = reason;
        return this;
    }

    /// <summary>
    /// Настраивает мок для возврата указанной уверенности
    /// <tags>builders, moderation-service, confidence, fluent-api</tags>
    /// </summary>
    public ModerationServiceMockBuilder WithConfidence(double confidence)
    {
        _defaultConfidence = confidence;
        return this;
    }

    /// <summary>
    /// Настраивает мок для разрешения сообщений
    /// <tags>builders, moderation-service, allow, fluent-api</tags>
    /// </summary>
    public ModerationServiceMockBuilder ThatAllowsMessages()
    {
        _defaultAction = ModerationAction.Allow;
        _defaultReason = "Message allowed";
        return this;
    }

    /// <summary>
    /// Настраивает мок для удаления сообщений
    /// <tags>builders, moderation-service, delete, fluent-api</tags>
    /// </summary>
    public ModerationServiceMockBuilder ThatDeletesMessages(string reason = "Spam detected")
    {
        _defaultAction = ModerationAction.Delete;
        _defaultReason = reason;
        return this;
    }

    /// <summary>
    /// Настраивает мок для бана пользователей
    /// <tags>builders, moderation-service, ban, fluent-api</tags>
    /// </summary>
    public ModerationServiceMockBuilder ThatBansUsers(string reason = "Spam detected")
    {
        _defaultAction = ModerationAction.Ban;
        _defaultReason = reason;
        return this;
    }

    /// <summary>
    /// Строит мок
    /// <tags>builders, moderation-service, build, fluent-api</tags>
    /// </summary>
    public Mock<IModerationService> Build()
    {
        _mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(_defaultAction, _defaultReason, _defaultConfidence));
        
        return _mock;
    }

    /// <summary>
    /// Строит объект мока
    /// <tags>builders, moderation-service, build-object, fluent-api</tags>
    /// </summary>
    public IModerationService BuildObject() => Build().Object;
} 
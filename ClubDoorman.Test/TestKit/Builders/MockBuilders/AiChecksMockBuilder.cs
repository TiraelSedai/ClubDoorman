using ClubDoorman.Services;
using ClubDoorman.Models;
using Telegram.Bot.Types;
using Moq;

namespace ClubDoorman.Test.TestKit.Builders.MockBuilders;

/// <summary>
/// Билдер для мока IAiChecks
/// <tags>builders, ai-checks, mocks, fluent-api</tags>
/// </summary>
public class AiChecksMockBuilder
{
    private readonly Mock<IAiChecks> _mock = new();

    /// <summary>
    /// Настраивает мок для одобрения фото
    /// <tags>builders, ai-checks, approve-photo, fluent-api</tags>
    /// </summary>
    public AiChecksMockBuilder ThatApprovesPhoto()
    {
        _mock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), It.IsAny<Func<string, Task>>()))
            .ReturnsAsync(new SpamPhotoBio(new SpamProbability { Probability = 0.1, Reason = "Photo approved" }, new byte[0], "Mock user bio"));
        
        _mock.Setup(x => x.GetSpamProbability(It.IsAny<Message>()))
            .ReturnsAsync(new SpamProbability { Probability = 0.1, Reason = "Message approved" });
        
        return this;
    }

    /// <summary>
    /// Настраивает мок для отклонения фото
    /// <tags>builders, ai-checks, reject-photo, fluent-api</tags>
    /// </summary>
    public AiChecksMockBuilder ThatRejectsPhoto()
    {
        _mock.Setup(x => x.GetAttentionBaitProbability(It.IsAny<User>(), It.IsAny<Func<string, Task>>()))
            .ReturnsAsync(new SpamPhotoBio(new SpamProbability { Probability = 0.9, Reason = "Photo rejected" }, new byte[0], "Mock user bio"));
        
        _mock.Setup(x => x.GetSpamProbability(It.IsAny<Message>()))
            .ReturnsAsync(new SpamProbability { Probability = 0.9, Reason = "Message rejected" });
        
        return this;
    }

    /// <summary>
    /// Строит мок
    /// <tags>builders, ai-checks, build, fluent-api</tags>
    /// </summary>
    public Mock<IAiChecks> Build() => _mock;

    /// <summary>
    /// Строит объект мока
    /// <tags>builders, ai-checks, build-object, fluent-api</tags>
    /// </summary>
    public IAiChecks BuildObject() => Build().Object;
} 
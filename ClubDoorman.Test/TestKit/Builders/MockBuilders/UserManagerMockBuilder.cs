using ClubDoorman.Services;
using Moq;

namespace ClubDoorman.Test.TestKit.Builders.MockBuilders;

/// <summary>
/// Билдер для мока IUserManager
/// <tags>builders, user-manager, mocks, fluent-api</tags>
/// </summary>
public class UserManagerMockBuilder
{
    private readonly Mock<IUserManager> _mock = new();

    /// <summary>
    /// Настраивает мок для одобрения пользователя
    /// <tags>builders, user-manager, approve, fluent-api</tags>
    /// </summary>
    public UserManagerMockBuilder ThatApprovesUser(long userId)
    {
        _mock.Setup(x => x.Approved(userId, null))
            .Returns(true);
        return this;
    }

    /// <summary>
    /// Настраивает мок для отклонения пользователя
    /// <tags>builders, user-manager, reject, fluent-api</tags>
    /// </summary>
    public UserManagerMockBuilder ThatRejectsUser(long userId)
    {
        _mock.Setup(x => x.Approved(userId, null))
            .Returns(false);
        return this;
    }

    /// <summary>
    /// Настраивает мок для проверки отсутствия пользователя в блэклисте
    /// <tags>builders, user-manager, not-in-banlist, fluent-api</tags>
    /// </summary>
    public UserManagerMockBuilder ThatIsNotInBanlist(long userId)
    {
        _mock.Setup(x => x.InBanlist(userId))
            .ReturnsAsync(false);
        return this;
    }

    /// <summary>
    /// Настраивает мок для проверки наличия пользователя в блэклисте
    /// <tags>builders, user-manager, in-banlist, fluent-api</tags>
    /// </summary>
    public UserManagerMockBuilder ThatIsInBanlist(long userId)
    {
        _mock.Setup(x => x.InBanlist(userId))
            .ReturnsAsync(true);
        return this;
    }

    /// <summary>
    /// Строит мок
    /// <tags>builders, user-manager, build, fluent-api</tags>
    /// </summary>
    public Mock<IUserManager> Build() => _mock;

    /// <summary>
    /// Строит объект мока
    /// <tags>builders, user-manager, build-object, fluent-api</tags>
    /// </summary>
    public IUserManager BuildObject() => Build().Object;
} 
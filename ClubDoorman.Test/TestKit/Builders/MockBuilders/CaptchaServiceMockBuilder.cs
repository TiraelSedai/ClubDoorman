using ClubDoorman.Services;
using ClubDoorman.Models;
using Moq;

namespace ClubDoorman.Test.TestKit.Builders.MockBuilders;

/// <summary>
/// Билдер для мока ICaptchaService
/// <tags>builders, captcha-service, mocks, fluent-api</tags>
/// </summary>
public class CaptchaServiceMockBuilder
{
    private readonly Mock<ICaptchaService> _mock = new();

    /// <summary>
    /// Настраивает мок для успешного прохождения капчи
    /// <tags>builders, captcha-service, success, fluent-api</tags>
    /// </summary>
    public CaptchaServiceMockBuilder ThatSucceeds()
    {
        _mock.Setup(x => x.ValidateCaptchaAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(true);
        
        return this;
    }

    /// <summary>
    /// Настраивает мок для неудачного прохождения капчи
    /// <tags>builders, captcha-service, failure, fluent-api</tags>
    /// </summary>
    public CaptchaServiceMockBuilder ThatFails()
    {
        _mock.Setup(x => x.ValidateCaptchaAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(false);
        
        return this;
    }

    /// <summary>
    /// Строит мок
    /// <tags>builders, captcha-service, build, fluent-api</tags>
    /// </summary>
    public Mock<ICaptchaService> Build() => _mock;

    /// <summary>
    /// Строит объект мока
    /// <tags>builders, captcha-service, build-object, fluent-api</tags>
    /// </summary>
    public ICaptchaService BuildObject() => Build().Object;
} 
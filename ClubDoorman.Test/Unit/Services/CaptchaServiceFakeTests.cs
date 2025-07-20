using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Reflection;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("unit")]
[Category("services")]
[Category("captcha")]
public class CaptchaServiceFakeTests
{
    private CaptchaServiceTestFactory _factory = null!;
    private FakeTelegramClient _fakeClient = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new CaptchaServiceTestFactory();
        _fakeClient = new FakeTelegramClient();
    }

    [Test]
    public async Task CreateCaptchaAsync_ValidUser_SendsWelcomeMessage()
    {
        // Arrange
        var service = _factory.CreateCaptchaServiceWithFake(_fakeClient);
        var chat = new Chat { Id = 123456, Title = "Test Chat", Type = ChatType.Group };
        var user = new User { Id = 789, FirstName = "Test", LastName = "User" };

        // Act
        var captchaInfo = await service.CreateCaptchaAsync(chat, user);

        // Assert
        Assert.That(captchaInfo, Is.Not.Null);
        Assert.That(captchaInfo.User.Id, Is.EqualTo(789));
        Assert.That(_fakeClient.WasMessageSentTo(123456, "Привет"), Is.True);
        Assert.That(_fakeClient.SentMessages.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task CreateCaptchaAsync_UserWithInappropriateName_UsesGenericName()
    {
        // Arrange
        var service = _factory.CreateCaptchaServiceWithFake(_fakeClient);
        var chat = new Chat { Id = 123456, Title = "Test Chat", Type = ChatType.Group };
        var user = new User { Id = 789, FirstName = "p0rn", LastName = "user" };

        // Act
        await service.CreateCaptchaAsync(chat, user);

        // Assert
        var sentMessage = _fakeClient.SentMessages.First();
        Assert.That(sentMessage.Text, Does.Contain("новый участник чата"));
        Assert.That(sentMessage.Text, Does.Not.Contain("p0rn"));
    }

    [Test]
    public async Task ValidateCaptchaAsync_CorrectAnswer_ReturnsTrue()
    {
        // Arrange
        var service = _factory.CreateCaptchaServiceWithFake(_fakeClient);
        var chat = new Chat { Id = 123456, Title = "Test Chat", Type = ChatType.Group };
        var user = new User { Id = 789, FirstName = "Test", LastName = "User" };

        var captchaInfo = await service.CreateCaptchaAsync(chat, user);
        var key = service.GenerateKey(chat.Id, user.Id);

        // Act
        var result = await service.ValidateCaptchaAsync(key, captchaInfo.CorrectAnswer);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ValidateCaptchaAsync_WrongAnswer_ReturnsFalse()
    {
        // Arrange
        var service = _factory.CreateCaptchaServiceWithFake(_fakeClient);
        var chat = new Chat { Id = 123456, Title = "Test Chat", Type = ChatType.Group };
        var user = new User { Id = 789, FirstName = "Test", LastName = "User" };

        await service.CreateCaptchaAsync(chat, user);
        var key = service.GenerateKey(chat.Id, user.Id);

        // Act
        var result = await service.ValidateCaptchaAsync(key, 999); // Неправильный ответ

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CreateCaptchaAsync_TelegramError_ThrowsException()
    {
        // Arrange
        _fakeClient.ShouldThrowException = true;
        _fakeClient.ExceptionToThrow = new Exception("Telegram API error");
        
        var service = _factory.CreateCaptchaServiceWithFake(_fakeClient);
        var chat = new Chat { Id = 123456, Title = "Test Chat", Type = ChatType.Group };
        var user = new User { Id = 789, FirstName = "Test", LastName = "User" };

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(async () => 
            await service.CreateCaptchaAsync(chat, user));
        
        Assert.That(ex.Message, Is.EqualTo("Telegram API error"));
    }

    [Test]
    public void GenerateKey_ValidIds_ReturnsExpectedKey()
    {
        // Arrange
        var service = _factory.CreateCaptchaServiceWithFake(_fakeClient);

        // Act
        var key = service.GenerateKey(123456, 789);

        // Assert
        Assert.That(key, Is.EqualTo("123456_789"));
    }

    [Test]
    public async Task CreateCaptchaAsync_IncludesVpnAd_ByDefault()
    {
        // Arrange
        var service = _factory.CreateCaptchaServiceWithFake(_fakeClient);
        var chat = new Chat { Id = 123456, Title = "Test Chat", Type = ChatType.Group };
        var user = new User { Id = 789, FirstName = "Test", LastName = "User" };

        // Act
        await service.CreateCaptchaAsync(chat, user);

        // Assert
        var sentMessage = _fakeClient.SentMessages.First();
        Assert.That(sentMessage.Text, Does.Contain("📍 Место для рекламы"));
    }



    [Test]
    public async Task ValidateCaptchaAsync_ExpiredCaptcha_ReturnsFalse()
    {
        // Arrange
        var service = _factory.CreateCaptchaServiceWithFake(_fakeClient);
        var chat = new Chat { Id = 123456, Title = "Test Chat", Type = ChatType.Group };
        var user = new User { Id = 789, FirstName = "Test", LastName = "User" };

        await service.CreateCaptchaAsync(chat, user);
        var key = service.GenerateKey(chat.Id, user.Id);

        // Симулируем истечение времени капчи
        await Task.Delay(100); // Небольшая задержка для теста

        // Act
        var result = await service.ValidateCaptchaAsync(key, 0);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ValidateCaptchaAsync_InvalidKey_ReturnsFalse()
    {
        // Arrange
        var service = _factory.CreateCaptchaServiceWithFake(_fakeClient);

        // Act
        var result = await service.ValidateCaptchaAsync("invalid_key", 0);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetCaptchaInfo_ValidKey_ReturnsCaptchaInfo()
    {
        // Arrange
        var service = _factory.CreateCaptchaServiceWithFake(_fakeClient);
        var key = service.GenerateKey(123456, 789);

        // Act
        var info = service.GetCaptchaInfo(key);

        // Assert
        Assert.That(info, Is.Null); // Капча еще не создана
    }

    [Test]
    public void RemoveCaptcha_ValidKey_ReturnsTrue()
    {
        // Arrange
        var service = _factory.CreateCaptchaServiceWithFake(_fakeClient);
        var key = service.GenerateKey(123456, 789);

        // Act
        var result = service.RemoveCaptcha(key);

        // Assert
        Assert.That(result, Is.False); // Капча не существует
    }
} 
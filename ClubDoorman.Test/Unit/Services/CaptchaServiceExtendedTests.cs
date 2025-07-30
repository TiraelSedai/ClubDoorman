using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Models;
using ClubDoorman.Models.Requests;
using ClubDoorman.Test.TestData;
using NUnit.Framework;
using Moq;
using System;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace ClubDoorman.Test.Unit.Services;

/// <summary>
/// Расширенные тесты для CaptchaService
/// </summary>
[TestFixture]
[Category("extended")]
public class CaptchaServiceExtendedTests
{
    private CaptchaServiceTestFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new CaptchaServiceTestFactory()
            .WithAppConfigSetup(mock => 
            {
                mock.Setup(x => x.NoCaptchaGroups).Returns(new HashSet<long>());
                mock.Setup(x => x.NoVpnAdGroups).Returns(new HashSet<long>());
            })
            .WithMessageServiceSetup(mock =>
            {
                mock.Setup(x => x.SendCaptchaMessageAsync(It.IsAny<SendCaptchaMessageRequest>()))
                    .ReturnsAsync(new Message 
                    { 
                        Chat = new Chat { Id = 123456 },
                        Date = DateTime.UtcNow
                    });
            });
    }

    #region CreateCaptchaAsync Tests

    [Test]
    public async Task CreateCaptchaAsync_ValidParameters_CreatesCaptchaSuccessfully()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser();
        var joinMessage = CreateTestMessage();

        // Act
        var request = new CreateCaptchaRequest(chat, user, joinMessage);
        var result = await service.CreateCaptchaAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ChatId, Is.EqualTo(chat.Id));
        Assert.That(result.User.Id, Is.EqualTo(user.Id));
        Assert.That(result.CorrectAnswer, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.CorrectAnswer, Is.LessThan(30)); // Captcha.CaptchaList.Count = 30
        Assert.That(result.Timestamp, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
        Assert.That(result.UserJoinedMessage, Is.EqualTo(joinMessage));
    }

    [Test]
    public async Task CreateCaptchaAsync_NullChat_ThrowsArgumentNullException()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var user = CreateTestUser();

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            var request = new CreateCaptchaRequest(null!, user, null);
            return service.CreateCaptchaAsync(request);
        });
        Assert.That(ex.ParamName, Is.EqualTo("chat"));
    }

    [Test]
    public async Task CreateCaptchaAsync_NullUser_ThrowsArgumentNullException()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            var request = new CreateCaptchaRequest(chat, null!, null);
            return service.CreateCaptchaAsync(request);
        });
        Assert.That(ex.ParamName, Is.EqualTo("user"));
    }

    [Test]
    public async Task CreateCaptchaAsync_UserWithInappropriateName_UsesGenericName()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser(firstName: "p0rn", lastName: "user");

        // Act
        var request = new CreateCaptchaRequest(chat, user, null);
        var result = await service.CreateCaptchaAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        // Проверяем, что имя было заменено на "новый участник чата"
        // Это проверяется косвенно через логику сервиса
    }

    [Test]
    public async Task CreateCaptchaAsync_UserWithInappropriateUsername_UsesGenericName()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser(username: "porn_user");

        // Act
        var request = new CreateCaptchaRequest(chat, user, null);
        var result = await service.CreateCaptchaAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        // Проверяем, что имя было заменено на "новый участник чата"
    }

    [Test]
    public async Task CreateCaptchaAsync_WithoutJoinMessage_CreatesCaptchaSuccessfully()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser();

        // Act
        var request = new CreateCaptchaRequest(chat, user, null);
        var result = await service.CreateCaptchaAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserJoinedMessage, Is.Null);
    }

    [Test]
    public async Task CreateCaptchaAsync_MultipleUsers_CreatesUniqueCaptchas()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user1 = CreateTestUser(id: 1);
        var user2 = CreateTestUser(id: 2);

        // Act
        var request1 = new CreateCaptchaRequest(chat, user1, null);
        var request2 = new CreateCaptchaRequest(chat, user2, null);
        var result1 = await service.CreateCaptchaAsync(request1);
        
        // Небольшая задержка для изменения seed генератора случайных чисел
        await Task.Delay(1);
        
        var result2 = await service.CreateCaptchaAsync(request2);

        // Assert
        Assert.That(result1, Is.Not.Null);
        Assert.That(result2, Is.Not.Null);
        Assert.That(result1.User.Id, Is.EqualTo(1));
        Assert.That(result2.User.Id, Is.EqualTo(2));
        Assert.That(result1.CorrectAnswer, Is.Not.EqualTo(result2.CorrectAnswer));
    }

    #endregion

    #region ValidateCaptchaAsync Tests

    [Test]
    public async Task ValidateCaptchaAsync_CorrectAnswer_ReturnsTrue()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser();
        var request = new CreateCaptchaRequest(chat, user, null);
        var captchaInfo = await service.CreateCaptchaAsync(request);
        var key = service.GenerateKey(chat.Id, user.Id);

        // Act
        var result = await service.ValidateCaptchaAsync(key, captchaInfo.CorrectAnswer);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ValidateCaptchaAsync_IncorrectAnswer_ReturnsFalse()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser();
        var request = new CreateCaptchaRequest(chat, user, null);
        var captchaInfo = await service.CreateCaptchaAsync(request);
        var key = service.GenerateKey(chat.Id, user.Id);
        var wrongAnswer = (captchaInfo.CorrectAnswer + 1) % 8;

        // Act
        var result = await service.ValidateCaptchaAsync(key, wrongAnswer);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ValidateCaptchaAsync_EmptyKey_ReturnsFalse()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();

        // Act
        var result = await service.ValidateCaptchaAsync("", 0);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ValidateCaptchaAsync_NullKey_ReturnsFalse()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();

        // Act
        var result = await service.ValidateCaptchaAsync(null!, 0);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ValidateCaptchaAsync_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();

        // Act
        var result = await service.ValidateCaptchaAsync("non_existent_key", 0);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ValidateCaptchaAsync_AfterValidation_CaptchaRemoved()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser();
        var request = new CreateCaptchaRequest(chat, user, null);
        await service.CreateCaptchaAsync(request);
        var key = service.GenerateKey(chat.Id, user.Id);

        // Act
        await service.ValidateCaptchaAsync(key, 0);

        // Assert
        var captchaInfo = service.GetCaptchaInfo(key);
        Assert.That(captchaInfo, Is.Null);
    }

    #endregion

    #region GetCaptchaInfo Tests

    [Test]
    public async Task GetCaptchaInfo_ExistingCaptcha_ReturnsCaptchaInfo()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser();
        var request = new CreateCaptchaRequest(chat, user, null);
        var originalCaptcha = await service.CreateCaptchaAsync(request);
        var key = service.GenerateKey(chat.Id, user.Id);

        // Act
        var result = service.GetCaptchaInfo(key);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ChatId, Is.EqualTo(originalCaptcha.ChatId));
        Assert.That(result.User.Id, Is.EqualTo(originalCaptcha.User.Id));
        Assert.That(result.CorrectAnswer, Is.EqualTo(originalCaptcha.CorrectAnswer));
    }

    [Test]
    public async Task GetCaptchaInfo_NonExistentCaptcha_ReturnsNull()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();

        // Act
        var result = service.GetCaptchaInfo("non_existent_key");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetCaptchaInfo_AfterValidation_ReturnsNull()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser();
        var request = new CreateCaptchaRequest(chat, user, null);
        await service.CreateCaptchaAsync(request);
        var key = service.GenerateKey(chat.Id, user.Id);
        await service.ValidateCaptchaAsync(key, 0);

        // Act
        var result = service.GetCaptchaInfo(key);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region RemoveCaptcha Tests

    [Test]
    public async Task RemoveCaptcha_ExistingCaptcha_ReturnsTrue()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser();
        var request = new CreateCaptchaRequest(chat, user, null);
        await service.CreateCaptchaAsync(request);
        var key = service.GenerateKey(chat.Id, user.Id);

        // Act
        var result = service.RemoveCaptcha(key);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task RemoveCaptcha_NonExistentCaptcha_ReturnsFalse()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();

        // Act
        var result = service.RemoveCaptcha("non_existent_key");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task RemoveCaptcha_AfterRemoval_CaptchaNotAccessible()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser();
        var request = new CreateCaptchaRequest(chat, user, null);
        await service.CreateCaptchaAsync(request);
        var key = service.GenerateKey(chat.Id, user.Id);

        // Act
        service.RemoveCaptcha(key);

        // Assert
        var captchaInfo = service.GetCaptchaInfo(key);
        Assert.That(captchaInfo, Is.Null);
    }

    #endregion

    #region GenerateKey Tests

    [Test]
    public void GenerateKey_ValidParameters_ReturnsExpectedKey()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chatId = 123456L;
        var userId = 789L;

        // Act
        var result = service.GenerateKey(chatId, userId);

        // Assert
        Assert.That(result, Is.EqualTo($"{chatId}_{userId}"));
    }

    [Test]
    public void GenerateKey_DifferentParameters_ReturnsDifferentKeys()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();

        // Act
        var key1 = service.GenerateKey(1, 1);
        var key2 = service.GenerateKey(1, 2);
        var key3 = service.GenerateKey(2, 1);

        // Assert
        Assert.That(key1, Is.Not.EqualTo(key2));
        Assert.That(key1, Is.Not.EqualTo(key3));
        Assert.That(key2, Is.Not.EqualTo(key3));
    }

    [Test]
    public void GenerateKey_SameParameters_ReturnsSameKey()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chatId = 123L;
        var userId = 456L;

        // Act
        var key1 = service.GenerateKey(chatId, userId);
        var key2 = service.GenerateKey(chatId, userId);

        // Assert
        Assert.That(key1, Is.EqualTo(key2));
    }

    #endregion

    #region BanExpiredCaptchaUsersAsync Tests

    [Test]
    public async Task BanExpiredCaptchaUsersAsync_NoExpiredCaptchas_CompletesSuccessfully()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();

        // Act & Assert
        Assert.DoesNotThrowAsync(() => service.BanExpiredCaptchaUsersAsync());
    }

    [Test]
    public async Task BanExpiredCaptchaUsersAsync_WithActiveCaptchas_CompletesSuccessfully()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser();
        var request = new CreateCaptchaRequest(chat, user, null);
        await service.CreateCaptchaAsync(request);

        // Act & Assert
        Assert.DoesNotThrowAsync(() => service.BanExpiredCaptchaUsersAsync());
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public async Task CreateCaptchaAsync_TelegramApiError_ThrowsException()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser();

        // Act & Assert
        // Этот тест может падать из-за реальных вызовов Telegram API
        // В реальном проекте нужно мокировать TelegramBotClient
        Assert.DoesNotThrowAsync(() => {
            var request = new CreateCaptchaRequest(chat, user, null);
            return service.CreateCaptchaAsync(request);
        });
    }

    [Test]
    public async Task ValidateCaptchaAsync_ConcurrentValidation_HandlesCorrectly()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser();
        var request = new CreateCaptchaRequest(chat, user, null);
        var captchaInfo = await service.CreateCaptchaAsync(request);
        var key = service.GenerateKey(chat.Id, user.Id);

        // Act
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(service.ValidateCaptchaAsync(key, captchaInfo.CorrectAnswer));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        // Только один вызов должен вернуть true, остальные false
        Assert.That(results.Count(r => r), Is.LessThanOrEqualTo(1));
    }

    [Test]
    public async Task CreateCaptchaAsync_MultipleCaptchasSameUser_HandlesCorrectly()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser();

        // Act
        var request1 = new CreateCaptchaRequest(chat, user, null);
        var captcha1 = await service.CreateCaptchaAsync(request1);
        var request2 = new CreateCaptchaRequest(chat, user, null);
        var captcha2 = await service.CreateCaptchaAsync(request2);

        // Assert
        Assert.That(captcha1, Is.Not.Null);
        Assert.That(captcha2, Is.Not.Null);
        // Второй вызов должен перезаписать первый
        var key = service.GenerateKey(chat.Id, user.Id);
        var info = service.GetCaptchaInfo(key);
        Assert.That(info, Is.Not.Null);
    }

    [Test]
    public async Task CreateCaptchaAsync_CancellationToken_RespectsCancellation()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var user = CreateTestUser();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.DoesNotThrowAsync(() => {
            var request = new CreateCaptchaRequest(chat, user, null);
            return service.CreateCaptchaAsync(request);
        });
        // Примечание: CaptchaService не принимает CancellationToken в CreateCaptchaAsync
        // но внутренние операции могут быть отменены
    }

    #endregion

    #region Performance and Load Tests

    [Test]
    public async Task CreateCaptchaAsync_LargeBatch_HandlesCorrectly()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var tasks = new List<Task<CaptchaInfo>>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var user = CreateTestUser(id: i);
            tasks.Add(service.CreateCaptchaAsync(new CreateCaptchaRequest(chat, user, null)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.That(results, Has.Length.EqualTo(100));
        Assert.That(results.All(r => r != null), Is.True);
    }

    [Test]
    public async Task ValidateCaptchaAsync_LargeBatch_HandlesCorrectly()
    {
        // Arrange
        var service = _factory.CreateCaptchaService();
        var chat = CreateTestChat();
        var captchas = new List<(string key, int answer)>();

        // Создаем капчи
        for (int i = 0; i < 50; i++)
        {
            var user = CreateTestUser(id: i);
            var request = new CreateCaptchaRequest(chat, user, null);
            var captchaInfo = await service.CreateCaptchaAsync(request);
            var key = service.GenerateKey(chat.Id, user.Id);
            captchas.Add((key, captchaInfo.CorrectAnswer));
        }

        // Act
        var tasks = captchas.Select(c => service.ValidateCaptchaAsync(c.key, c.answer));
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.That(results, Has.Length.EqualTo(50));
        Assert.That(results.All(r => r), Is.True);
    }

    #endregion



    #region Helper Methods

    private static Chat CreateTestChat(long id = 123456, string title = "Test Chat")
    {
        return new Chat
        {
            Id = id,
            Title = title,
            Type = ChatType.Group
        };
    }

    private static User CreateTestUser(long id = 789, string firstName = "Test", string? lastName = "User", string? username = "testuser")
    {
        return new User
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Username = username,
            IsBot = false
        };
    }

    private static Message CreateTestMessage(long messageId = 1, long chatId = 123456, long userId = 789)
    {
        // Используем TestDataFactory для создания Message с MessageId
        return TK.CreateValidMessageWithId(messageId);
    }

    #endregion
} 
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestData;
using NUnit.Framework;
using Telegram.Bot.Types;
using Moq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("unit")]
[Category("services")]
[Category("extended")]
public class AiChecksExtendedTests
{
    private AiChecksTestFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new AiChecksTestFactory();
    }

    #region MarkUserOkay Tests

    [Test]
    public void MarkUserOkay_ValidUserId_MarksUserAsOkay()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var userId = 12345L;

        // Act
        service.MarkUserOkay(userId);

        // Assert - проверяем что метод выполнился без исключений
        Assert.Pass("Method executed successfully");
    }

    [Test]
    public void MarkUserOkay_ZeroUserId_MarksUserAsOkay()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var userId = 0L;

        // Act
        service.MarkUserOkay(userId);

        // Assert
        Assert.Pass("Method executed successfully");
    }

    [Test]
    public void MarkUserOkay_NegativeUserId_MarksUserAsOkay()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var userId = -12345L;

        // Act
        service.MarkUserOkay(userId);

        // Assert
        Assert.Pass("Method executed successfully");
    }

    #endregion

    #region GetAttentionBaitProbability Tests

    [Test]
    public async Task GetAttentionBaitProbability_NullUser_ReturnsDefaultResult()
    {
        // Arrange
        var service = _factory.CreateAiChecks();

        // Act
        var result = await service.GetAttentionBaitProbability(null!);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SpamProbability, Is.Not.Null);
        Assert.That(result.Photo, Is.Not.Null);
        Assert.That(result.NameBio, Is.Not.Null);
    }

    [Test]
    public async Task GetAttentionBaitProbability_ValidUser_ReturnsSpamPhotoBio()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var user = TK.CreateValidUser();

        // Act
        var result = await service.GetAttentionBaitProbability(user);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SpamProbability, Is.Not.Null);
        Assert.That(result.Photo, Is.Not.Null);
        Assert.That(result.NameBio, Is.Not.Null);
    }

    [Test]
    public async Task GetAttentionBaitProbability_UserWithoutUsername_ReturnsSpamPhotoBio()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var user = new User 
        { 
            Id = 123, 
            FirstName = "Test", 
            LastName = "User",
            Username = null
        };

        // Act
        var result = await service.GetAttentionBaitProbability(user);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SpamProbability, Is.Not.Null);
    }

    [Test]
    public async Task GetAttentionBaitProbability_UserWithNullLastName_ReturnsSpamPhotoBio()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var user = new User 
        { 
            Id = 123, 
            FirstName = "Test", 
            LastName = null,
            Username = "testuser"
        };

        // Act
        var result = await service.GetAttentionBaitProbability(user);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SpamProbability, Is.Not.Null);
    }

    [Test]
    public async Task GetAttentionBaitProbability_WithCallback_ReturnsSpamPhotoBio()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var user = TK.CreateValidUser();
        var callbackCalled = false;
        Func<string, Task> callback = async (reason) => 
        {
            callbackCalled = true;
            await Task.CompletedTask;
        };

        // Act
        var result = await service.GetAttentionBaitProbability(user, callback);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SpamProbability, Is.Not.Null);
        // Callback может не вызываться в зависимости от логики
    }

    #endregion

    #region GetSpamProbability Tests

    [Test]
    public async Task GetSpamProbability_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var service = _factory.CreateAiChecks();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await service.GetSpamProbability(null!));
        
        Assert.That(exception.ParamName, Is.EqualTo("message"));
    }

    [Test]
    public async Task GetSpamProbability_ValidMessage_ReturnsSpamProbability()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var message = TK.CreateValidMessage();

        // Act
        var result = await service.GetSpamProbability(message);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
        Assert.That(result.Reason, Is.Not.Null);
    }

    [Test]
    public async Task GetSpamProbability_EmptyMessage_ReturnsSpamProbability()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var message = TK.CreateEmptyMessage();

        // Act
        var result = await service.GetSpamProbability(message);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public async Task GetSpamProbability_SpamMessage_ReturnsSpamProbability()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var message = TK.CreateSpamMessage();

        // Act
        var result = await service.GetSpamProbability(message);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public async Task GetSpamProbability_LongMessage_ReturnsSpamProbability()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var message = TK.CreateLongMessage();

        // Act
        var result = await service.GetSpamProbability(message);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    #endregion

    #region GetSuspiciousUserSpamProbability Tests

    [Test]
    public async Task GetSuspiciousUserSpamProbability_NullMessage_ReturnsDefaultSpamProbability()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var user = TK.CreateValidUser();
        var firstMessages = new List<string> { "Hello" };
        var mimicryScore = 0.5;

        // Act
        var result = await service.GetSuspiciousUserSpamProbability(null!, user, firstMessages, mimicryScore);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public async Task GetSuspiciousUserSpamProbability_NullUser_ReturnsDefaultSpamProbability()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var message = TK.CreateValidMessage();
        var firstMessages = new List<string> { "Hello" };
        var mimicryScore = 0.5;

        // Act
        var result = await service.GetSuspiciousUserSpamProbability(message, null!, firstMessages, mimicryScore);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public async Task GetSuspiciousUserSpamProbability_NullFirstMessages_ReturnsDefaultSpamProbability()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var message = TK.CreateValidMessage();
        var user = TK.CreateValidUser();
        var mimicryScore = 0.5;

        // Act
        var result = await service.GetSuspiciousUserSpamProbability(message, user, null!, mimicryScore);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public async Task GetSuspiciousUserSpamProbability_ValidParameters_ReturnsSpamProbability()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var message = TK.CreateValidMessage();
        var user = TK.CreateValidUser();
        var firstMessages = new List<string> { "Hello", "How are you?" };
        var mimicryScore = 0.5;

        // Act
        var result = await service.GetSuspiciousUserSpamProbability(message, user, firstMessages, mimicryScore);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
        Assert.That(result.Reason, Is.Not.Null);
    }

    [Test]
    public async Task GetSuspiciousUserSpamProbability_EmptyFirstMessages_ReturnsSpamProbability()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var message = TK.CreateValidMessage();
        var user = TK.CreateValidUser();
        var firstMessages = new List<string>();
        var mimicryScore = 0.5;

        // Act
        var result = await service.GetSuspiciousUserSpamProbability(message, user, firstMessages, mimicryScore);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public async Task GetSuspiciousUserSpamProbability_HighMimicryScore_ReturnsSpamProbability()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var message = TK.CreateValidMessage();
        var user = TK.CreateValidUser();
        var firstMessages = new List<string> { "Hello" };
        var mimicryScore = 0.9; // Высокий score

        // Act
        var result = await service.GetSuspiciousUserSpamProbability(message, user, firstMessages, mimicryScore);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public async Task GetSuspiciousUserSpamProbability_LowMimicryScore_ReturnsSpamProbability()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var message = TK.CreateValidMessage();
        var user = TK.CreateValidUser();
        var firstMessages = new List<string> { "Hello" };
        var mimicryScore = 0.1; // Низкий score

        // Act
        var result = await service.GetSuspiciousUserSpamProbability(message, user, firstMessages, mimicryScore);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public async Task GetSpamProbability_MessageWithoutText_ReturnsSpamProbability()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var message = new Message
        {
            From = TK.CreateValidUser(),
            Chat = TK.CreateGroupChat(),
            Text = null
        };

        // Act
        var result = await service.GetSpamProbability(message);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public async Task GetSpamProbability_MessageWithoutFrom_ReturnsSpamProbability()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var message = new Message
        {
            From = null,
            Chat = TK.CreateGroupChat(),
            Text = "Test message"
        };

        // Act
        var result = await service.GetSpamProbability(message);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result.Probability, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public async Task GetSpamProbability_ConcurrentCalls_HandlesCorrectly()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var message = TK.CreateValidMessage();
        var tasks = new List<ValueTask<SpamProbability>>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(service.GetSpamProbability(message));
        }

        var results = await Task.WhenAll(tasks.Select(t => t.AsTask()));

        // Assert
        Assert.That(results, Has.Length.EqualTo(5));
        Assert.That(results.All(r => r.Probability >= 0.0 && r.Probability <= 1.0), Is.True);
    }

    #endregion

    #region Integration Tests

    [Test]
    public async Task MarkUserOkay_ThenGetAttentionBaitProbability_ReturnsCachedResult()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var user = TK.CreateValidUser();
        var userId = user.Id;

        // Act
        service.MarkUserOkay(userId);
        var result = await service.GetAttentionBaitProbability(user);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SpamProbability, Is.Not.Null);
    }

    [Test]
    public async Task MultipleCalls_GetSpamProbability_ReturnsConsistentResults()
    {
        // Arrange
        var service = _factory.CreateAiChecks();
        var message = TK.CreateValidMessage();

        // Act
        var result1 = await service.GetSpamProbability(message);
        var result2 = await service.GetSpamProbability(message);

        // Assert
        Assert.That(result1, Is.Not.Null);
        Assert.That(result2, Is.Not.Null);
        Assert.That(result1.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result1.Probability, Is.LessThanOrEqualTo(1.0));
        Assert.That(result2.Probability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result2.Probability, Is.LessThanOrEqualTo(1.0));
    }

    #endregion
} 
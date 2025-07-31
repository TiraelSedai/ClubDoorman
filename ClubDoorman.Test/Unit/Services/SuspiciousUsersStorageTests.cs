using NUnit.Framework;
using Moq;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubDoorman.Models;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("unit")]
[Category("services")]
[Category("suspicious-users")]
public class SuspiciousUsersStorageTests
{
    private SuspiciousUsersStorageTestFactory _factory = null!;
    private ISuspiciousUsersStorage _storage = null!;
    private Mock<ILogger<SuspiciousUsersStorage>> _loggerMock = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new SuspiciousUsersStorageTestFactory();
        _storage = _factory.CreateSuspiciousUsersStorage();
        _loggerMock = _factory.LoggerMock;
        
        // Очищаем данные перед каждым тестом  
        var allUsers = _storage.GetAiDetectUsers();
        foreach (var (userId, chatId) in allUsers)
        {
            _storage.RemoveSuspicious(userId, chatId);
        }
    }

    #region AddSuspiciousUser Tests

    [Test]
    public void AddSuspiciousUser_ValidUserId_AddsUserSuccessfully()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;
        var info = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Test message" },
            0.5,
            true,
            0
        );

        // Act
        var result = _storage.AddSuspicious(userId, chatId, info);

        // Assert
        Assert.That(result, Is.True);
        var isSuspicious = _storage.IsSuspicious(userId, chatId);
        Assert.That(isSuspicious, Is.True);
    }

    [Test]
    public void AddSuspiciousUser_ZeroUserId_AddsUserSuccessfully()
    {
        // Arrange
        var userId = 0L;
        var chatId = 67890L;
        var info = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Test message" },
            0.5,
            true,
            0
        );

        // Act
        var result = _storage.AddSuspicious(userId, chatId, info);

        // Assert
        Assert.That(result, Is.True);
        var isSuspicious = _storage.IsSuspicious(userId, chatId);
        Assert.That(isSuspicious, Is.True);
    }

    [Test]
    public void AddSuspiciousUser_NegativeUserId_AddsUserSuccessfully()
    {
        // Arrange
        var userId = -12345L;
        var chatId = 67890L;
        var info = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Test message" },
            0.5,
            true,
            0
        );

        // Act
        var result = _storage.AddSuspicious(userId, chatId, info);

        // Assert
        Assert.That(result, Is.True);
        var isSuspicious = _storage.IsSuspicious(userId, chatId);
        Assert.That(isSuspicious, Is.True);
    }

    [Test]
    public void AddSuspiciousUser_EmptyReason_AddsUserSuccessfully()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;
        var info = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "" },
            0.5,
            true,
            0
        );

        // Act
        var result = _storage.AddSuspicious(userId, chatId, info);

        // Assert
        Assert.That(result, Is.True);
        var isSuspicious = _storage.IsSuspicious(userId, chatId);
        Assert.That(isSuspicious, Is.True);
    }

    [Test]
    public void AddSuspiciousUser_NullReason_AddsUserSuccessfully()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;
        var info = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { null! },
            0.5,
            true,
            0
        );

        // Act
        var result = _storage.AddSuspicious(userId, chatId, info);

        // Assert
        Assert.That(result, Is.True);
        var isSuspicious = _storage.IsSuspicious(userId, chatId);
        Assert.That(isSuspicious, Is.True);
    }

    [Test]
    public void AddSuspiciousUser_DuplicateUserId_HandlesGracefully()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;
        var info1 = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "First reason" },
            0.5,
            true,
            0
        );
        var info2 = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Second reason" },
            0.5,
            true,
            0
        );

        // Act
        var result1 = _storage.AddSuspicious(userId, chatId, info1);
        var result2 = _storage.AddSuspicious(userId, chatId, info2);

        // Assert
        Assert.That(result1, Is.True);
        Assert.That(result2, Is.False); // Should return false for duplicate
        var isSuspicious = _storage.IsSuspicious(userId, chatId);
        Assert.That(isSuspicious, Is.True);
    }

    #endregion

    #region RemoveSuspiciousUser Tests

    [Test]
    public void RemoveSuspiciousUser_ExistingUser_RemovesSuccessfully()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;
        var info = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Test message" },
            0.5,
            true,
            0
        );
        _storage.AddSuspicious(userId, chatId, info);

        // Act
        var result = _storage.RemoveSuspicious(userId, chatId);

        // Assert
        Assert.That(result, Is.True);
        var isSuspicious = _storage.IsSuspicious(userId, chatId);
        Assert.That(isSuspicious, Is.False);
    }

    [Test]
    public void RemoveSuspiciousUser_NonExistentUser_HandlesGracefully()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;

        // Act
        var result = _storage.RemoveSuspicious(userId, chatId);

        // Assert
        Assert.That(result, Is.False);
        var isSuspicious = _storage.IsSuspicious(userId, chatId);
        Assert.That(isSuspicious, Is.False);
    }

    [Test]
    public void RemoveSuspiciousUser_ZeroUserId_HandlesGracefully()
    {
        // Arrange
        var userId = 0L;
        var chatId = 67890L;

        // Act
        var result = _storage.RemoveSuspicious(userId, chatId);

        // Assert
        Assert.That(result, Is.False);
        var isSuspicious = _storage.IsSuspicious(userId, chatId);
        Assert.That(isSuspicious, Is.False);
    }

    [Test]
    public void RemoveSuspiciousUser_NegativeUserId_HandlesGracefully()
    {
        // Arrange
        var userId = -12345L;
        var chatId = 67890L;

        // Act
        var result = _storage.RemoveSuspicious(userId, chatId);

        // Assert
        Assert.That(result, Is.False);
        var isSuspicious = _storage.IsSuspicious(userId, chatId);
        Assert.That(isSuspicious, Is.False);
    }

    #endregion

    #region IsSuspiciousUser Tests

    [Test]
    public void IsSuspiciousUser_ExistingUser_ReturnsTrue()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;
        var info = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Test message" },
            0.5,
            true,
            0
        );
        _storage.AddSuspicious(userId, chatId, info);

        // Act
        var result = _storage.IsSuspicious(userId, chatId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsSuspiciousUser_NonExistentUser_ReturnsFalse()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;

        // Act
        var result = _storage.IsSuspicious(userId, chatId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsSuspiciousUser_ZeroUserId_ReturnsFalse()
    {
        // Arrange
        var userId = 0L;
        var chatId = 67890L;

        // Act
        var result = _storage.IsSuspicious(userId, chatId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsSuspiciousUser_NegativeUserId_ReturnsFalse()
    {
        // Arrange
        var userId = -12345L;
        var chatId = 67890L;

        // Act
        var result = _storage.IsSuspicious(userId, chatId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsSuspiciousUser_RemovedUser_ReturnsFalse()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;
        var info = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Test message" },
            0.5,
            true,
            0
        );
        _storage.AddSuspicious(userId, chatId, info);
        _storage.RemoveSuspicious(userId, chatId);

        // Act
        var result = _storage.IsSuspicious(userId, chatId);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region GetAiDetectUsers Tests

    [Test]
    public void GetAiDetectUsers_EmptyStorage_ReturnsEmptyList()
    {
        // Act
        var users = _storage.GetAiDetectUsers();

        // Assert
        Assert.That(users, Is.Empty);
    }

    [Test]
    public void GetAiDetectUsers_WithUsers_ReturnsAllUsers()
    {
        // Arrange
        var userId1 = 12345L;
        var userId2 = 67890L;
        var chatId = 11111L;
        var info1 = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Test reason 1" },
            0.5,
            true,
            0
        );
        var info2 = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Test reason 2" },
            0.5,
            true,
            0
        );
        _storage.AddSuspicious(userId1, chatId, info1);
        _storage.AddSuspicious(userId2, chatId, info2);

        // Act
        var users = _storage.GetAiDetectUsers();

        // Assert
        Assert.That(users, Has.Count.EqualTo(2));
        Assert.That(users, Does.Contain((userId1, chatId)));
        Assert.That(users, Does.Contain((userId2, chatId)));
    }

    [Test]
    public void GetAiDetectUsers_AfterRemoval_ReturnsRemainingUsers()
    {
        // Arrange
        var userId1 = 12345L;
        var userId2 = 67890L;
        var chatId = 11111L;
        var info1 = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Test reason 1" },
            0.5,
            true,
            0
        );
        var info2 = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Test reason 2" },
            0.5,
            true,
            0
        );
        _storage.AddSuspicious(userId1, chatId, info1);
        _storage.AddSuspicious(userId2, chatId, info2);
        _storage.RemoveSuspicious(userId1, chatId);

        // Act
        var users = _storage.GetAiDetectUsers();

        // Assert
        Assert.That(users, Has.Count.EqualTo(1));
        Assert.That(users, Does.Contain((userId2, chatId)));
    }

    #endregion

    #region Full Lifecycle Tests

    [Test]
    public void FullLifecycle_AddCheckRemove_WorksCorrectly()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;
        var info = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Test message" },
            0.5,
            true,
            0
        );

        // Act & Assert - Add
        var addResult = _storage.AddSuspicious(userId, chatId, info);
        Assert.That(addResult, Is.True);

        // Act & Assert - Check
        var isSuspicious = _storage.IsSuspicious(userId, chatId);
        Assert.That(isSuspicious, Is.True);

        // Act & Assert - Remove
        var removeResult = _storage.RemoveSuspicious(userId, chatId);
        Assert.That(removeResult, Is.True);

        // Act & Assert - Check after removal
        var isSuspiciousAfter = _storage.IsSuspicious(userId, chatId);
        Assert.That(isSuspiciousAfter, Is.False);
    }

    [Test]
    public void MultipleUsers_ConcurrentOperations_HandleCorrectly()
    {
        // Arrange
        var userId1 = 12345L;
        var userId2 = 67890L;
        var chatId = 11111L;
        var info1 = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Test reason 1" },
            0.5,
            true,
            0
        );
        var info2 = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Test reason 2" },
            0.5,
            true,
            0
        );

        // Act & Assert
        var addResult1 = _storage.AddSuspicious(userId1, chatId, info1);
        var addResult2 = _storage.AddSuspicious(userId2, chatId, info2);
        Assert.That(addResult1, Is.True);
        Assert.That(addResult2, Is.True);

        var isSuspicious1 = _storage.IsSuspicious(userId1, chatId);
        var isSuspicious2 = _storage.IsSuspicious(userId2, chatId);
        Assert.That(isSuspicious1, Is.True);
        Assert.That(isSuspicious2, Is.True);

        var removeResult1 = _storage.RemoveSuspicious(userId1, chatId);
        Assert.That(removeResult1, Is.True);

        var isSuspiciousAfter1 = _storage.IsSuspicious(userId1, chatId);
        var isSuspiciousAfter2 = _storage.IsSuspicious(userId2, chatId);
        Assert.That(isSuspiciousAfter1, Is.False);
        Assert.That(isSuspiciousAfter2, Is.True);
    }

    [Test]
    public void EdgeCases_HandleCorrectly()
    {
        // Arrange
        var userId = 12345L;
        var chatId = 67890L;
        var info = new SuspiciousUserInfo(
            DateTime.UtcNow,
            new List<string> { "Test message" },
            0.5,
            true,
            0
        );

        // Act & Assert - Add same user multiple times
        var addResult1 = _storage.AddSuspicious(userId, chatId, info);
        var addResult2 = _storage.AddSuspicious(userId, chatId, info);
        Assert.That(addResult1, Is.True);
        Assert.That(addResult2, Is.False);

        // Act & Assert - Remove non-existent user
        var removeResult = _storage.RemoveSuspicious(99999L, chatId);
        Assert.That(removeResult, Is.False);

        // Act & Assert - Check non-existent user
        var isSuspicious = _storage.IsSuspicious(99999L, chatId);
        Assert.That(isSuspicious, Is.False);
    }

    #endregion


} 
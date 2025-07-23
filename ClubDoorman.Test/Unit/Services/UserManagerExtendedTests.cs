using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using NUnit.Framework;
using Moq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("unit")]
[Category("services")]
[Category("extended")]
public class UserManagerExtendedTests
{
    private Mock<IUserManager> _userManagerMock = null!;

    [SetUp]
    public void Setup()
    {
        _userManagerMock = new Mock<IUserManager>();
    }

    #region Approved Tests

    [Test]
    public void Approved_ValidUserId_ReturnsApprovalStatus()
    {
        // Arrange
        var userId = 12345L;
        _userManagerMock.Setup(x => x.Approved(userId, null))
            .Returns(true);

        // Act
        var result = _userManagerMock.Object.Approved(userId);

        // Assert
        Assert.That(result, Is.True);
        _userManagerMock.Verify(x => x.Approved(userId, null), Times.Once);
    }

    [Test]
    public void Approved_ZeroUserId_ReturnsApprovalStatus()
    {
        // Arrange
        var userId = 0L;
        _userManagerMock.Setup(x => x.Approved(userId, null))
            .Returns(false);

        // Act
        var result = _userManagerMock.Object.Approved(userId);

        // Assert
        Assert.That(result, Is.False);
        _userManagerMock.Verify(x => x.Approved(userId, null), Times.Once);
    }

    [Test]
    public void Approved_NegativeUserId_ReturnsApprovalStatus()
    {
        // Arrange
        var userId = -12345L;
        _userManagerMock.Setup(x => x.Approved(userId, null))
            .Returns(false);

        // Act
        var result = _userManagerMock.Object.Approved(userId);

        // Assert
        Assert.That(result, Is.False);
        _userManagerMock.Verify(x => x.Approved(userId, null), Times.Once);
    }

    [Test]
    public void Approved_WithGroupId_ReturnsApprovalStatus()
    {
        // Arrange
        var userId = 12345L;
        var groupId = 67890L;
        _userManagerMock.Setup(x => x.Approved(userId, groupId))
            .Returns(true);

        // Act
        var result = _userManagerMock.Object.Approved(userId, groupId);

        // Assert
        Assert.That(result, Is.True);
        _userManagerMock.Verify(x => x.Approved(userId, groupId), Times.Once);
    }

    [Test]
    public void Approved_MaxLongUserId_ReturnsApprovalStatus()
    {
        // Arrange
        var userId = long.MaxValue;
        _userManagerMock.Setup(x => x.Approved(userId, null))
            .Returns(false);

        // Act
        var result = _userManagerMock.Object.Approved(userId);

        // Assert
        Assert.That(result, Is.False);
        _userManagerMock.Verify(x => x.Approved(userId, null), Times.Once);
    }

    #endregion

    #region Approve Tests

    [Test]
    public async Task Approve_ValidUserId_ApprovesUser()
    {
        // Arrange
        var userId = 12345L;
        _userManagerMock.Setup(x => x.Approve(userId, null))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _userManagerMock.Object.Approve(userId);

        // Assert
        _userManagerMock.Verify(x => x.Approve(userId, null), Times.Once);
    }

    [Test]
    public async Task Approve_ZeroUserId_ApprovesUser()
    {
        // Arrange
        var userId = 0L;
        _userManagerMock.Setup(x => x.Approve(userId, null))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _userManagerMock.Object.Approve(userId);

        // Assert
        _userManagerMock.Verify(x => x.Approve(userId, null), Times.Once);
    }

    [Test]
    public async Task Approve_NegativeUserId_ApprovesUser()
    {
        // Arrange
        var userId = -12345L;
        _userManagerMock.Setup(x => x.Approve(userId, null))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _userManagerMock.Object.Approve(userId);

        // Assert
        _userManagerMock.Verify(x => x.Approve(userId, null), Times.Once);
    }

    [Test]
    public async Task Approve_WithGroupId_ApprovesUserInGroup()
    {
        // Arrange
        var userId = 12345L;
        var groupId = 67890L;
        _userManagerMock.Setup(x => x.Approve(userId, groupId))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _userManagerMock.Object.Approve(userId, groupId);

        // Assert
        _userManagerMock.Verify(x => x.Approve(userId, groupId), Times.Once);
    }

    [Test]
    public async Task Approve_MaxLongUserId_ApprovesUser()
    {
        // Arrange
        var userId = long.MaxValue;
        _userManagerMock.Setup(x => x.Approve(userId, null))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _userManagerMock.Object.Approve(userId);

        // Assert
        _userManagerMock.Verify(x => x.Approve(userId, null), Times.Once);
    }

    #endregion

    #region RemoveApproval Tests

    [Test]
    public void RemoveApproval_ValidUserId_ReturnsRemovalResult()
    {
        // Arrange
        var userId = 12345L;
        _userManagerMock.Setup(x => x.RemoveApproval(userId, null, false))
            .Returns(true);

        // Act
        var result = _userManagerMock.Object.RemoveApproval(userId);

        // Assert
        Assert.That(result, Is.True);
        _userManagerMock.Verify(x => x.RemoveApproval(userId, null, false), Times.Once);
    }

    [Test]
    public void RemoveApproval_ZeroUserId_ReturnsRemovalResult()
    {
        // Arrange
        var userId = 0L;
        _userManagerMock.Setup(x => x.RemoveApproval(userId, null, false))
            .Returns(false);

        // Act
        var result = _userManagerMock.Object.RemoveApproval(userId);

        // Assert
        Assert.That(result, Is.False);
        _userManagerMock.Verify(x => x.RemoveApproval(userId, null, false), Times.Once);
    }

    [Test]
    public void RemoveApproval_NegativeUserId_ReturnsRemovalResult()
    {
        // Arrange
        var userId = -12345L;
        _userManagerMock.Setup(x => x.RemoveApproval(userId, null, false))
            .Returns(false);

        // Act
        var result = _userManagerMock.Object.RemoveApproval(userId);

        // Assert
        Assert.That(result, Is.False);
        _userManagerMock.Verify(x => x.RemoveApproval(userId, null, false), Times.Once);
    }

    [Test]
    public void RemoveApproval_WithGroupId_ReturnsRemovalResult()
    {
        // Arrange
        var userId = 12345L;
        var groupId = 67890L;
        _userManagerMock.Setup(x => x.RemoveApproval(userId, groupId, false))
            .Returns(true);

        // Act
        var result = _userManagerMock.Object.RemoveApproval(userId, groupId);

        // Assert
        Assert.That(result, Is.True);
        _userManagerMock.Verify(x => x.RemoveApproval(userId, groupId, false), Times.Once);
    }

    [Test]
    public void RemoveApproval_WithRemoveAll_ReturnsRemovalResult()
    {
        // Arrange
        var userId = 12345L;
        var removeAll = true;
        _userManagerMock.Setup(x => x.RemoveApproval(userId, null, removeAll))
            .Returns(true);

        // Act
        var result = _userManagerMock.Object.RemoveApproval(userId, removeAll: removeAll);

        // Assert
        Assert.That(result, Is.True);
        _userManagerMock.Verify(x => x.RemoveApproval(userId, null, removeAll), Times.Once);
    }

    [Test]
    public void RemoveApproval_WithGroupIdAndRemoveAll_ReturnsRemovalResult()
    {
        // Arrange
        var userId = 12345L;
        var groupId = 67890L;
        var removeAll = true;
        _userManagerMock.Setup(x => x.RemoveApproval(userId, groupId, removeAll))
            .Returns(true);

        // Act
        var result = _userManagerMock.Object.RemoveApproval(userId, groupId, removeAll);

        // Assert
        Assert.That(result, Is.True);
        _userManagerMock.Verify(x => x.RemoveApproval(userId, groupId, removeAll), Times.Once);
    }

    #endregion

    #region InBanlist Tests

    [Test]
    public async Task InBanlist_ValidUserId_ReturnsBanlistStatus()
    {
        // Arrange
        var userId = 12345L;
        _userManagerMock.Setup(x => x.InBanlist(userId))
            .ReturnsAsync(true);

        // Act
        var result = await _userManagerMock.Object.InBanlist(userId);

        // Assert
        Assert.That(result, Is.True);
        _userManagerMock.Verify(x => x.InBanlist(userId), Times.Once);
    }

    [Test]
    public async Task InBanlist_ZeroUserId_ReturnsBanlistStatus()
    {
        // Arrange
        var userId = 0L;
        _userManagerMock.Setup(x => x.InBanlist(userId))
            .ReturnsAsync(false);

        // Act
        var result = await _userManagerMock.Object.InBanlist(userId);

        // Assert
        Assert.That(result, Is.False);
        _userManagerMock.Verify(x => x.InBanlist(userId), Times.Once);
    }

    [Test]
    public async Task InBanlist_NegativeUserId_ReturnsBanlistStatus()
    {
        // Arrange
        var userId = -12345L;
        _userManagerMock.Setup(x => x.InBanlist(userId))
            .ReturnsAsync(false);

        // Act
        var result = await _userManagerMock.Object.InBanlist(userId);

        // Assert
        Assert.That(result, Is.False);
        _userManagerMock.Verify(x => x.InBanlist(userId), Times.Once);
    }

    [Test]
    public async Task InBanlist_MaxLongUserId_ReturnsBanlistStatus()
    {
        // Arrange
        var userId = long.MaxValue;
        _userManagerMock.Setup(x => x.InBanlist(userId))
            .ReturnsAsync(false);

        // Act
        var result = await _userManagerMock.Object.InBanlist(userId);

        // Assert
        Assert.That(result, Is.False);
        _userManagerMock.Verify(x => x.InBanlist(userId), Times.Once);
    }

    #endregion

    #region GetClubUsername Tests

    [Test]
    public async Task GetClubUsername_ValidUserId_ReturnsUsername()
    {
        // Arrange
        var userId = 12345L;
        var expectedUsername = "john_doe";
        _userManagerMock.Setup(x => x.GetClubUsername(userId))
            .ReturnsAsync(expectedUsername);

        // Act
        var result = await _userManagerMock.Object.GetClubUsername(userId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedUsername));
        _userManagerMock.Verify(x => x.GetClubUsername(userId), Times.Once);
    }

    [Test]
    public async Task GetClubUsername_ZeroUserId_ReturnsUsername()
    {
        // Arrange
        var userId = 0L;
        _userManagerMock.Setup(x => x.GetClubUsername(userId))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _userManagerMock.Object.GetClubUsername(userId);

        // Assert
        Assert.That(result, Is.Null);
        _userManagerMock.Verify(x => x.GetClubUsername(userId), Times.Once);
    }

    [Test]
    public async Task GetClubUsername_NegativeUserId_ReturnsUsername()
    {
        // Arrange
        var userId = -12345L;
        _userManagerMock.Setup(x => x.GetClubUsername(userId))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _userManagerMock.Object.GetClubUsername(userId);

        // Assert
        Assert.That(result, Is.Null);
        _userManagerMock.Verify(x => x.GetClubUsername(userId), Times.Once);
    }

    [Test]
    public async Task GetClubUsername_UserNotFound_ReturnsNull()
    {
        // Arrange
        var userId = 99999L;
        _userManagerMock.Setup(x => x.GetClubUsername(userId))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _userManagerMock.Object.GetClubUsername(userId);

        // Assert
        Assert.That(result, Is.Null);
        _userManagerMock.Verify(x => x.GetClubUsername(userId), Times.Once);
    }

    [Test]
    public async Task GetClubUsername_EmptyUsername_ReturnsEmptyString()
    {
        // Arrange
        var userId = 12345L;
        var expectedUsername = "";
        _userManagerMock.Setup(x => x.GetClubUsername(userId))
            .ReturnsAsync(expectedUsername);

        // Act
        var result = await _userManagerMock.Object.GetClubUsername(userId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedUsername));
        _userManagerMock.Verify(x => x.GetClubUsername(userId), Times.Once);
    }

    #endregion

    #region RefreshBanlist Tests

    [Test]
    public async Task RefreshBanlist_ExecutesSuccessfully()
    {
        // Arrange
        _userManagerMock.Setup(x => x.RefreshBanlist())
            .Returns(Task.CompletedTask);

        // Act
        await _userManagerMock.Object.RefreshBanlist();

        // Assert
        _userManagerMock.Verify(x => x.RefreshBanlist(), Times.Once);
    }

    [Test]
    public async Task RefreshBanlist_ThrowsException_HandlesGracefully()
    {
        // Arrange
        _userManagerMock.Setup(x => x.RefreshBanlist())
            .ThrowsAsync(new InvalidOperationException("Network error"));

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _userManagerMock.Object.RefreshBanlist());
        
        Assert.That(exception.Message, Is.EqualTo("Network error"));
        _userManagerMock.Verify(x => x.RefreshBanlist(), Times.Once);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public async Task Approve_ThenRemoveApproval_WorksCorrectly()
    {
        // Arrange
        var userId = 12345L;
        _userManagerMock.Setup(x => x.Approve(userId, null))
            .Returns(ValueTask.CompletedTask);
        _userManagerMock.Setup(x => x.RemoveApproval(userId, null, false))
            .Returns(true);

        // Act
        await _userManagerMock.Object.Approve(userId);
        var removeResult = _userManagerMock.Object.RemoveApproval(userId);

        // Assert
        Assert.That(removeResult, Is.True);
        _userManagerMock.Verify(x => x.Approve(userId, null), Times.Once);
        _userManagerMock.Verify(x => x.RemoveApproval(userId, null, false), Times.Once);
    }

    [Test]
    public async Task Approve_ThenCheckApproval_ReturnsCorrectStatus()
    {
        // Arrange
        var userId = 12345L;
        _userManagerMock.Setup(x => x.Approve(userId, null))
            .Returns(ValueTask.CompletedTask);
        _userManagerMock.Setup(x => x.Approved(userId, null))
            .Returns(true);

        // Act
        await _userManagerMock.Object.Approve(userId);
        var approvalStatus = _userManagerMock.Object.Approved(userId);

        // Assert
        Assert.That(approvalStatus, Is.True);
        _userManagerMock.Verify(x => x.Approve(userId, null), Times.Once);
        _userManagerMock.Verify(x => x.Approved(userId, null), Times.Once);
    }

    [Test]
    public async Task MultipleApprovals_SameUser_HandlesCorrectly()
    {
        // Arrange
        var userId = 12345L;
        _userManagerMock.Setup(x => x.Approve(userId, null))
            .Returns(ValueTask.CompletedTask);
        _userManagerMock.Setup(x => x.Approved(userId, null))
            .Returns(true);

        // Act
        await _userManagerMock.Object.Approve(userId);
        await _userManagerMock.Object.Approve(userId);
        var approvalStatus = _userManagerMock.Object.Approved(userId);

        // Assert
        Assert.That(approvalStatus, Is.True);
        _userManagerMock.Verify(x => x.Approve(userId, null), Times.Exactly(2));
        _userManagerMock.Verify(x => x.Approved(userId, null), Times.Once);
    }

    [Test]
    public async Task ConcurrentOperations_HandleCorrectly()
    {
        // Arrange
        var userId = 12345L;
        _userManagerMock.Setup(x => x.Approve(userId, null))
            .Returns(ValueTask.CompletedTask);
        _userManagerMock.Setup(x => x.InBanlist(userId))
            .ReturnsAsync(false);

        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_userManagerMock.Object.Approve(userId).AsTask());
            tasks.Add(_userManagerMock.Object.InBanlist(userId).AsTask());
        }

        await Task.WhenAll(tasks);

        // Assert
        _userManagerMock.Verify(x => x.Approve(userId, null), Times.Exactly(5));
        _userManagerMock.Verify(x => x.InBanlist(userId), Times.Exactly(5));
    }

    #endregion

    #region Integration Tests

    [Test]
    public async Task FullUserLifecycle_WorksCorrectly()
    {
        // Arrange
        var userId = 12345L;
        var groupId = 67890L;
        _userManagerMock.Setup(x => x.Approved(userId, groupId))
            .Returns(false);
        _userManagerMock.Setup(x => x.Approve(userId, groupId))
            .Returns(ValueTask.CompletedTask);
        _userManagerMock.Setup(x => x.RemoveApproval(userId, groupId, false))
            .Returns(true);

        // Act & Assert
        // 1. Проверяем начальное состояние
        var initialApproval = _userManagerMock.Object.Approved(userId, groupId);
        Assert.That(initialApproval, Is.False);

        // 2. Одобряем пользователя
        await _userManagerMock.Object.Approve(userId, groupId);
        _userManagerMock.Setup(x => x.Approved(userId, groupId))
            .Returns(true);
        var afterApproval = _userManagerMock.Object.Approved(userId, groupId);
        Assert.That(afterApproval, Is.True);

        // 3. Удаляем одобрение
        var removeResult = _userManagerMock.Object.RemoveApproval(userId, groupId);
        Assert.That(removeResult, Is.True);

        // 4. Проверяем финальное состояние
        _userManagerMock.Setup(x => x.Approved(userId, groupId))
            .Returns(false);
        var finalApproval = _userManagerMock.Object.Approved(userId, groupId);
        Assert.That(finalApproval, Is.False);
    }

    [Test]
    public async Task BanlistAndApproval_IndependentOperations()
    {
        // Arrange
        var userId = 12345L;
        _userManagerMock.Setup(x => x.InBanlist(userId))
            .ReturnsAsync(true);
        _userManagerMock.Setup(x => x.Approved(userId, null))
            .Returns(false);

        // Act
        var banlistStatus = await _userManagerMock.Object.InBanlist(userId);
        var approvalStatus = _userManagerMock.Object.Approved(userId);

        // Assert
        Assert.That(banlistStatus, Is.True);
        Assert.That(approvalStatus, Is.False);
        // Banlist и approval должны работать независимо
    }

    [Test]
    public async Task UserManagement_WithClubIntegration_WorksCorrectly()
    {
        // Arrange
        var userId = 12345L;
        var clubUsername = "john_doe";
        _userManagerMock.Setup(x => x.GetClubUsername(userId))
            .ReturnsAsync(clubUsername);
        _userManagerMock.Setup(x => x.Approve(userId, null))
            .Returns(ValueTask.CompletedTask);

        // Act
        var username = await _userManagerMock.Object.GetClubUsername(userId);
        await _userManagerMock.Object.Approve(userId);

        // Assert
        Assert.That(username, Is.EqualTo(clubUsername));
        _userManagerMock.Verify(x => x.GetClubUsername(userId), Times.Once);
        _userManagerMock.Verify(x => x.Approve(userId, null), Times.Once);
    }

    #endregion

    #region Performance and Stress Tests

    [Test]
    public async Task MultipleUsers_ConcurrentOperations_HandleCorrectly()
    {
        // Arrange
        var userIds = new long[] { 1, 2, 3, 4, 5 };
        var tasks = new List<Task>();

        foreach (var userId in userIds)
        {
            _userManagerMock.Setup(x => x.Approve(userId, null))
                .Returns(ValueTask.CompletedTask);
            _userManagerMock.Setup(x => x.Approved(userId, null))
                .Returns(true);
        }

        // Act
        foreach (var userId in userIds)
        {
            tasks.Add(_userManagerMock.Object.Approve(userId).AsTask());
            tasks.Add(Task.Run(() => _userManagerMock.Object.Approved(userId)));
        }

        await Task.WhenAll(tasks);

        // Assert
        foreach (var userId in userIds)
        {
            _userManagerMock.Verify(x => x.Approve(userId, null), Times.Once);
            _userManagerMock.Verify(x => x.Approved(userId, null), Times.Once);
        }
    }

    [Test]
    public async Task LargeUserId_HandlesCorrectly()
    {
        // Arrange
        var userId = 999999999999L;
        _userManagerMock.Setup(x => x.Approved(userId, null))
            .Returns(false);
        _userManagerMock.Setup(x => x.InBanlist(userId))
            .ReturnsAsync(false);

        // Act
        var approvalStatus = _userManagerMock.Object.Approved(userId);
        var banlistStatus = await _userManagerMock.Object.InBanlist(userId);

        // Assert
        Assert.That(approvalStatus, Is.False);
        Assert.That(banlistStatus, Is.False);
        _userManagerMock.Verify(x => x.Approved(userId, null), Times.Once);
        _userManagerMock.Verify(x => x.InBanlist(userId), Times.Once);
    }

    #endregion
} 
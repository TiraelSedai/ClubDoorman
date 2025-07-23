using NUnit.Framework;
using ClubDoorman;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace ClubDoorman.Test.Unit;

[TestFixture]
[Category("fast")]
[Category("critical")]
[Category("uses:worker")]
public class WorkerTests
{

    [SetUp]
    public void Setup()
    {
        // Для тестирования статических методов Worker не нужны моки
    }

    [Test]
    public void FullName_WithFirstNameAndLastName_ReturnsCombinedName()
    {
        // Arrange
        var firstName = "John";
        var lastName = "Doe";
        var method = typeof(Worker).GetMethod("FullName", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { firstName, lastName });

        // Assert
        Assert.That(result, Is.EqualTo("John Doe"));
    }

    [Test]
    public void FullName_WithFirstNameOnly_ReturnsFirstName()
    {
        // Arrange
        var firstName = "John";
        string? lastName = null;
        var method = typeof(Worker).GetMethod("FullName", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { firstName, lastName });

        // Assert
        Assert.That(result, Is.EqualTo("John"));
    }

    [Test]
    public void FullName_WithEmptyLastName_ReturnsFirstName()
    {
        // Arrange
        var firstName = "John";
        var lastName = "";
        var method = typeof(Worker).GetMethod("FullName", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { firstName, lastName });

        // Assert
        Assert.That(result, Is.EqualTo("John"));
    }

    [Test]
    public void UserToKey_ReturnsCorrectFormat()
    {
        // Arrange
        var chatId = 123456789L;
        var user = new User { Id = 987654321 };
        var method = typeof(Worker).GetMethod("UserToKey", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { chatId, user });

        // Assert
        Assert.That(result, Is.EqualTo("123456789_987654321"));
    }

    [Test]
    public void AdminDisplayName_WithUsername_ReturnsUsername()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "John", Username = "john_doe" };
        var method = typeof(Worker).GetMethod("AdminDisplayName", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { user });

        // Assert
        Assert.That(result, Is.EqualTo("john_doe"));
    }

    [Test]
    public void AdminDisplayName_WithoutUsername_ReturnsFullName()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "John", LastName = "Doe" };
        var method = typeof(Worker).GetMethod("AdminDisplayName", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { user });

        // Assert
        Assert.That(result, Is.EqualTo("John Doe"));
    }

    [Test]
    public void AdminDisplayName_WithFirstNameOnly_ReturnsFirstName()
    {
        // Arrange
        var user = new User { Id = 123, FirstName = "John" };
        var method = typeof(Worker).GetMethod("AdminDisplayName", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { user });

        // Assert
        Assert.That(result, Is.EqualTo("John"));
    }

    [Test]
    public void Worker_StaticMethods_AreAccessible()
    {
        // Arrange
        var fullNameMethod = typeof(Worker).GetMethod("FullName", BindingFlags.NonPublic | BindingFlags.Static);
        var userToKeyMethod = typeof(Worker).GetMethod("UserToKey", BindingFlags.NonPublic | BindingFlags.Static);
        var adminDisplayNameMethod = typeof(Worker).GetMethod("AdminDisplayName", BindingFlags.NonPublic | BindingFlags.Static);

        // Assert
        Assert.That(fullNameMethod, Is.Not.Null, "FullName method should be accessible");
        Assert.That(userToKeyMethod, Is.Not.Null, "UserToKey method should be accessible");
        Assert.That(adminDisplayNameMethod, Is.Not.Null, "AdminDisplayName method should be accessible");
    }
} 
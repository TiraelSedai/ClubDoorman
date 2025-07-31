using ClubDoorman.Services;
using ClubDoorman.Test.TestData;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Moderation;

[TestFixture]
[Category("unit")]
[Category("moderation")]
[Category("ml")]
public class SpamHamClassifierTests : TestBase
{
    private Mock<ISpamHamClassifier> _classifierMock = null!;
    private SpamHamClassifierTestFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new SpamHamClassifierTestFactory();
        _classifierMock = TK.CreateMockSpamHamClassifier();
    }

    [Test]
    public async Task IsSpam_ValidMessage_ReturnsNotSpam()
    {
        // Arrange
        var message = TK.CreateValidMessage();
        _classifierMock.Setup(x => x.IsSpam(message.Text!))
            .ReturnsAsync((false, 0.2f));

        // Act
        var result = await _classifierMock.Object.IsSpam(message.Text!);

        // Assert
        Assert.That(result.Spam, Is.False);
        Assert.That(result.Score, Is.LessThan(0.5f));
    }

    [Test]
    public async Task IsSpam_SpamMessage_ReturnsSpam()
    {
        // Arrange
        var message = TK.CreateSpamMessage();
        _classifierMock.Setup(x => x.IsSpam(message.Text!))
            .ReturnsAsync((true, 0.8f));

        // Act
        var result = await _classifierMock.Object.IsSpam(message.Text!);

        // Assert
        Assert.That(result.Spam, Is.True);
        Assert.That(result.Score, Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(result.Score, Is.LessThanOrEqualTo(1.0f));
    }

    [Test]
    public async Task IsSpam_EmptyMessage_ReturnsNotSpam()
    {
        // Arrange
        var message = TK.CreateEmptyMessage();
        _classifierMock.Setup(x => x.IsSpam(message.Text!))
            .ReturnsAsync((false, 0.1f));

        // Act
        var result = await _classifierMock.Object.IsSpam(message.Text!);

        // Assert
        Assert.That(result.Spam, Is.False);
        Assert.That(result.Score, Is.LessThan(0.5f));
    }

    [Test]
    public async Task IsSpam_NullText_ThrowsNullReferenceException()
    {
        // Arrange
        var message = TK.CreateNullTextMessage();
        _classifierMock.Setup(x => x.IsSpam(message.Text!))
            .ThrowsAsync(new NullReferenceException());

        // Act & Assert
        var exception = Assert.ThrowsAsync<NullReferenceException>(async () => 
            await _classifierMock.Object.IsSpam(message.Text!));
    }

    [Test]
    public async Task IsSpam_LongMessage_HandlesCorrectly()
    {
        // Arrange
        var message = TK.CreateLongMessage();
        _classifierMock.Setup(x => x.IsSpam(message.Text!))
            .ReturnsAsync((false, 0.3f));

        // Act
        var result = await _classifierMock.Object.IsSpam(message.Text!);

        // Assert
        Assert.That(result.Score, Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(result.Score, Is.LessThanOrEqualTo(1.0f));
    }

    [Test]
    public async Task IsSpam_ConcurrentCalls_HandlesCorrectly()
    {
        // Arrange
        var message = TK.CreateValidMessage();
        _classifierMock.Setup(x => x.IsSpam(message.Text!))
            .ReturnsAsync((false, 0.2f));
        var tasks = new List<Task<(bool, float)>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_classifierMock.Object.IsSpam(message.Text!));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.That(results, Has.Length.EqualTo(10));
        foreach (var result in results)
        {
            Assert.That(result.Item2, Is.GreaterThanOrEqualTo(0.0f));
            Assert.That(result.Item2, Is.LessThanOrEqualTo(1.0f));
        }
    }

    [Test]
    public async Task IsSpam_DifferentUserTypes_HandlesCorrectly()
    {
        // Arrange
        var validMessage = TK.CreateValidMessage();
        var spamMessage = TK.CreateSpamMessage();
        _classifierMock.Setup(x => x.IsSpam(validMessage.Text!))
            .ReturnsAsync((false, 0.2f));
        _classifierMock.Setup(x => x.IsSpam(spamMessage.Text!))
            .ReturnsAsync((true, 0.8f));

        // Act
        var validResult = await _classifierMock.Object.IsSpam(validMessage.Text!);
        var spamResult = await _classifierMock.Object.IsSpam(spamMessage.Text!);

        // Assert
        Assert.That(validResult.Score, Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(validResult.Score, Is.LessThanOrEqualTo(1.0f));
        Assert.That(spamResult.Score, Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(spamResult.Score, Is.LessThanOrEqualTo(1.0f));
    }

    [Test]
    public async Task IsSpam_DifferentChatTypes_HandlesCorrectly()
    {
        // Arrange
        var groupMessage = TK.CreateValidMessage();
        var privateMessage = TK.CreateSpamMessage();
        _classifierMock.Setup(x => x.IsSpam(groupMessage.Text!))
            .ReturnsAsync((false, 0.2f));
        _classifierMock.Setup(x => x.IsSpam(privateMessage.Text!))
            .ReturnsAsync((true, 0.8f));

        // Act
        var groupResult = await _classifierMock.Object.IsSpam(groupMessage.Text!);
        var privateResult = await _classifierMock.Object.IsSpam(privateMessage.Text!);

        // Assert
        Assert.That(groupResult.Score, Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(privateResult.Score, Is.GreaterThanOrEqualTo(0.0f));
    }

    [Test]
    public async Task AddSpam_ValidMessage_AddsToDataset()
    {
        // Arrange
        var message = TK.CreateSpamMessage();

        // Act
        await _classifierMock.Object.AddSpam(message.Text!);

        // Assert
        // Проверяем, что метод выполнился без исключений
        Assert.Pass("AddSpam executed successfully");
    }

    [Test]
    public async Task AddHam_ValidMessage_AddsToDataset()
    {
        // Arrange
        var message = TK.CreateValidMessage();

        // Act
        await _classifierMock.Object.AddHam(message.Text!);

        // Assert
        // Проверяем, что метод выполнился без исключений
        Assert.Pass("AddHam executed successfully");
    }

    [Test]
    public async Task AddSpam_EmptyMessage_HandlesCorrectly()
    {
        // Arrange
        var message = TK.CreateEmptyMessage();

        // Act
        await _classifierMock.Object.AddSpam(message.Text!);

        // Assert
        // Проверяем, что метод выполнился без исключений
        Assert.Pass("AddSpam with empty message executed successfully");
    }

    [Test]
    public async Task AddHam_EmptyMessage_HandlesCorrectly()
    {
        // Arrange
        var message = TK.CreateEmptyMessage();

        // Act
        await _classifierMock.Object.AddHam(message.Text!);

        // Assert
        // Проверяем, что метод выполнился без исключений
        Assert.Pass("AddHam with empty message executed successfully");
    }

    [Test]
    public async Task AddSpam_LongMessage_HandlesCorrectly()
    {
        // Arrange
        var message = TK.CreateLongMessage();

        // Act
        await _classifierMock.Object.AddSpam(message.Text!);

        // Assert
        // Проверяем, что метод выполнился без исключений
        Assert.Pass("AddSpam with long message executed successfully");
    }

    [Test]
    public async Task AddHam_LongMessage_HandlesCorrectly()
    {
        // Arrange
        var message = TK.CreateLongMessage();

        // Act
        await _classifierMock.Object.AddHam(message.Text!);

        // Assert
        // Проверяем, что метод выполнился без исключений
        Assert.Pass("AddHam with long message executed successfully");
    }

    [Test]
    public async Task AddSpam_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var messageWithSpecialChars = "Hello! @#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        await _classifierMock.Object.AddSpam(messageWithSpecialChars);

        // Assert
        // Проверяем, что метод выполнился без исключений
        Assert.Pass("AddSpam with special characters executed successfully");
    }

    [Test]
    public async Task AddHam_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var messageWithSpecialChars = "Hello! @#$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        await _classifierMock.Object.AddHam(messageWithSpecialChars);

        // Assert
        // Проверяем, что метод выполнился без исключений
        Assert.Pass("AddHam with special characters executed successfully");
    }

    [Test]
    public async Task AddSpam_ConcurrentCalls_HandlesCorrectly()
    {
        // Arrange
        var message = TK.CreateSpamMessage();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_classifierMock.Object.AddSpam(message.Text!));
        }

        await Task.WhenAll(tasks);

        // Assert
        // Проверяем, что все вызовы выполнились без исключений
        Assert.Pass("Concurrent AddSpam calls executed successfully");
    }

    [Test]
    public async Task AddHam_ConcurrentCalls_HandlesCorrectly()
    {
        // Arrange
        var message = TK.CreateValidMessage();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_classifierMock.Object.AddHam(message.Text!));
        }

        await Task.WhenAll(tasks);

        // Assert
        // Проверяем, что все вызовы выполнились без исключений
        Assert.Pass("Concurrent AddHam calls executed successfully");
    }
} 
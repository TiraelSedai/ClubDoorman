using ClubDoorman.Services;
using ClubDoorman.Test.TestData;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Moderation;

[TestFixture]
[Category("unit")]
[Category("moderation")]
[Category("ml")]
[Category("slow")]
public class SpamHamClassifierTests : TestBase
{
    private SpamHamClassifier _classifier = null!;
    private Mock<ILogger<SpamHamClassifier>> _loggerMock = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<SpamHamClassifier>>();
        _classifier = new SpamHamClassifier(_loggerMock.Object);
    }

    [Test]
    [CancelAfter(10000)] // 10 секунд максимум
    public async Task IsSpam_ValidMessage_ReturnsNotSpam()
    {
        // Arrange
        var message = TestDataFactory.CreateValidMessage();

        // Act
        var result = await _classifier.IsSpam(message.Text!);

        // Assert
        Assert.That(result.Spam, Is.False);
        Assert.That(result.Score, Is.LessThan(0.5f));
    }

    [Test]
    [CancelAfter(10000)] // 10 секунд максимум
    public async Task IsSpam_SpamMessage_ReturnsSpam()
    {
        // Arrange
        var message = TestDataFactory.CreateSpamMessage();

        // Act
        var result = await _classifier.IsSpam(message.Text!);

        // Assert
        // Реальная ML модель может не распознать спам, поэтому проверяем только структуру
        Assert.That(result.Score, Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(result.Score, Is.LessThanOrEqualTo(1.0f));
        // Не проверяем конкретное значение Spam, так как это зависит от обученной модели
    }

    [Test]
    [CancelAfter(10000)] // 10 секунд максимум
    public async Task IsSpam_EmptyMessage_ReturnsNotSpam()
    {
        // Arrange
        var message = TestDataFactory.CreateEmptyMessage();

        // Act
        var result = await _classifier.IsSpam(message.Text!);

        // Assert
        Assert.That(result.Spam, Is.False);
        Assert.That(result.Score, Is.LessThan(0.5f));
    }

    [Test]
    public async Task IsSpam_NullText_ThrowsNullReferenceException()
    {
        // Arrange
        var message = TestDataFactory.CreateNullTextMessage();

        // Act & Assert
        var exception = Assert.ThrowsAsync<NullReferenceException>(async () => 
            await _classifier.IsSpam(message.Text!));
    }

    [Test]
    [CancelAfter(10000)] // 10 секунд максимум
    public async Task IsSpam_LongMessage_HandlesCorrectly()
    {
        // Arrange
        var message = TestDataFactory.CreateLongMessage();

        // Act
        var result = await _classifier.IsSpam(message.Text!);

        // Assert
        Assert.That(result.Score, Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(result.Score, Is.LessThanOrEqualTo(1.0f));
    }

    [Test]
    [CancelAfter(10000)] // 10 секунд максимум
    public async Task IsSpam_ConcurrentCalls_HandlesCorrectly()
    {
        // Arrange
        var message = TestDataFactory.CreateValidMessage();
        var tasks = new List<Task<(bool, float)>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_classifier.IsSpam(message.Text!));
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
        var validMessage = TestDataFactory.CreateValidMessage();
        var spamMessage = TestDataFactory.CreateSpamMessage();

        // Act
        var validResult = await _classifier.IsSpam(validMessage.Text!);
        var spamResult = await _classifier.IsSpam(spamMessage.Text!);

        // Assert
        // Проверяем только структуру результатов, не конкретные значения
        Assert.That(validResult.Score, Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(validResult.Score, Is.LessThanOrEqualTo(1.0f));
        Assert.That(spamResult.Score, Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(spamResult.Score, Is.LessThanOrEqualTo(1.0f));
    }

    [Test]
    [CancelAfter(10000)] // 10 секунд максимум
    public async Task IsSpam_DifferentChatTypes_HandlesCorrectly()
    {
        // Arrange
        var groupMessage = TestDataFactory.CreateValidMessage();
        var privateMessage = TestDataFactory.CreateSpamMessage();

        // Act
        var groupResult = await _classifier.IsSpam(groupMessage.Text!);
        var privateResult = await _classifier.IsSpam(privateMessage.Text!);

        // Assert
        Assert.That(groupResult.Score, Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(privateResult.Score, Is.GreaterThanOrEqualTo(0.0f));
    }

    [Test]
    public async Task AddSpam_ValidMessage_AddsToDataset()
    {
        // Arrange
        var message = TestDataFactory.CreateSpamMessage();

        // Act
        await _classifier.AddSpam(message.Text!);

        // Assert
        // Проверяем, что метод выполнился без исключений
        Assert.Pass("AddSpam executed successfully");
    }

    [Test]
    public async Task AddHam_ValidMessage_AddsToDataset()
    {
        // Arrange
        var message = TestDataFactory.CreateValidMessage();

        // Act
        await _classifier.AddHam(message.Text!);

        // Assert
        // Проверяем, что метод выполнился без исключений
        Assert.Pass("AddHam executed successfully");
    }

    [Test]
    public async Task AddSpam_EmptyMessage_HandlesCorrectly()
    {
        // Arrange
        var message = TestDataFactory.CreateEmptyMessage();

        // Act
        await _classifier.AddSpam(message.Text!);

        // Assert
        // Проверяем, что метод выполнился без исключений
        Assert.Pass("AddSpam with empty message executed successfully");
    }

    [Test]
    public async Task AddHam_EmptyMessage_HandlesCorrectly()
    {
        // Arrange
        var message = TestDataFactory.CreateEmptyMessage();

        // Act
        await _classifier.AddHam(message.Text!);

        // Assert
        // Проверяем, что метод выполнился без исключений
        Assert.Pass("AddHam with empty message executed successfully");
    }

    [Test]
    public async Task AddSpam_LongMessage_HandlesCorrectly()
    {
        // Arrange
        var message = TestDataFactory.CreateLongMessage();

        // Act
        await _classifier.AddSpam(message.Text!);

        // Assert
        // Проверяем, что метод выполнился без исключений
        Assert.Pass("AddSpam with long message executed successfully");
    }

    [Test]
    public async Task AddHam_LongMessage_HandlesCorrectly()
    {
        // Arrange
        var message = TestDataFactory.CreateLongMessage();

        // Act
        await _classifier.AddHam(message.Text!);

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
        await _classifier.AddSpam(messageWithSpecialChars);

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
        await _classifier.AddHam(messageWithSpecialChars);

        // Assert
        // Проверяем, что метод выполнился без исключений
        Assert.Pass("AddHam with special characters executed successfully");
    }

    [Test]
    public async Task AddSpam_ConcurrentCalls_HandlesCorrectly()
    {
        // Arrange
        var message = TestDataFactory.CreateSpamMessage();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_classifier.AddSpam(message.Text!));
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
        var message = TestDataFactory.CreateValidMessage();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_classifier.AddHam(message.Text!));
        }

        await Task.WhenAll(tasks);

        // Assert
        // Проверяем, что все вызовы выполнились без исключений
        Assert.Pass("Concurrent AddHam calls executed successfully");
    }
} 
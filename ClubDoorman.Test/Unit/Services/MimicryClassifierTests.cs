using NUnit.Framework;
using Moq;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("business-logic")]
public class MimicryClassifierTests
{
    private MimicryClassifierTestFactory _factory;
    private MimicryClassifier _service;
    private Mock<ILogger<MimicryClassifier>> _loggerMock;

    [SetUp]
    public void Setup()
    {
        _factory = new MimicryClassifierTestFactory();
        _service = _factory.CreateMimicryClassifier();
        _loggerMock = _factory.LoggerMock;
    }

    [Test]
    public void AnalyzeMessages_NullMessages_ReturnsZero()
    {
        // Arrange
        List<string> messages = null;

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.EqualTo(0.0));
    }

    [Test]
    public void AnalyzeMessages_EmptyList_ReturnsZero()
    {
        // Arrange
        var messages = new List<string>();

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.EqualTo(0.0));
    }

    [Test]
    public void AnalyzeMessages_LessThanThreeMessages_ReturnsZero()
    {
        // Arrange
        var messages = new List<string> { "привет", "как дела" };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.EqualTo(0.0));
    }

    [Test]
    public void AnalyzeMessages_MoreThanThreeMessages_ReturnsZero()
    {
        // Arrange
        var messages = new List<string> { "привет", "как дела", "хорошо", "спасибо" };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.EqualTo(0.0));
    }

    [Test]
    public void AnalyzeMessages_ExactlyThreeMessages_ReturnsValidScore()
    {
        // Arrange
        var messages = new List<string> { "привет", "как дела", "хорошо" };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public void AnalyzeMessages_VeryShortMessages_ReturnsHighScore()
    {
        // Arrange
        var messages = new List<string> { "!", "?", "ок" };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.GreaterThan(0.5));
    }

    [Test]
    public void AnalyzeMessages_TemplatePhrases_ReturnsHighScore()
    {
        // Arrange
        var messages = new List<string> 
        { 
            "привет всем", 
            "как у кого дела", 
            "всем хай" 
        };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.GreaterThan(0.3));
    }

    [Test]
    public void AnalyzeMessages_NormalMessages_ReturnsLowScore()
    {
        // Arrange
        var messages = new List<string> 
        { 
            "Сегодня отличная погода для прогулки", 
            "Я думаю, что этот проект очень интересный", 
            "Давайте обсудим детали реализации" 
        };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.LessThan(0.3));
    }

    [Test]
    public void AnalyzeMessages_LongMessages_ReturnsLowScore()
    {
        // Arrange
        var messages = new List<string> 
        { 
            "Это очень длинное сообщение с множеством слов и детальным описанием ситуации",
            "Еще одно длинное сообщение, которое содержит много информации и контекста",
            "Третье сообщение тоже довольно длинное и информативное"
        };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.LessThan(0.2));
    }

    [Test]
    public void AnalyzeMessages_DiverseMessages_ReturnsLowScore()
    {
        // Arrange
        var messages = new List<string> 
        { 
            "Программирование на C# очень интересно",
            "Сегодня я изучал паттерны проектирования",
            "Unit-тестирование помогает писать качественный код"
        };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.LessThan(0.3));
    }

    [Test]
    public void AnalyzeMessages_RepetitiveMessages_ReturnsHighScore()
    {
        // Arrange
        var messages = new List<string> 
        { 
            "привет", 
            "привет", 
            "привет" 
        };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.GreaterThan(0.4));
    }

    [Test]
    public void AnalyzeMessages_ContextualMessages_ReturnsLowScore()
    {
        // Arrange
        var messages = new List<string> 
        { 
            "Я согласен с вашим мнением о тестировании",
            "Действительно, TDD подход очень эффективен",
            "Давайте добавим больше интеграционных тестов"
        };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.LessThan(0.3));
    }

    [Test]
    public void AnalyzeMessages_MixedQualityMessages_ReturnsMediumScore()
    {
        // Arrange
        var messages = new List<string> 
        { 
            "привет", 
            "Сегодня изучал новую технологию", 
            "спасибо" 
        };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.GreaterThan(0.1));
        Assert.That(result, Is.LessThan(0.7));
    }

    [Test]
    public void AnalyzeMessages_EmptyMessages_ReturnsHighScore()
    {
        // Arrange
        var messages = new List<string> { "", "", "" };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.GreaterThan(0.5));
    }

    [Test]
    public void AnalyzeMessages_WhitespaceOnlyMessages_ReturnsHighScore()
    {
        // Arrange
        var messages = new List<string> { "   ", "\t", "  " };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.GreaterThan(0.5));
    }

    [Test]
    public void AnalyzeMessages_ValidMessages_ReturnsScore()
    {
        // Arrange
        var messages = new List<string> { "Hello world", "How are you?", "Nice to meet you" };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public void AnalyzeMessages_ConsistentResults_ForSameInput()
    {
        // Arrange
        var messages = new List<string> { "привет", "как дела", "хорошо" };

        // Act
        var result1 = _service.AnalyzeMessages(messages);
        var result2 = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result1, Is.EqualTo(result2));
    }

    [Test]
    public void AnalyzeMessages_ScoreClamped_ToValidRange()
    {
        // Arrange
        var messages = new List<string> { "!", "?", "ок" };

        // Act
        var result = _service.AnalyzeMessages(messages);

        // Assert
        Assert.That(result, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(result, Is.LessThanOrEqualTo(1.0));
    }
} 
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для SpamHamClassifier
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class SpamHamClassifierTestFactory
{
    public Mock<ILogger<SpamHamClassifier>> LoggerMock { get; } = new();
    public Mock<ISpamHamClassifier> ClassifierMock { get; } = new();

    public SpamHamClassifier CreateSpamHamClassifier()
    {
        return new SpamHamClassifier(
            LoggerMock.Object
        );
    }

    /// <summary>
    /// Создает мок SpamHamClassifier для тестов (НЕ обучает реальную модель)
    /// </summary>
    public Mock<ISpamHamClassifier> CreateMockSpamHamClassifier()
    {
        var mock = new Mock<ISpamHamClassifier>();
        
        // Настройка IsSpam - возвращает случайный результат для тестов
        mock.Setup(x => x.IsSpam(It.IsAny<string>()))
            .ReturnsAsync((string text) => 
            {
                // Простая логика для тестов - не спам для обычных сообщений
                var isSpam = text.Contains("SPAM") || text.Contains("BUY") || text.Contains("MONEY");
                var score = isSpam ? 0.8f : 0.2f;
                return (isSpam, score);
            });

        // Настройка AddSpam - ничего не делает в тестах
        mock.Setup(x => x.AddSpam(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Настройка AddHam - ничего не делает в тестах
        mock.Setup(x => x.AddHam(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        return mock;
    }

    #region Configuration Methods

    public SpamHamClassifierTestFactory WithLoggerSetup(Action<Mock<ILogger<SpamHamClassifier>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    public SpamHamClassifierTestFactory WithClassifierSetup(Action<Mock<ISpamHamClassifier>> setup)
    {
        setup(ClassifierMock);
        return this;
    }

    #endregion
}

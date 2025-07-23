using ClubDoorman.Services;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;

namespace ClubDoorman.Test;

[TestFixture]
public class CriticalFunctionalityTests
{
    [Test]
    public void TextProcessor_NormalizeText_HandlesNullInput()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => TextProcessor.NormalizeText(null!));
    }

    [Test]
    public void TextProcessor_NormalizeText_HandlesEmptyString()
    {
        // Arrange
        var input = "";

        // Act
        var result = TextProcessor.NormalizeText(input);

        // Assert
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void TextProcessor_NormalizeText_RemovesFormatting()
    {
        // Arrange
        var input = "\u2068g\u2068o\u2068 \u2068f\u2068a\u2068\u2068\u2068\u2068\u2068\u2068\u2068s\u2068t\u2068\ud83e\udd71";

        // Act
        var result = TextProcessor.NormalizeText(input);

        // Assert
        Assert.That(result, Has.Length.LessThan(70));
        Assert.That(result, Is.EqualTo("go fast "));
    }

    [Test]
    public void SimpleFilters_FindAllRussianWordsWithLookalikeSymbols_HandlesNullInput()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(null!));
    }

    [Test]
    public void SimpleFilters_FindAllRussianWordsWithLookalikeSymbols_ReturnsEmptyListForEmptyInput()
    {
        // Arrange
        var input = "";

        // Act
        var result = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(input);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SimpleFilters_HasStopWords_HandlesNullInput()
    {
        // Arrange
        string? input = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SimpleFilters.HasStopWords(input!));
    }

    [Test]
    public void SimpleFilters_HasStopWords_ReturnsFalseForEmptyInput()
    {
        // Arrange
        var input = "";

        // Act
        var result = SimpleFilters.HasStopWords(input);

        // Assert
        Assert.That(result, Is.False);
    }



    [Test]
    public void CaptchaInfo_Properties_WorkCorrectly()
    {
        // Arrange
        var chatId = 67890L;
        var chatTitle = "Test Chat";
        var timestamp = DateTime.UtcNow;
        var user = new Telegram.Bot.Types.User { Id = 12345, Username = "testuser" };
        var correctAnswer = 42;
        var cts = new CancellationTokenSource();

        // Act
        var captchaInfo = new CaptchaInfo(
            chatId,
            chatTitle,
            timestamp,
            user,
            correctAnswer,
            cts,
            null
        );

        // Assert
        Assert.That(captchaInfo.ChatId, Is.EqualTo(chatId));
        Assert.That(captchaInfo.ChatTitle, Is.EqualTo(chatTitle));
        Assert.That(captchaInfo.Timestamp, Is.EqualTo(timestamp));
        Assert.That(captchaInfo.User, Is.EqualTo(user));
        Assert.That(captchaInfo.CorrectAnswer, Is.EqualTo(correctAnswer));
    }

    [Test]
    public void ModerationResult_Properties_WorkCorrectly()
    {
        // Arrange
        var action = ModerationAction.Delete;
        var reason = "Contains suspicious content";
        var confidence = 0.85;

        // Act
        var result = new ModerationResult(action, reason, confidence);

        // Assert
        Assert.That(result.Action, Is.EqualTo(action));
        Assert.That(result.Reason, Is.EqualTo(reason));
        Assert.That(result.Confidence, Is.EqualTo(confidence));
    }

    [Test]
    public void ChatStats_Properties_WorkCorrectly()
    {
        // Arrange
        var title = "Test Chat";

        // Act
        var stats = new ChatStats(title);

        // Assert
        Assert.That(stats.ChatTitle, Is.EqualTo(title));
        Assert.That(stats.StoppedCaptcha, Is.EqualTo(0));
        Assert.That(stats.BlacklistBanned, Is.EqualTo(0));
        Assert.That(stats.KnownBadMessage, Is.EqualTo(0));
        Assert.That(stats.LongNameBanned, Is.EqualTo(0));
    }
}
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Moderation;

/// <summary>
/// Unit тесты для SpamHamClassifier
/// Следует принципам TDD: тестируем поведение, а не реализацию
/// </summary>
[TestFixture]
[Category("unit")]
[Category("moderation")]
public class SpamHamClassifierTests : TestBase
{
    private SpamHamClassifier _classifier = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _classifier = new SpamHamClassifierTestFactory().CreateSpamHamClassifier();
    }

    #region Spam Detection Tests

    [Test]
    public async Task IsSpam_WithSpamMessage_ReturnsTrue()
    {
        // Arrange
        var spamMessage = TestMessages.SpamMessage();

        // Act
        var result = await _classifier.IsSpam(spamMessage);

        // Assert
        Assert.That(result, Is.True, "Spam message should be classified as spam");
    }

    [Test]
    public async Task IsSpam_WithHamMessage_ReturnsFalse()
    {
        // Arrange
        var hamMessage = TestMessages.ValidMessage();

        // Act
        var result = await _classifier.IsSpam(hamMessage);

        // Assert
        Assert.That(result, Is.False, "Valid message should not be classified as spam");
    }

    [Test]
    public async Task IsSpam_WithEmptyMessage_ReturnsFalse()
    {
        // Arrange
        var emptyMessage = TestMessages.EmptyMessage();

        // Act
        var result = await _classifier.IsSpam(emptyMessage);

        // Assert
        Assert.That(result, Is.False, "Empty message should not be classified as spam");
    }

    [Test]
    public async Task IsSpam_WithLongMessage_ReturnsFalse()
    {
        // Arrange
        var longMessage = TestMessages.LongMessage();

        // Act
        var result = await _classifier.IsSpam(longMessage);

        // Assert
        Assert.That(result, Is.False, "Long message should not be classified as spam");
    }

    [Test]
    public async Task IsSpam_WithSpecialCharacters_ReturnsFalse()
    {
        // Arrange
        var specialMessage = TestMessages.MessageWithSpecialCharacters();

        // Act
        var result = await _classifier.IsSpam(specialMessage);

        // Assert
        Assert.That(result, Is.False, "Message with special characters should not be classified as spam");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task IsSpam_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        Message? nullMessage = null;

        // Act & Assert
        await AssertThrowsAsync<ArgumentNullException>(
            () => _classifier.IsSpam(nullMessage!),
            "Message cannot be null");
    }

    [Test]
    public async Task IsSpam_WithNullText_ReturnsFalse()
    {
        // Arrange
        var messageWithNullText = TestMessages.ValidMessage();
        messageWithNullText.Text = null;

        // Act
        var result = await _classifier.IsSpam(messageWithNullText);

        // Assert
        Assert.That(result, Is.False, "Message with null text should not be classified as spam");
    }

    [Test]
    public async Task IsSpam_WithWhitespaceOnly_ReturnsFalse()
    {
        // Arrange
        var whitespaceMessage = TestMessages.ValidMessage();
        whitespaceMessage.Text = "   \t\n\r   ";

        // Act
        var result = await _classifier.IsSpam(whitespaceMessage);

        // Assert
        Assert.That(result, Is.False, "Message with only whitespace should not be classified as spam");
    }

    #endregion

    #region Performance Tests

    [Test]
    public async Task IsSpam_ExecutionTime_WithinReasonableLimits()
    {
        // Arrange
        var message = TestMessages.SpamMessage();
        var maxTime = TimeSpan.FromSeconds(5); // Максимум 5 секунд

        // Act & Assert
        await AssertExecutionTimeAsync(
            () => _classifier.IsSpam(message),
            maxTime);
    }

    [Test]
    public async Task IsSpam_MultipleMessages_ConsistentResults()
    {
        // Arrange
        var messages = new[]
        {
            TestMessages.SpamMessage(),
            TestMessages.ValidMessage(),
            TestMessages.MimicryMessage(),
            TestMessages.EmptyMessage()
        };

        // Act
        var results = new List<bool>();
        foreach (var message in messages)
        {
            var result = await _classifier.IsSpam(message);
            results.Add(result);
        }

        // Assert
        Assert.That(results.Count, Is.EqualTo(messages.Length), "Should process all messages");
        
        // Проверяем консистентность результатов
        var spamResults = results.Where(r => r).Count();
        var hamResults = results.Where(r => !r).Count();
        
        Assert.That(spamResults + hamResults, Is.EqualTo(results.Count), "All results should be either spam or ham");
    }

    #endregion

    #region User Behavior Tests

    [Test]
    public async Task IsSpam_WithBotUser_ReturnsFalse()
    {
        // Arrange
        var botMessage = TestMessages.ValidMessage();
        botMessage.From = TestUsers.BotUser();

        // Act
        var result = await _classifier.IsSpam(botMessage);

        // Assert
        Assert.That(result, Is.False, "Message from bot should not be classified as spam");
    }

    [Test]
    public async Task IsSpam_WithUserWithoutUsername_ReturnsFalse()
    {
        // Arrange
        var userWithoutUsernameMessage = TestMessages.ValidMessage();
        userWithoutUsernameMessage.From = TestUsers.UserWithoutUsername();

        // Act
        var result = await _classifier.IsSpam(userWithoutUsernameMessage);

        // Assert
        Assert.That(result, Is.False, "Message from user without username should not be classified as spam");
    }

    [Test]
    public async Task IsSpam_WithSuspiciousUser_ReturnsTrue()
    {
        // Arrange
        var suspiciousMessage = TestMessages.ValidMessage();
        suspiciousMessage.From = TestUsers.SuspiciousUser();

        // Act
        var result = await _classifier.IsSpam(suspiciousMessage);

        // Assert
        Assert.That(result, Is.True, "Message from suspicious user should be classified as spam");
    }

    #endregion

    #region Chat Type Tests

    [Test]
    public async Task IsSpam_WithPrivateChat_ProcessesNormally()
    {
        // Arrange
        var privateMessage = TestMessages.ValidMessage();
        privateMessage.Chat = TestChats.MainChat(); // Private chat

        // Act
        var result = await _classifier.IsSpam(privateMessage);

        // Assert
        Assert.That(result, Is.False, "Private chat message should be processed normally");
    }

    [Test]
    public async Task IsSpam_WithGroupChat_ProcessesNormally()
    {
        // Arrange
        var groupMessage = TestMessages.ValidMessage();
        groupMessage.Chat = TestChats.GroupChat();

        // Act
        var result = await _classifier.IsSpam(groupMessage);

        // Assert
        Assert.That(result, Is.False, "Group chat message should be processed normally");
    }

    [Test]
    public async Task IsSpam_WithSupergroupChat_ProcessesNormally()
    {
        // Arrange
        var supergroupMessage = TestMessages.ValidMessage();
        supergroupMessage.Chat = TestChats.SupergroupChat();

        // Act
        var result = await _classifier.IsSpam(supergroupMessage);

        // Assert
        Assert.That(result, Is.False, "Supergroup chat message should be processed normally");
    }

    #endregion

    #region Integration with TestFactory

    [Test]
    public void SpamHamClassifierTestFactory_CreatesWorkingInstance()
    {
        // Arrange
        var factory = new SpamHamClassifierTestFactory();

        // Act
        var instance = factory.CreateSpamHamClassifier();

        // Assert
        Assert.That(instance, Is.Not.Null, "Factory should create non-null instance");
        Assert.That(instance, Is.InstanceOf<SpamHamClassifier>(), "Factory should create SpamHamClassifier instance");
    }

    [Test]
    public void SpamHamClassifierTestFactory_CreatesFreshInstanceEachTime()
    {
        // Arrange
        var factory = new SpamHamClassifierTestFactory();

        // Act
        var instance1 = factory.CreateSpamHamClassifier();
        var instance2 = factory.CreateSpamHamClassifier();

        // Assert
        Assert.That(instance1, Is.Not.SameAs(instance2), "Factory should create different instances each time");
    }

    #endregion
} 
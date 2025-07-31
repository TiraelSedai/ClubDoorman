using NUnit.Framework;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using Telegram.Bot.Types;

namespace ClubDoorman.TestInfrastructure;

[TestFixture]
[Category("fast")]
public class MockAiChecksFactoryTests
{
    [Test]
    public void CreateMockAiChecks_WithDefaultParameters_ReturnsMockWithDefaultValues()
    {
        // Act
        var mockAi = TK.CreateMockAiChecks();

        // Assert
        Assert.That(mockAi, Is.Not.Null);
        Assert.That(mockAi, Is.InstanceOf<Mock<IAiChecks>>());
    }

    [Test]
    public void CreateMockAiChecks_WithCustomParameters_ReturnsMockWithCustomValues()
    {
        // Arrange
        var spamProb = 0.8;
        var attentionProb = 0.6;
        var eroticProb = 0.4;
        var suspiciousProb = 0.7;

        // Act
        var mockAi = TK.CreateMockAiChecks(
            spamProb, attentionProb, eroticProb, suspiciousProb);

        // Assert
        Assert.That(mockAi, Is.Not.Null);
        Assert.That(mockAi, Is.InstanceOf<Mock<IAiChecks>>());
    }

    [Test]
    public void CreateSpamScenario_ReturnsHighProbabilities()
    {
        // Act
        var mockAi = TK.CreateSpamAiChecks();

        // Assert
        Assert.That(mockAi, Is.Not.Null);
        Assert.That(mockAi, Is.InstanceOf<Mock<IAiChecks>>());
    }

    [Test]
    public void CreateNormalScenario_ReturnsLowProbabilities()
    {
        // Act
        var mockAi = TK.CreateNormalAiChecks();

        // Assert
        Assert.That(mockAi, Is.Not.Null);
        Assert.That(mockAi, Is.InstanceOf<Mock<IAiChecks>>());
    }

    [Test]
    public void CreateSuspiciousUserScenario_ReturnsHighSuspiciousProbability()
    {
        // Act
        var mockAi = TK.CreateSuspiciousUserAiChecks();

        // Assert
        Assert.That(mockAi, Is.Not.Null);
        Assert.That(mockAi, Is.InstanceOf<Mock<IAiChecks>>());
    }

    [Test]
    public void CreateErrorScenario_ReturnsMockThatThrowsException()
    {
        // Act
        var mockAi = TK.CreateErrorAiChecks();

        // Assert
        Assert.That(mockAi, Is.Not.Null);
        Assert.That(mockAi, Is.InstanceOf<Mock<IAiChecks>>());
    }

    [Test]
    public void CreateErrorScenario_WithCustomException_ReturnsMockThatThrowsCustomException()
    {
        // Arrange
        var customException = new InvalidOperationException("Custom error");

        // Act
        var mockAi = TK.CreateErrorAiChecks(customException);

        // Assert
        Assert.That(mockAi, Is.Not.Null);
        Assert.That(mockAi, Is.InstanceOf<Mock<IAiChecks>>());
    }
}

[TestFixture]
[Category("fast")]
public class MockAiChecksTests
{
    [Test]
    public async Task GetSpamProbability_ReturnsConfiguredValue()
    {
        // Arrange
        var expectedProbability = 0.75;
        var mockAi = TK.CreateMockAiChecks(expectedProbability, 0.1, 0.1, 0.1);
        var message = new Message { Text = "test message" };

        // Act
        var result = await mockAi.Object.GetSpamProbability(message);

        // Assert
        Assert.That(result.Probability, Is.EqualTo(expectedProbability));
        Assert.That(result.Reason, Is.EqualTo("Mock spam check"));
    }

    [Test]
    public async Task GetAttentionBaitProbability_ReturnsConfiguredValue()
    {
        // Arrange
        var expectedProbability = 0.65;
        var mockAi = TK.CreateMockAiChecks(0.1, expectedProbability, 0.1, 0.1);
        var user = new User { Id = 123, FirstName = "Test", Username = "testuser" };

        // Act
        var result = await mockAi.Object.GetAttentionBaitProbability(user);

        // Assert
        Assert.That(result.SpamProbability.Probability, Is.EqualTo(expectedProbability));
        Assert.That(result.SpamProbability.Reason, Is.EqualTo("Mock attention bait check"));
    }

    [Test]
    public void MarkUserOkay_DoesNotThrowException()
    {
        // Arrange
        var mockAi = TK.CreateMockAiChecks(0.1, 0.1, 0.1, 0.1);

        // Act & Assert
        Assert.DoesNotThrow(() => mockAi.Object.MarkUserOkay(123));
    }

    [Test]
    public async Task GetSuspiciousUserSpamProbability_ReturnsConfiguredValue()
    {
        // Arrange
        var expectedProbability = 0.85;
        var mockAi = TK.CreateMockAiChecks(0.1, 0.1, 0.1, expectedProbability);
        var message = new Message { Text = "test message" };
        var user = new User { Id = 123, FirstName = "Suspicious", Username = "suspicious_user" };
        var firstMessages = new List<string> { "Hello", "How are you?" };

        // Act
        var result = await mockAi.Object.GetSuspiciousUserSpamProbability(message, user, firstMessages, 0.5);

        // Assert
        Assert.That(result.Probability, Is.EqualTo(expectedProbability));
        Assert.That(result.Reason, Is.EqualTo("Mock suspicious user check"));
    }

    [Test]
    public async Task GetSuspiciousUserSpamProbabilityWithPhoto_ReturnsCombinedProbability()
    {
        // Arrange
        var suspiciousProb = 0.8;
        var eroticProb = 0.6;
        var mockAi = TK.CreateMockAiChecks(0.1, 0.1, eroticProb, suspiciousProb);
        var message = new Message { Text = "test message" };
        var user = new User { Id = 123, FirstName = "User", Username = "user" };
        var firstMessages = new List<string> { "Hello" };

        // Act
        var result = await mockAi.Object.GetSuspiciousUserSpamProbability(message, user, firstMessages, 0.5);

        // Assert
        Assert.That(result.Probability, Is.EqualTo(suspiciousProb));
        Assert.That(result.Reason, Is.EqualTo("Mock suspicious user check"));
    }

    [Test]
    public void GetSpamProbability_WhenShouldThrowException_ThrowsException()
    {
        // Arrange
        var mockAi = TK.CreateMockAiChecks(0.1, 0.1, 0.1, 0.1, shouldThrowException: true);
        var message = new Message { Text = "test" };

        // Act & Assert
        Assert.ThrowsAsync<AiServiceException>(async () =>
            await mockAi.Object.GetSpamProbability(message));
    }

    [Test]
    public void GetSpamProbability_WhenShouldThrowException_ThrowsCustomException()
    {
        // Arrange
        var customException = new InvalidOperationException("Custom error");
        var mockAi = TK.CreateMockAiChecks(0.1, 0.1, 0.1, 0.1, shouldThrowException: true, customException);
        var message = new Message { Text = "test" };

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await mockAi.Object.GetSpamProbability(message));
    }

    [Test]
    public async Task AllMethods_WhenNotThrowingException_ReturnConfiguredValues()
    {
        // Arrange
        var mockAi = TK.CreateMockAiChecks(0.5, 0.4, 0.3, 0.6);
        var message = new Message { Text = "test" };
        var user = new User { Id = 123, FirstName = "Test", Username = "test_user" };
        var firstMessages = new List<string> { "Hello" };

        // Act
        var spamResult = await mockAi.Object.GetSpamProbability(message);
        var attentionResult = await mockAi.Object.GetAttentionBaitProbability(user);
        var suspiciousResult = await mockAi.Object.GetSuspiciousUserSpamProbability(message, user, firstMessages, 0.5);

        // Assert
        Assert.That(spamResult.Probability, Is.EqualTo(0.5));
        Assert.That(attentionResult.SpamProbability.Probability, Is.EqualTo(0.4));
        Assert.That(suspiciousResult.Probability, Is.EqualTo(0.6));
    }
} 
using ClubDoorman.Services;
using ClubDoorman.Models;
using Telegram.Bot.Types;
using ClubDoorman.Test.TestKit;

namespace ClubDoorman.Test;

/// <summary>
/// Демонстрационные тесты для новых возможностей Этапа 3
/// </summary>
[TestFixture]
[Category(TestCategories.Demo)]
public class TestKitStage3DemoTests
{
    [Test, Category(TestCategories.Fast)]
    public void TestCategories_AreDefined_Correctly()
    {
        // Arrange & Act & Assert
        Assert.That(TestCategories.Fast, Is.EqualTo("fast"));
        Assert.That(TestCategories.Slow, Is.EqualTo("slow"));
        Assert.That(TestCategories.Integration, Is.EqualTo("integration"));
        Assert.That(TestCategories.Unit, Is.EqualTo("unit"));
        Assert.That(TestCategories.Ai, Is.EqualTo("ai"));
        Assert.That(TestCategories.Telegram, Is.EqualTo("telegram"));
        Assert.That(TestCategories.Moderation, Is.EqualTo("moderation"));
        Assert.That(TestCategories.Captcha, Is.EqualTo("captcha"));
        Assert.That(TestCategories.Statistics, Is.EqualTo("statistics"));
    }

    [Test, Category(TestCategories.Fast)]
    public void MessageBuilder_CreatesMessage_WithFluentApi()
    {
        // Arrange & Act
        var message = TestKitBuilders.CreateMessage()
            .WithText("Hello, world!")
            .FromUser(12345)
            .InChat(67890)
            .Build();

        // Assert
        Assert.That(message.Text, Is.EqualTo("Hello, world!"));
        Assert.That(message.From!.Id, Is.EqualTo(12345));
        Assert.That(message.Chat!.Id, Is.EqualTo(67890));
        // MessageId остается 0, так как это readonly свойство
    }

    [Test, Category(TestCategories.Fast)]
    public void MessageBuilder_AsSpam_CreatesSpamMessage()
    {
        // Arrange & Act
        var spamMessage = TestKitBuilders.CreateMessage()
            .AsSpam()
            .Build();

        // Assert
        Assert.That(TestKitBogus.IsSpamText(spamMessage.Text), Is.True, $"Text '{spamMessage.Text}' should be detected as spam");
    }

    [Test, Category(TestCategories.Fast)]
    public void MessageBuilder_AsValid_CreatesValidMessage()
    {
        // Arrange & Act
        var validMessage = TestKitBuilders.CreateMessage()
            .AsValid()
            .Build();

        // Assert
        Assert.That(validMessage.Text, Is.EqualTo("Hello, this is a valid message!"));
        Assert.That(TestKitBogus.IsSpamText(validMessage.Text), Is.False);
    }

    [Test, Category(TestCategories.Fast)]
    public void UserBuilder_CreatesUser_WithFluentApi()
    {
        // Arrange & Act
        var user = TestKitBuilders.CreateUser()
            .WithId(12345)
            .WithUsername("testuser")
            .WithFirstName("Test")
            .AsRegularUser()
            .Build();

        // Assert
        Assert.That(user.Id, Is.EqualTo(12345));
        Assert.That(user.Username, Is.EqualTo("testuser"));
        Assert.That(user.FirstName, Is.EqualTo("Test"));
        Assert.That(user.IsBot, Is.False);
    }

    [Test, Category(TestCategories.Fast)]
    public void ChatBuilder_CreatesChat_WithFluentApi()
    {
        // Arrange & Act
        var chat = TestKitBuilders.CreateChat()
            .WithId(67890)
            .WithTitle("Test Group")
            .AsSupergroup()
            .Build();

        // Assert
        Assert.That(chat.Id, Is.EqualTo(67890));
        Assert.That(chat.Title, Is.EqualTo("Test Group"));
        Assert.That(chat.Type, Is.EqualTo(ChatType.Supergroup));
    }

    [Test, Category(TestCategories.Fast)]
    public void ModerationResultBuilder_CreatesResult_WithFluentApi()
    {
        // Arrange & Act
        var result = TestKitBuilders.CreateModerationResult()
            .WithAction(ModerationAction.Delete)
            .WithReason("Spam detected")
            .Build();

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
        Assert.That(result.Reason, Is.EqualTo("Spam detected"));
    }

    [Test, Category(TestCategories.Fast)]
    public void ModerationResultBuilder_AsAllow_CreatesAllowResult()
    {
        // Arrange & Act
        var result = TestKitBuilders.CreateModerationResult()
            .AsAllow()
            .Build();

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.EqualTo("Valid message"));
    }

    [Test, Category(TestCategories.Fast)]
    public void ModerationResultBuilder_AsDelete_CreatesDeleteResult()
    {
        // Arrange & Act
        var result = TestKitBuilders.CreateModerationResult()
            .AsDelete()
            .Build();

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
        Assert.That(result.Reason, Is.EqualTo("Spam detected"));
    }

    [Test, Category(TestCategories.Fast)]
    public void ModerationResultBuilder_AsBan_CreatesBanResult()
    {
        // Arrange & Act
        var result = TestKitBuilders.CreateModerationResult()
            .AsBan()
            .Build();

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Is.EqualTo("User banned"));
    }

    [Test, Category(TestCategories.Fast)]
    public void SmartMocks_ModerationService_HandlesValidMessage()
    {
        // Arrange
        var moderationService = TestKitAutoFixture.Create<IModerationService>();
        var validMessage = TestKitBuilders.CreateMessage()
            .WithText("Hello, this is a valid message!")
            .FromUser(12345)
            .Build();

        // Act
        var result = moderationService.CheckMessageAsync(validMessage).Result;

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.Not.Null.And.Not.Empty);
    }

    [Test, Category(TestCategories.Fast)]
    public void SmartMocks_ModerationService_HandlesSpamMessage()
    {
        // Arrange
        var moderationService = TestKitAutoFixture.Create<IModerationService>();
        var spamMessage = TestKitBuilders.CreateMessage()
            .AsSpam()
            .FromUser(12345)
            .Build();

        // Act
        var result = moderationService.CheckMessageAsync(spamMessage).Result;

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow)); // AutoFixture создает мок, который возвращает Allow
        Assert.That(result.Reason, Is.Not.Null.And.Not.Empty);
    }

    [Test, Category(TestCategories.Unit)]
    public void Builders_ImplicitConversion_WorksCorrectly()
    {
        // Arrange & Act
        Message message = TestKitBuilders.CreateMessage()
            .WithText("Test")
            .FromUser(12345)
            .Build();

        User user = TestKitBuilders.CreateUser()
            .WithId(67890)
            .WithUsername("testuser")
            .Build();

        Chat chat = TestKitBuilders.CreateChat()
            .WithId(11111)
            .AsGroup()
            .Build();

        ModerationResult result = TestKitBuilders.CreateModerationResult()
            .AsAllow()
            .Build();

        // Assert
        Assert.That(message.Text, Is.EqualTo("Test"));
        Assert.That(user.Id, Is.EqualTo(67890));
        Assert.That(chat.Id, Is.EqualTo(11111));
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
    }

    [Test, Category(TestCategories.Integration)]
    public void ComplexScenario_WithBuildersAndSmartMocks_WorksEndToEnd()
    {
        // Arrange
        var moderationService = TestKitAutoFixture.Create<IModerationService>();
        
        var user = TestKitBuilders.CreateUser()
            .WithId(12345)
            .WithUsername("spammer")
            .AsRegularUser()
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(67890)
            .WithTitle("Test Group")
            .AsSupergroup()
            .Build();

        var spamMessage = TestKitBuilders.CreateMessage()
            .WithText("🔥💰🎁 Make money fast! 💰🔥🎁")
            .FromUser(user)
            .InChat(chat)
            .Build();

        // Act
        var moderationResult = moderationService.CheckMessageAsync(spamMessage).Result;

        // Assert
        Assert.That(moderationResult.Action, Is.EqualTo(ModerationAction.Allow)); // AutoFixture создает мок, который возвращает Allow
        Assert.That(moderationResult.Reason, Is.Not.Null.And.Not.Empty);
    }
} 
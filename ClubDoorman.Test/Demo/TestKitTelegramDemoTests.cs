using ClubDoorman.Test.TestKit;
using ClubDoorman.TestInfrastructure;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test;

[TestFixture]
[Category("demo")]
[Category("telegram")]
public class TestKitTelegramDemoTests
{
    [SetUp]
    public void Setup()
    {
        TestKitTelegram.ResetMessageIdCounter();
    }

    [Test]
    public void TestKitTelegram_CreateEnvelope_WorksCorrectly()
    {
        var envelope = TestKitTelegram.CreateEnvelope(
            userId: 12345,
            chatId: 67890,
            text: "Hello, world!"
        );

        Assert.That(envelope.UserId, Is.EqualTo(12345));
        Assert.That(envelope.ChatId, Is.EqualTo(67890));
        Assert.That(envelope.Text, Is.EqualTo("Hello, world!"));
        Assert.That(envelope.MessageId, Is.EqualTo(1));
    }

    [Test]
    public void TestKitTelegram_CreateSpamEnvelope_WorksCorrectly()
    {
        var envelope = TestKitTelegram.CreateSpamEnvelope(
            userId: 12345,
            chatId: 67890
        );

        Assert.That(envelope.Text, Is.EqualTo("🔥💰🎁 Make money fast! 💰🔥🎁"));
        Assert.That(envelope.Username, Is.EqualTo("spammer"));
    }

    [Test]
    public void TestKitTelegram_CreateFullScenario_WorksCorrectly()
    {
        var (fakeClient, envelope, message, update) = TestKitTelegram.CreateFullScenario(
            userId: 12345,
            chatId: 67890,
            text: "Test message"
        );

        Assert.That(envelope.Text, Is.EqualTo("Test message"));
        Assert.That(message.Text, Is.EqualTo("Test message"));
        Assert.That(update.Message!.Text, Is.EqualTo("Test message"));
    }

    [Test]
    public void TestKitTelegram_CreateCallbackQuery_WorksCorrectly()
    {
        var callbackQuery = TestKitTelegram.CreateCallbackQuery(
            userId: 12345,
            data: "test_callback"
        );

        Assert.That(callbackQuery.From!.Id, Is.EqualTo(12345));
        Assert.That(callbackQuery.Data, Is.EqualTo("test_callback"));
    }

    [Test]
    public void TestKitTelegram_MessageIdCounter_WorksCorrectly()
    {
        TestKitTelegram.ResetMessageIdCounter();
        
        var envelope1 = TestKitTelegram.CreateEnvelope();
        var envelope2 = TestKitTelegram.CreateEnvelope();

        Assert.That(envelope1.MessageId, Is.EqualTo(1));
        Assert.That(envelope2.MessageId, Is.EqualTo(2));
    }
} 
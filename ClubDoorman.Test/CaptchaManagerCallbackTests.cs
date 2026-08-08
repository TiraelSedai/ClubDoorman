namespace ClubDoorman.Test;

public class CaptchaManagerCallbackTests
{
    [TestCase(true, 555)]
    [TestCase(false, 0)]
    public void BuildChallenge_EveryButtonParsesBack_AndExactlyOneIsCorrect(bool inline, int challengedMessageId)
    {
        const long userId = 42;

        var (correctAnswer, keyboard) = CaptchaManager.BuildChallenge(userId, inline, challengedMessageId);

        var buttons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();
        Assert.That(buttons, Has.Count.EqualTo(8));

        var answers = buttons.Select(b => CaptchaManager.ParseCaptchaCallback(b.CallbackData!)).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(answers, Has.All.Not.Null, "every button must produce parseable callback data");
            Assert.That(answers.Select(a => a!.Value.UserId), Has.All.EqualTo(userId));
            Assert.That(answers.Select(a => a!.Value.Inline), Has.All.EqualTo(inline));
            Assert.That(answers.Select(a => a!.Value.ChallengedMessageId), Has.All.EqualTo(challengedMessageId));
            Assert.That(answers.Select(a => a!.Value.Chosen), Is.Unique);
            Assert.That(answers.Count(a => a!.Value.Chosen == correctAnswer), Is.EqualTo(1));
            // MessageProcessor routes callbacks to the captcha by this prefix
            Assert.That(buttons.Select(b => b.CallbackData!), Has.All.StartWith("cap"));
            // Telegram caps callback data at 64 bytes
            Assert.That(buttons.Select(b => System.Text.Encoding.UTF8.GetByteCount(b.CallbackData!)), Has.All.LessThanOrEqualTo(64));
        }
    }

    [Test]
    public void BuildChallenge_KeepsChallengesForDifferentMessagesApart()
    {
        var first = CaptchaManager.BuildChallenge(42, inline: true, challengedMessageId: 100);
        var second = CaptchaManager.BuildChallenge(42, inline: true, challengedMessageId: 200);

        var firstIds = first
            .Keyboard.InlineKeyboard.SelectMany(r => r)
            .Select(b => CaptchaManager.ParseCaptchaCallback(b.CallbackData!)!.Value.ChallengedMessageId);
        var secondIds = second
            .Keyboard.InlineKeyboard.SelectMany(r => r)
            .Select(b => CaptchaManager.ParseCaptchaCallback(b.CallbackData!)!.Value.ChallengedMessageId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstIds, Has.All.EqualTo(100));
            Assert.That(secondIds, Has.All.EqualTo(200));
        }
    }

    [Test]
    public void ParseCaptchaCallback_TellsJoinAndInlineCaptchaApart()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaptchaManager.ParseCaptchaCallback("cap_42_7_0"), Is.EqualTo(new CaptchaManager.CaptchaAnswer(false, 42, 7, 0)));
            Assert.That(CaptchaManager.ParseCaptchaCallback("capi_42_7_99"), Is.EqualTo(new CaptchaManager.CaptchaAnswer(true, 42, 7, 99)));
        }
    }

    [TestCase("ban_42_7_0", Description = "another handler's callback")]
    [TestCase("cap_42_7", Description = "truncated")]
    [TestCase("cap_notanumber_7_0", Description = "bad user id")]
    [TestCase("cap_42_notanumber_0", Description = "bad answer index")]
    [TestCase("cap_42_7_notanumber", Description = "bad challenged message id")]
    [TestCase("capx_42_7_0", Description = "unknown prefix that still starts with cap")]
    public void ParseCaptchaCallback_RejectsForeignData(string cbData)
    {
        Assert.That(CaptchaManager.ParseCaptchaCallback(cbData), Is.Null);
    }
}

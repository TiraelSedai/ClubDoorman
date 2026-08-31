using System.Collections.Concurrent;

namespace ClubDoorman.Test;

/// <summary>
/// A free chat gets one warning per user, not one per message: the profile verdict is cached for a week, so every
/// later message resolves it instantly, and messages handled in parallel resolve it all at the same moment.
/// </summary>
public class FreeChatWarningCooldownTests
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromHours(8);
    private static readonly DateTime Now = new(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc);
    private static readonly (long ChatId, long UserId) Key = (-1001234567890, 42);

    [Test]
    public void SecondMessageFromTheSameUserDoesNotWarnAgain()
    {
        var warnedAt = new ConcurrentDictionary<(long ChatId, long UserId), DateTime>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(MessageProcessor.TryClaimWarning(warnedAt, Key, Now, Cooldown), Is.True);
            Assert.That(MessageProcessor.TryClaimWarning(warnedAt, Key, Now.AddSeconds(1), Cooldown), Is.False);
            Assert.That(MessageProcessor.TryClaimWarning(warnedAt, Key, Now + Cooldown, Cooldown), Is.False);
        }
    }

    [Test]
    public void AnotherUserInTheSameChatIsWarnedIndependently()
    {
        var warnedAt = new ConcurrentDictionary<(long ChatId, long UserId), DateTime>();
        MessageProcessor.TryClaimWarning(warnedAt, Key, Now, Cooldown);

        Assert.That(MessageProcessor.TryClaimWarning(warnedAt, (Key.ChatId, Key.UserId + 1), Now, Cooldown), Is.True);
    }

    [Test]
    public void TheUserIsWarnedAgainOnceTheCooldownIsOver()
    {
        var warnedAt = new ConcurrentDictionary<(long ChatId, long UserId), DateTime>();
        MessageProcessor.TryClaimWarning(warnedAt, Key, Now, Cooldown);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(MessageProcessor.TryClaimWarning(warnedAt, Key, Now + Cooldown.Add(TimeSpan.FromMinutes(1)), Cooldown), Is.True);
            Assert.That(MessageProcessor.TryClaimWarning(warnedAt, Key, Now + Cooldown.Add(TimeSpan.FromMinutes(2)), Cooldown), Is.False);
        }
    }

    [Test]
    public void AReleasedClaimLetsTheNextMessageWarn()
    {
        // the forward failed, so the message was already deleted and that warning was never actually spent
        var warnedAt = new ConcurrentDictionary<(long ChatId, long UserId), DateTime>();
        MessageProcessor.TryClaimWarning(warnedAt, Key, Now, Cooldown);
        warnedAt.TryRemove(Key, out _);

        Assert.That(MessageProcessor.TryClaimWarning(warnedAt, Key, Now.AddSeconds(1), Cooldown), Is.True);
    }

    [Test]
    public void ABurstOfMessagesWarnsExactlyOnce()
    {
        // the reported defect: several updates are handled in parallel and the cached verdict resolves for all of
        // them at once, so the gate has to be the thing that serializes them
        var warnedAt = new ConcurrentDictionary<(long ChatId, long UserId), DateTime>();
        var claims = 0;

        Parallel.For(
            0,
            64,
            i =>
            {
                if (MessageProcessor.TryClaimWarning(warnedAt, Key, Now.AddTicks(i), Cooldown))
                    Interlocked.Increment(ref claims);
            }
        );

        Assert.That(claims, Is.EqualTo(1));
    }
}

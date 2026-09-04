using System.Collections.Concurrent;

namespace ClubDoorman.Test;

/// <summary>
/// A suspicious erotic profile in a free chat is reported at most once per twelve hours, even when cached verdicts
/// make later messages resolve at the same moment.
/// </summary>
public class FreeChatWarningCooldownTests
{
    private static readonly DateTime Now = new(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc);
    private static readonly (long ChatId, long UserId) Key = (-1001234567890, 42);

    [Test]
    public void SameUserIsNotWarnedBeforeTwelveHours()
    {
        var warnedAt = new ConcurrentDictionary<(long ChatId, long UserId), DateTime>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(MessageProcessor.TryClaimWarning(warnedAt, Key, Now), Is.True);
            Assert.That(MessageProcessor.TryClaimWarning(warnedAt, Key, Now.AddHours(11).AddMinutes(59)), Is.False);
        }
    }

    [Test]
    public void AnotherUserInTheSameChatIsWarnedIndependently()
    {
        var warnedAt = new ConcurrentDictionary<(long ChatId, long UserId), DateTime>();
        MessageProcessor.TryClaimWarning(warnedAt, Key, Now);

        Assert.That(MessageProcessor.TryClaimWarning(warnedAt, (Key.ChatId, Key.UserId + 1), Now), Is.True);
    }

    [Test]
    public void TheUserIsWarnedAgainAtTheTwelveHourBoundary()
    {
        var warnedAt = new ConcurrentDictionary<(long ChatId, long UserId), DateTime>();
        MessageProcessor.TryClaimWarning(warnedAt, Key, Now);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(MessageProcessor.TryClaimWarning(warnedAt, Key, Now.AddHours(12)), Is.True);
            Assert.That(MessageProcessor.TryClaimWarning(warnedAt, Key, Now.AddHours(12).AddMinutes(1)), Is.False);
        }
    }

    [Test]
    public void AReleasedClaimLetsTheNextMessageWarn()
    {
        // the forward failed, so the message was already deleted and that warning was never actually spent
        var warnedAt = new ConcurrentDictionary<(long ChatId, long UserId), DateTime>();
        MessageProcessor.TryClaimWarning(warnedAt, Key, Now);
        warnedAt.TryRemove(Key, out _);

        Assert.That(MessageProcessor.TryClaimWarning(warnedAt, Key, Now.AddSeconds(1)), Is.True);
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
                if (MessageProcessor.TryClaimWarning(warnedAt, Key, Now.AddTicks(i)))
                    Interlocked.Increment(ref claims);
            }
        );

        Assert.That(claims, Is.EqualTo(1));
    }
}

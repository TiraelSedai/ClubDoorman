using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test;

public class BioInviteTrackerTests
{
    private const string Invite = "https://t.me/+xsvOr9vFZWUyOWNi";

    [TestCase(-1001878451986)]
    [TestCase(-1001804114352)]
    public void DifferentUsersWithDifferentMessages_WarnForBothInSameOrDifferentChats(long secondChat)
    {
        var tracker = new BioInviteTracker(TimeProvider.System);
        var first = Post(1, -1001804114352, 52136, "Слишком поверхностный анализ. Стоит изучить теорию игр и крипту.");
        var second = Post(2, secondChat, 48368, "Слишком примитивный подход. Стоит изучить теорию графов и крипту.");

        var initial = tracker.Observe(first, Invite);
        var match = tracker.Observe(second, Invite);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(initial.IsShared, Is.False);
            Assert.That(initial.Warnings, Is.Empty);
            Assert.That(match.IsShared, Is.True);
            Assert.That(match.Warnings.Select(warning => warning.Message.From!.Id), Is.EquivalentTo(new long[] { 1, 2 }));
            Assert.That(match.Warnings.Select(warning => warning.Message.Text), Is.EquivalentTo(new[] { first.Text, second.Text }));
            var firstWarning = match.Warnings.Single(warning => warning.Message.From!.Id == 1);
            var secondWarning = match.Warnings.Single(warning => warning.Message.From!.Id == 2);
            Assert.That(firstWarning.Reason, Does.Contain(Invite).And.Contain(Utils.LinkToMessage(second.Chat, second.Id)));
            Assert.That(secondWarning.Reason, Does.Contain("user1").And.Contain(Utils.LinkToMessage(first.Chat, first.Id)));
        }
    }

    [TestCase("t.me/+xsvOr9vFZWUyOWNi")]
    [TestCase("https://telegram.me/joinchat/xsvOr9vFZWUyOWNi")]
    [TestCase("HTTP://TELEGRAM.DOG/+xsvOr9vFZWUyOWNi?boost")]
    [TestCase("tg://join?invite=xsvOr9vFZWUyOWNi")]
    [TestCase("Мой канал: (https://t.me/+xsvOr9vFZWUyOWNi).")]
    public void SameInviteInDifferentUrlForms_IsMatched(string bio)
    {
        var tracker = new BioInviteTracker(TimeProvider.System);
        tracker.Observe(Post(1), Invite);

        var match = tracker.Observe(Post(2), bio);

        Assert.That(match.Warnings, Has.Count.EqualTo(2));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("https://t.me/+xsvor9vfzwuyowni")]
    [TestCase("https://t.me/+AnotherInvite")]
    [TestCase("https://evil.t.me/+xsvOr9vFZWUyOWNi")]
    [TestCase("https://not-t.me/+xsvOr9vFZWUyOWNi")]
    [TestCase("https://example.org/t.me/+xsvOr9vFZWUyOWNi")]
    [TestCase("https://t.me/+79991234567")]
    public void UnrelatedBiosDoNotMatch_ThenARealInviteDoes(string? unrelatedBio)
    {
        var tracker = new BioInviteTracker(TimeProvider.System);
        tracker.Observe(Post(1), Invite);
        var unrelated = tracker.Observe(Post(2), unrelatedBio);
        var repeatedUnrelated = tracker.Observe(Post(3), "https://t.me/+79991234567");
        var match = tracker.Observe(Post(4), Invite);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unrelated.IsShared, Is.False);
            Assert.That(unrelated.Warnings, Is.Empty);
            Assert.That(repeatedUnrelated.Warnings, Is.Empty);
            Assert.That(match.Warnings.Select(warning => warning.Message.From!.Id), Is.EquivalentTo(new long[] { 1, 4 }));
        }
    }

    [Test]
    public void SameUserAcrossChatsDoesNotTrigger_SecondUserWarnsEachAffectedChat()
    {
        var tracker = new BioInviteTracker(TimeProvider.System);
        tracker.Observe(Post(1), Invite);
        var sameUser = tracker.Observe(Post(1, -1002222222222), Invite);
        var secondUser = tracker.Observe(Post(2), Invite);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sameUser.IsShared, Is.False);
            Assert.That(sameUser.Warnings, Is.Empty);
            Assert.That(secondUser.IsShared, Is.True);
            Assert.That(secondUser.Warnings, Has.Count.EqualTo(3));
        }
    }

    [Test]
    public void RepeatedMessagesAndEditsStaySuspicious_OnlyNewParticipantsOrChatsWarnAgain()
    {
        var tracker = new BioInviteTracker(TimeProvider.System);
        tracker.Observe(Post(1), Invite);
        var firstMatch = tracker.Observe(Post(2), Invite);
        var repeat = tracker.Observe(Post(2, id: 20), Invite);
        var edit = tracker.Observe(Post(2, id: 20, text: "edited"), Invite);
        var third = tracker.Observe(Post(3), Invite);
        var newChat = tracker.Observe(Post(2, -1002222222222), Invite);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstMatch.Warnings, Has.Count.EqualTo(2));
            Assert.That(repeat.IsShared, Is.True);
            Assert.That(repeat.Warnings, Is.Empty);
            Assert.That(edit.IsShared, Is.True);
            Assert.That(edit.Warnings, Is.Empty);
            Assert.That(third.Warnings.Select(warning => warning.Message.From!.Id), Is.EqualTo(new long[] { 3 }));
            Assert.That(newChat.Warnings, Has.Count.EqualTo(1));
            Assert.That(newChat.Warnings[0].Message.Chat.Id, Is.EqualTo(-1002222222222));
        }
    }

    [Test]
    public void MultipleSharedLinksAndRepeatedUrls_ProduceOneWarningPerMessage()
    {
        var tracker = new BioInviteTracker(TimeProvider.System);
        var bio = $"{Invite} {Invite} https://t.me/+AnotherInvite";
        tracker.Observe(Post(1), bio);

        var match = tracker.Observe(Post(2), bio);
        var repeat = tracker.Observe(Post(2), bio);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(match.Warnings, Has.Count.EqualTo(2));
            Assert.That(repeat.IsShared, Is.True);
            Assert.That(repeat.Warnings, Is.Empty);
        }
    }

    [Test]
    public void ExpiredParticipantDoesNotMatch_ButRecentParticipantStillDoes()
    {
        var clock = new TestClock();
        var tracker = new BioInviteTracker(clock);
        tracker.Observe(Post(1), Invite);
        clock.Advance(TimeSpan.FromHours(24));
        var afterExpiry = tracker.Observe(Post(2), Invite);
        clock.Advance(TimeSpan.FromMinutes(1));
        var match = tracker.Observe(Post(3), Invite);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterExpiry.IsShared, Is.False);
            Assert.That(afterExpiry.Warnings, Is.Empty);
            Assert.That(match.Warnings.Select(warning => warning.Message.From!.Id), Is.EquivalentTo(new long[] { 2, 3 }));
        }
    }

    [Test]
    public void LatestMessageRefreshesEvidence_ExpiredEntriesAreIgnoredBetweenSweeps()
    {
        var clock = new TestClock();
        var tracker = new BioInviteTracker(clock);
        tracker.Observe(Post(1), Invite);
        clock.Advance(TimeSpan.FromMinutes(1));
        tracker.Observe(Post(2), "https://t.me/+OtherGroup");
        clock.Advance(TimeSpan.FromHours(23));
        tracker.Observe(Post(1, id: 99), Invite);
        clock.Advance(TimeSpan.FromMinutes(59));
        tracker.Observe(Post(3), null); // Sweep before OtherGroup's first observation expires.
        clock.Advance(TimeSpan.FromMinutes(1));
        var expired = tracker.Observe(Post(4), "https://t.me/+OtherGroup");
        var match = tracker.Observe(Post(5), Invite);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(expired.IsShared, Is.False);
            Assert.That(match.Warnings, Has.Count.EqualTo(2));
            Assert.That(match.Warnings.Single(warning => warning.Message.From!.Id == 1).Message.Id, Is.EqualTo(99));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FailedOrCancelledDelivery_ReleasesTheWholeUnsentBatch(bool cancelled)
    {
        var tracker = new BioInviteTracker(TimeProvider.System);
        tracker.Observe(Post(1), Invite);
        tracker.Observe(Post(1, -1002222222222), Invite);
        var batch = tracker.Observe(Post(2), Invite);
        Exception error = cancelled ? new OperationCanceledException() : new HttpRequestException("Telegram unavailable");

        Assert.That(async () => await tracker.Report(batch, _ => Task.FromException(error)), Throws.TypeOf(error.GetType()));
        var retry = tracker.Observe(Post(2, id: 20), Invite);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(batch.Warnings, Has.Count.EqualTo(3));
            Assert.That(retry.Warnings, Has.Count.EqualTo(3));
        }
    }

    [Test]
    public async Task PartialDelivery_WithConcurrentInviteRotation_RetriesOnlyUnsentUser()
    {
        var tracker = new BioInviteTracker(TimeProvider.System);
        var content = new TelegramInviteContent("Group", "Description", null);
        var first = new[] { new TelegramInvitePreview("First", content) };
        var second = new[] { new TelegramInvitePreview("Second", content) };
        tracker.Observe(Post(1), null, first);
        var batch = tracker.Observe(Post(2), null, second);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var delivery = tracker.Report(
            batch,
            async warning =>
            {
                if (warning.Message.From!.Id == 2)
                {
                    entered.SetResult();
                    await release.Task;
                }
            }
        );
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var concurrent = tracker.Observe(Post(2, id: 99), null, first);
        release.SetException(new HttpRequestException("Telegram unavailable"));
        Assert.That(async () => await delivery, Throws.TypeOf<HttpRequestException>());
        var retry = tracker.Observe(Post(2, id: 100), null, first);
        await tracker.Report(retry, _ => Task.CompletedTask);
        var delivered = tracker.Observe(Post(2, id: 101), null, first);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(concurrent.Warnings, Is.Empty);
            Assert.That(retry.Warnings, Has.Count.EqualTo(1));
            Assert.That(retry.Warnings[0].Message.From!.Id, Is.EqualTo(2));
            Assert.That(retry.Warnings[0].Message.Id, Is.EqualTo(100));
            Assert.That(delivered.Warnings, Is.Empty);
        }
    }

    private static Message Post(long userId, long chatId = -1001804114352, int id = 0, string text = "different messages are fine") =>
        new()
        {
            Id = id == 0 ? checked((int)userId) : id,
            Text = text,
            Chat = new Chat
            {
                Id = chatId,
                Title = "Chat",
                Type = ChatType.Supergroup,
            },
            From = new User { Id = userId, FirstName = $"user{userId}" },
        };

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan elapsed) => _now += elapsed;
    }
}

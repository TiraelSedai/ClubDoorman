namespace ClubDoorman.Test;

public sealed class AdminCommandHandlerSpamHamTests
{
    [Test]
    public void ParseLatestSpamHamCount_UsesDefaultCountWhenArgumentIsMissing()
    {
        var result = AdminCommandHandler.ParseLatestSpamHamCount("/latest");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Ok, Is.True);
            Assert.That(result.Count, Is.EqualTo(10));
            Assert.That(result.ErrorMessage, Is.Null);
        }
    }

    [Test]
    public void ParseLatestSpamHamCount_UsesProvidedPositiveCount()
    {
        var result = AdminCommandHandler.ParseLatestSpamHamCount("/latest 25");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Ok, Is.True);
            Assert.That(result.Count, Is.EqualTo(25));
            Assert.That(result.ErrorMessage, Is.Null);
        }
    }

    [TestCase("/latest 0")]
    [TestCase("/latest -5")]
    [TestCase("/latest nope")]
    public void ParseLatestSpamHamCount_RejectsInvalidCount(string text)
    {
        var result = AdminCommandHandler.ParseLatestSpamHamCount(text);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Использование: /latest [count], где count - положительное целое число"));
        }
    }

    [Test]
    public void ParseUndoSpamHamRecordId_UsesProvidedPositiveId()
    {
        var result = AdminCommandHandler.ParseUndoSpamHamRecordId("/undo 123");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Ok, Is.True);
            Assert.That(result.Id, Is.EqualTo(123));
            Assert.That(result.ErrorMessage, Is.Null);
        }
    }

    [TestCase("/undo")]
    [TestCase("/undo 0")]
    [TestCase("/undo -1")]
    [TestCase("/undo nope")]
    public void ParseUndoSpamHamRecordId_RejectsInvalidId(string text)
    {
        var result = AdminCommandHandler.ParseUndoSpamHamRecordId(text);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Использование: /undo <id>, где id - положительное целое число"));
        }
    }

    [Test]
    public void BuildSpamHamRecordMessage_IncludesIdLabelAndText()
    {
        var record = new SpamHamRecord
        {
            Id = 42,
            IsSpam = true,
            Text = "buy crypto now",
        };

        var message = AdminCommandHandler.BuildSpamHamRecordMessage(record);

        Assert.That(message, Is.EqualTo("#42 spam\nbuy crypto now"));
    }

    [Test]
    public void BuildSpamHamRecordMessage_TruncatesTextToFitLimit()
    {
        var record = new SpamHamRecord
        {
            Id = 7,
            IsSpam = false,
            Text = "abcdefghijklmno",
        };

        var message = AdminCommandHandler.BuildSpamHamRecordMessage(record, maxLength: 20);

        Assert.That(message, Is.EqualTo("#7 ham\na\n[truncated]"));
    }

    [Test]
    public void BuildDeletedSpamHamRecordMessage_IncludesDeletedRecordPreview()
    {
        var record = new SpamHamRecord
        {
            Id = 12,
            IsSpam = false,
            Text = "normal discussion",
        };

        var message = AdminCommandHandler.BuildDeletedSpamHamRecordMessage(record);

        Assert.That(message, Is.EqualTo("Запись #12 ham удалена, переобучение запланировано\nnormal discussion"));
    }

    [Test]
    public void BuildDeletedSpamHamRecordMessage_TruncatesPreview()
    {
        var record = new SpamHamRecord
        {
            Id = 12,
            IsSpam = true,
            Text = "abcdef",
        };

        var message = AdminCommandHandler.BuildDeletedSpamHamRecordMessage(record, maxPreviewLength: 3);

        Assert.That(message, Is.EqualTo("Запись #12 spam удалена, переобучение запланировано\nabc\n[truncated]"));
    }
}

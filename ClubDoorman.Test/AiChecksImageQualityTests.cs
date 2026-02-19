using Telegram.Bot.Types;
using tryAGI.OpenAI;

namespace ClubDoorman.Test;

public class AiChecksImageQualityTests
{
    [Test]
    public void SelectHighestQualityPhoto_PicksLargestByResolution()
    {
        var photos = new[]
        {
            new PhotoSize
            {
                FileId = "mid",
                FileUniqueId = "mid-unique",
                Width = 640,
                Height = 640,
                FileSize = 12_000,
            },
            new PhotoSize
            {
                FileId = "smallest_but_heavy",
                FileUniqueId = "smallest_but_heavy-unique",
                Width = 320,
                Height = 320,
                FileSize = 20_000,
            },
            new PhotoSize
            {
                FileId = "large",
                FileUniqueId = "large-unique",
                Width = 1280,
                Height = 1280,
                FileSize = 10_000,
            },
        };

        var selected = AiChecks.SelectHighestQualityPhoto(photos);

        Assert.That(selected.FileId, Is.EqualTo("large"));
    }

    [Test]
    public void SelectHighestQualityPhoto_FallsBackToLargestDimensionsWhenFileSizeMissing()
    {
        var photos = new[]
        {
            new PhotoSize
            {
                FileId = "first",
                FileUniqueId = "first-unique",
                Width = 200,
                Height = 400,
                FileSize = null,
            },
            new PhotoSize
            {
                FileId = "largest",
                FileUniqueId = "largest-unique",
                Width = 300,
                Height = 300,
                FileSize = null,
            },
            new PhotoSize
            {
                FileId = "second",
                FileUniqueId = "second-unique",
                Width = 250,
                Height = 300,
                FileSize = null,
            },
        };

        var selected = AiChecks.SelectHighestQualityPhoto(photos);

        Assert.That(selected.FileId, Is.EqualTo("largest"));
    }

    [Test]
    public void SelectHighestQualityPhoto_ThrowsOnEmptyCollection()
    {
        Assert.Throws<InvalidOperationException>(() => AiChecks.SelectHighestQualityPhoto(Array.Empty<PhotoSize>()));
    }

    [Test]
    public void CreateContextImageMessage_UsesLowDetail()
    {
        var message = AiChecks.CreateContextImageMessage([1, 2, 3, 4]);

        var detail = ExtractImageDetail(message);

        Assert.That(detail, Is.EqualTo(ChatCompletionRequestMessageContentPartImageImageUrlDetail.Low));
    }

    [Test]
    public void CreateSpamImageMessage_UsesHighDetail()
    {
        var message = AiChecks.CreateSpamImageMessage([1, 2, 3, 4]);

        var detail = ExtractImageDetail(message);

        Assert.That(detail, Is.EqualTo(ChatCompletionRequestMessageContentPartImageImageUrlDetail.High));
    }

    private static ChatCompletionRequestMessageContentPartImageImageUrlDetail? ExtractImageDetail(ChatCompletionRequestUserMessage message)
    {
        Assert.That(message.Content.IsValue2, Is.True, "Expected image message content parts.");
        Assert.That(message.Content.Value2, Has.Count.EqualTo(1), "Expected single image content part.");

        var part = message.Content.Value2.Single();
        Assert.That(part.IsImageUrl, Is.True, "Expected image content part.");
        return part.ImageUrl.ImageUrl.Detail;
    }
}

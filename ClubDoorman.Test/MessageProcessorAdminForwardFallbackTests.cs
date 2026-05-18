using Telegram.Bot.Types;

namespace ClubDoorman.Test;

public class MessageProcessorAdminForwardFallbackTests
{
    [Test]
    public void BuildAdminForwardFallbackMessage_UsesTextAsIs()
    {
        var message = new Message { Text = "spam text\nsecond line" };

        var fallback = MessageProcessor.BuildAdminForwardFallbackMessage(message);

        Assert.That(fallback, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fallback!.Kind, Is.EqualTo(AdminForwardFallbackMessageKind.Text));
            Assert.That(fallback.Text, Is.EqualTo("spam text\nsecond line"));
        }
    }

    [Test]
    public void BuildAdminForwardFallbackMessage_UsesLargestPhotoAndCaption()
    {
        var message = new Message
        {
            Caption = "photo caption",
            Photo =
            [
                new PhotoSize
                {
                    FileId = "small",
                    FileUniqueId = "small-unique",
                    Width = 90,
                    Height = 90,
                },
                new PhotoSize
                {
                    FileId = "large",
                    FileUniqueId = "large-unique",
                    Width = 1280,
                    Height = 720,
                },
            ],
        };

        var fallback = MessageProcessor.BuildAdminForwardFallbackMessage(message);

        Assert.That(fallback, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fallback!.Kind, Is.EqualTo(AdminForwardFallbackMessageKind.Photo));
            Assert.That(fallback.FileId, Is.EqualTo("large"));
            Assert.That(fallback.Caption, Is.EqualTo("photo caption"));
        }
    }

    [Test]
    public void BuildAdminForwardFallbackMessage_UsesVideoAndCaption()
    {
        var message = new Message
        {
            Caption = "video caption",
            Video = new Video { FileId = "video-file", FileUniqueId = "video-unique" },
        };

        var fallback = MessageProcessor.BuildAdminForwardFallbackMessage(message);

        Assert.That(fallback, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fallback!.Kind, Is.EqualTo(AdminForwardFallbackMessageKind.Video));
            Assert.That(fallback.FileId, Is.EqualTo("video-file"));
            Assert.That(fallback.Caption, Is.EqualTo("video caption"));
        }
    }
}

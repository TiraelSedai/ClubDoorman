using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test;

public class UtilsTextWithLinksTests
{
    [Test]
    public void HiddenLinkIsExpandedBeforeAnchorText()
    {
        var message = new Message
        {
            Text = "Каталог недвижимости Таиланда",
            Entities =
            [
                new MessageEntity
                {
                    Type = MessageEntityType.TextLink,
                    Offset = 0,
                    Length = 29,
                    Url = "https://t.me/AGENSTVO_TOPUP/7",
                },
            ],
        };

        Assert.That(Utils.TextWithLinks(message), Is.EqualTo("https://t.me/AGENSTVO_TOPUP/7 Каталог недвижимости Таиланда"));
    }

    [Test]
    public void MultipleHiddenLinksKeepTheirPositions()
    {
        var message = new Message
        {
            Caption = "купи тут или тут",
            CaptionEntities =
            [
                new MessageEntity
                {
                    Type = MessageEntityType.TextLink,
                    Offset = 5,
                    Length = 3,
                    Url = "https://a.example",
                },
                new MessageEntity
                {
                    Type = MessageEntityType.TextLink,
                    Offset = 13,
                    Length = 3,
                    Url = "https://b.example",
                },
            ],
        };

        Assert.That(Utils.TextWithLinks(message), Is.EqualTo("купи https://a.example тут или https://b.example тут"));
    }

    [Test]
    public void NonLinkEntitiesAreLeftAlone()
    {
        var message = new Message
        {
            Text = "жирный текст",
            Entities =
            [
                new MessageEntity
                {
                    Type = MessageEntityType.Bold,
                    Offset = 0,
                    Length = 6,
                },
            ],
        };

        Assert.That(Utils.TextWithLinks(message), Is.EqualTo("жирный текст"));
    }

    [Test]
    public void EmojiOffsetsAreSurrogateAware()
    {
        // 🔥 is two UTF-16 code units, and Telegram entity offsets count those, same as .NET string indices
        var message = new Message
        {
            Text = "🔥 жми",
            Entities =
            [
                new MessageEntity
                {
                    Type = MessageEntityType.TextLink,
                    Offset = 3,
                    Length = 3,
                    Url = "https://c.example",
                },
            ],
        };

        Assert.That(Utils.TextWithLinks(message), Is.EqualTo("🔥 https://c.example жми"));
    }

    [Test]
    public void MidWordAnchorIsNotGluedToThePrecedingWord()
    {
        var message = new Message
        {
            Text = "Каталог недвижимости",
            Entities =
            [
                new MessageEntity
                {
                    Type = MessageEntityType.TextLink,
                    Offset = 4,
                    Length = 3,
                    Url = "https://d.example",
                },
            ],
        };

        Assert.That(Utils.TextWithLinks(message), Is.EqualTo("Ката https://d.example лог недвижимости"));
    }

    [Test]
    public void MessageWithoutTextReturnsNull()
    {
        Assert.That(Utils.TextWithLinks(new Message()), Is.Null);
    }
}

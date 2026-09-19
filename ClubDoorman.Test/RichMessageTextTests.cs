using System.Text.Json;
using Telegram.Bot.Types;

namespace ClubDoorman.Test;

public sealed class RichMessageTextTests
{
    internal static Message BookReview() =>
        JsonSerializer.Deserialize<Message>(
            """
            {
              "message_id": 1, "date": 0, "chat": {"id": 1, "type": "private"},
              "rich_message": {"blocks": [
                {"type": "paragraph", "text": {"type": "bold", "text": "День опричника"}},
                {"type": "blockquote", "blocks": [
                  {"type": "paragraph", "text": "Первая строка\nВторая строка"}
                ]},
                {"type": "paragraph", "text": ["Читать ",
                  {"type": "url", "url": "https://example.org/book?a=1&b=2", "text": "книгу"},
                  " <целиком> & обсудить"]}
              ]}
            }
            """,
            Telegram.Bot.JsonBotAPI.Options
        )!;

    [Test]
    public void RichMessage_VisibleText_PreservesContentWithoutHiddenUrl()
    {
        Assert.That(
            Utils.VisibleText(BookReview()),
            Is.EqualTo("День опричника\nПервая строка\nВторая строка\nЧитать книгу <целиком> & обсудить")
        );
    }

    [Test]
    public void RichMessage_TextWithLinks_PreservesParagraphsQuotesAndHiddenUrl()
    {
        var message = BookReview();

        Assert.That(
            Utils.TextWithLinks(message),
            Is.EqualTo("День опричника\nПервая строка\nВторая строка\nЧитать https://example.org/book?a=1&b=2 книгу <целиком> & обсудить")
        );
    }
}

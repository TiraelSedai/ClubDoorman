using System.Text.Json;
using Telegram.Bot.Types;

namespace ClubDoorman.Test;

public sealed class PollMessageTextTests
{
    [TestCase("regular")]
    [TestCase("quiz")]
    public void Poll_QuestionAndOptionsReachBothTextReaders(string pollType)
    {
        var message = JsonSerializer.Deserialize<Message>(
            $$"""
            {
              "message_id": 1, "date": 0, "chat": {"id": 1, "type": "supergroup"},
              "poll": {
                "id": "123", "type": "{{pollType}}", "is_anonymous": true,
                "is_closed": false, "allows_multiple_answers": false, "total_voter_count": 1,
                "question": "https://t.me/+example\nзашли?",
                "options": [
                  {"text": "даа", "voter_count": 1},
                  {"text": "щаа https://example.org", "voter_count": 0}
                ]
              }
            }
            """,
            Telegram.Bot.JsonBotAPI.Options
        )!;
        const string expected = "[ опрос ] https://t.me/+example\nзашли?\n- даа\n- щаа https://example.org";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Utils.VisibleText(message), Is.EqualTo(expected));
            Assert.That(Utils.TextWithLinks(message), Is.EqualTo(expected));
        }
    }
}

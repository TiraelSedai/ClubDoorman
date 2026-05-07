using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test;

public class ReactionHandlerTests
{
    [Test]
    public void BuildReactionAutobanNotificationMessage_IncludesReasonUserChatAndPostLink()
    {
        var chat = new Chat
        {
            Id = -1001234567890,
            Title = "Paid Chat",
            Type = ChatType.Supergroup,
        };
        var user = new User
        {
            Id = 42,
            FirstName = "Spam",
            LastName = "User",
            Username = "spam_user",
        };

        var message = ReactionHandler.BuildReactionAutobanNotificationMessage(chat, user, 123);

        Assert.Multiple(() =>
        {
            Assert.That(message, Does.Contain("Авто-бан по реакции: пользователь из банлиста"));
            Assert.That(message, Does.Contain("Юзер Spam User @spam_user из чата Paid Chat"));
            Assert.That(message, Does.Contain("https://t.me/c/1234567890/123"));
        });
    }
}

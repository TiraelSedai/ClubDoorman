using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test;

/// <summary>
/// A free chat never gets a ban or a deletion out of the LLM, only these two texts: one for the chat, one for me.
/// </summary>
public class FreeChatWarningTests
{
    private static readonly Chat FreeChat = new()
    {
        Id = -1001234567890,
        Title = "Free Chat",
        Type = ChatType.Supergroup,
    };

    private static readonly User Spammer = new()
    {
        Id = 42,
        FirstName = "Spam",
        LastName = "User",
        Username = "spam_user",
    };

    [Test]
    public void Warning_KeepsTheReasonAndTheUpsell()
    {
        var warning = MessageProcessor.BuildFreeChatWarning("Сообщение с подозрением на спам. Обещает 200$ в день");

        Assert.That(
            warning,
            Is.EqualTo(
                $"Сообщение с подозрением на спам. Обещает 200$ в день{Environment.NewLine}Для более точного анализа переходите на платный тариф"
            )
        );
    }

    [Test]
    public void AdminReport_NamesTheReasonUserChatAndPostLink()
    {
        var report = MessageProcessor.BuildFreeChatAdminReport(
            FreeChat,
            Spammer,
            123,
            "Профиль с подозрением на эротику. Аватарка и био про OnlyFans"
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(report, Does.Contain("Сработала LLM-проверка free-чата"));
            Assert.That(report, Does.Contain("Профиль с подозрением на эротику. Аватарка и био про OnlyFans"));
            Assert.That(report, Does.Contain("Юзер Spam User @spam_user из чата Free Chat"));
            Assert.That(report, Does.Contain("https://t.me/c/1234567890/123"));
            Assert.That(report, Does.Not.Contain("платный тариф"), "the upsell is for the chat, not for the admin report");
        }
    }
}

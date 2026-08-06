using System.Globalization;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman;

internal static class Utils
{
    /// <summary>
    /// Message text with hidden hyperlink targets spliced in before their anchor text, so spam checks see the URL.
    /// </summary>
    public static string? TextWithLinks(Message message)
    {
        var text = message.Text ?? message.Caption;
        var entities = message.Text != null ? message.Entities : message.CaptionEntities;
        if (text == null || entities == null)
            return text;
        var result = new StringBuilder(text);
        foreach (var entity in entities.Where(e => e.Url is { Length: > 0 }).OrderByDescending(e => e.Offset))
        {
            if (entity.Offset < 0 || entity.Offset > result.Length)
                continue;
            // an anchor can start mid-word, and gluing the url onto the preceding letters makes a lookalike token
            var separator = entity.Offset > 0 && !char.IsWhiteSpace(result[entity.Offset - 1]) ? " " : "";
            result.Insert(entity.Offset, $"{separator}{entity.Url} ");
        }
        return result.ToString();
    }

    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";

    public static string FullName(User user) => FullName(user.FirstName, user.LastName);

    private static string LinkToSuperGroupMessage(Chat chat, long messageId) =>
        $"https://t.me/c/{chat.Id.ToString(CultureInfo.InvariantCulture)[4..]}/{messageId}";

    private static string LinkToGroupWithNameMessage(Chat chat, long messageId) => $"https://t.me/{chat.Username}/{messageId}";

    public static string LinkToMessage(Chat chat, long messageId) =>
        chat.Type == ChatType.Supergroup ? LinkToSuperGroupMessage(chat, messageId)
        : chat.Username == null ? ""
        : LinkToGroupWithNameMessage(chat, messageId);
}

using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman;

internal static class Utils
{
    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";

    public static string FullName(User user) => FullName(user.FirstName, user.LastName);

    private static string LinkToSuperGroupMessage(Chat chat, long messageId) => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}";

    private static string LinkToGroupWithNameMessage(Chat chat, long messageId) => $"https://t.me/{chat.Username}/{messageId}";

    public static string LinkToMessage(Chat chat, long messageId) =>
        chat.Type == ChatType.Supergroup ? Utils.LinkToSuperGroupMessage(chat, messageId)
        : chat.Username == null ? ""
        : Utils.LinkToGroupWithNameMessage(chat, messageId);
}

using System.Globalization;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman;

internal static class Utils
{
    public static async Task DeleteMessageLater(
        this ITelegramBotClient bot,
        Message message,
        TimeSpan after,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await Task.Delay(after, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        await bot.DeleteMessageSafe(message, logger, cancellationToken);
    }

    public static async Task DeleteMessageSafe(
        this ITelegramBotClient bot,
        Message message,
        ILogger logger,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Ephemeral messages have MessageId == 0 and live under their own id space
            if (message.EphemeralMessageId is { } ephemeralMessageId)
                await bot.DeleteEphemeralMessage(message.Chat.Id, message.ReceiverUser!.Id, ephemeralMessageId, cancellationToken);
            else
                await bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogWarning(
                e,
                "DeleteMessage, chat = {Chat}, messageId = {MessageId}, ephemeralMessageId = {EphemeralMessageId}",
                message.Chat.Id,
                message.MessageId,
                message.EphemeralMessageId
            );
        }
    }

    public static string? VisibleText(Message message) =>
        message.Text ?? message.Caption ?? (message.RichMessage is { } rich ? RichMessageText(rich, false) : null);

    private static string RichMessageText(RichMessage message, bool includeLinks)
    {
        // Let Telegram.Bot handle the rich block schema; AngleSharp decodes text and link attributes.
        using var document = new HtmlParser().ParseDocument(message.ToHtml());
        var result = new StringBuilder();
        Append(document.Body!);
        return result.ToString().Trim();

        void Append(INode node)
        {
            if (node is IText text)
            {
                result.Append(text.Data);
                return;
            }
            var tag = (node as IElement)?.LocalName;
            if (includeLinks && node is IElement { LocalName: "a" } link && link.GetAttribute("href") is { Length: > 0 } url)
            {
                if (result.Length > 0 && !char.IsWhiteSpace(result[^1]))
                    result.Append(' ');
                result.Append(url).Append(' ');
            }
            foreach (var child in node.ChildNodes)
                Append(child);
            if (tag is "br" or "td" or "th")
                result.Append(tag == "br" ? '\n' : ' ');
        }
    }

    /// <summary>
    /// Message text with hidden hyperlink targets spliced in before their anchor text, so spam checks see the URL.
    /// </summary>
    public static string? TextWithLinks(Message message)
    {
        if (message.Text == null && message.Caption == null && message.RichMessage is { } rich)
            return RichMessageText(rich, true);
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

    public static string FullName(string firstName, string? lastName) =>
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

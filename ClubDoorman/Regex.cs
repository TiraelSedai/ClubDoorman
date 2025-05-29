using System.Text.RegularExpressions;

partial class MyRegexes
{
    [GeneratedRegex(@"(?<!\w)@([a-zA-Z0-9_]{5,32})(?!\w)|(?<!\w)t\.me/([a-zA-Z0-9_]{5,32})(?!\w)", RegexOptions.IgnoreCase)]
    public static partial Regex TelegramUsername();
}

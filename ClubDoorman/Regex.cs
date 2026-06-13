using System.Text.RegularExpressions;

partial class MyRegexes
{
    [GeneratedRegex(@"(?<!\w)@([a-zA-Z0-9_]{5,32})(?!\w)|(?<!\w)t\.me/([a-zA-Z0-9_]{5,32})(?!\w)", RegexOptions.IgnoreCase)]
    public static partial Regex TelegramUsername();

    [GeneratedRegex(
        @"крипто.*приваток\s+в\s+одном\s+месте.*t\.me/\+",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant
    )]
    public static partial Regex CryptoPrivatkiBio();
}

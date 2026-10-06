using System.Text.RegularExpressions;

partial class MyRegexes
{
    [GeneratedRegex(@"(?<!\w)@([a-zA-Z0-9_]{5,32})(?!\w)|(?<!\w)t\.me/(?!joinchat/)([a-zA-Z0-9_]{5,32})(?!\w)", RegexOptions.IgnoreCase)]
    public static partial Regex TelegramUsername();

    [GeneratedRegex(
        @"крипто[\s-]*(?:приват|вип)(?:ки|ок)\b.*t\.me/\+",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant
    )]
    public static partial Regex CryptoPrivatkiBio();

    [GeneratedRegex(@"(?<!\w)(?:@|t\.me/)(?:MXBW28)(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    public static partial Regex BlacklistedBioMention();
}

namespace ClubDoorman;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static partial class TextProcessor
{
    public static string NormalizeText(string input)
    {
        var result = input.ReplaceLineEndings(" ");
        result = result.ToLowerInvariant();
        result = StripEmojisAndPunctuation(result);
        result = WhitespaceCompacter.Replace(result, " ");
        result = StripDiacritics(result);
        return result;
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled | RegexOptions.NonBacktracking | RegexOptions.CultureInvariant)]
    private static partial Regex MyWhitespaceRegex();

    [GeneratedRegex(
        @"[\p{Cs}\p{So}\p{Sk}\p{Sm}\p{Sc}\p{P}]",
        RegexOptions.Compiled | RegexOptions.NonBacktracking | RegexOptions.CultureInvariant
    )]
    private static partial Regex MyEmojiPunctuationRegex();

    private static readonly Regex NoEmojisAndPunctuation = MyEmojiPunctuationRegex();
    private static readonly Regex WhitespaceCompacter = MyWhitespaceRegex();

    private static string StripEmojisAndPunctuation(string input) => NoEmojisAndPunctuation.Replace(input, " ");

    private static string StripDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        if (
            !normalizedString.Any(x =>
                CharUnicodeInfo.GetUnicodeCategory(x) is UnicodeCategory.NonSpacingMark or UnicodeCategory.ModifierLetter
            )
        )
            return text;
        var stringBuilder = new StringBuilder(text.Length);
        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark && unicodeCategory != UnicodeCategory.ModifierLetter)
                stringBuilder.Append(c);
        }
        return stringBuilder.ToString();
    }
}

namespace ClubDoorman;

using System.Text.RegularExpressions;

public static partial class TextProcessor
{
    public static string NormalizeText(string input)
    {
        var result = input.ToLowerInvariant();
        result = FormatCharacters().Replace(result, "");
        result = UnwantedCharacters().Replace(result, " ");
        result = WhitespaceCompacting().Replace(result, " ");
        result = result.Trim();
        return result;
    }

    [GeneratedRegex(@"\p{Cf}", RegexOptions.NonBacktracking | RegexOptions.CultureInvariant)]
    private static partial Regex FormatCharacters();

    [GeneratedRegex(@"(?!@)\p{P}|\p{So}|\p{Cs}|[|`=]", RegexOptions.CultureInvariant)]
    private static partial Regex UnwantedCharacters();

    [GeneratedRegex(@"\s+", RegexOptions.NonBacktracking | RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceCompacting();
}

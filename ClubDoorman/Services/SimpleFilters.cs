namespace ClubDoorman.Services;

public static class SimpleFilters
{
    private static readonly string[] StopWords = File.ReadAllLines("data/stop-words.txt");

    public static bool HasStopWords(string message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return StopWords.Any(sw => message.Contains(sw, StringComparison.InvariantCultureIgnoreCase));
    }

    public static bool TooManyEmojis(string message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return message.Length > 20 && message.Where(IsEmoji).Count() >= 10;
    }

    private static bool IsEmoji(char character) => character is >= '\uD800' and <= '\uDFFF' or >= '\u2600' and <= '\u27BF';

    public static List<string> FindAllRussianWordsWithLookalikeSymbols(string message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return TextProcessor
            .NormalizeText(message)
            .Split(null)
            .Where(word => IsRussianWord(word) && word.Any(c => !IsCyrillicLowercase(c) && !AllowedNonRussianCyrillicOrDigit(c)))
            .ToList();
    }

    public static List<string> FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(string message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        return message
            .Split(null)
            .Where(word => IsRussianWord(word) && word.Any(c => !IsCyrillicLowercase(c) && !AllowedNonRussianCyrillicOrDigit(c)))
            .ToList();
    }

    private static bool IsRussianWord(string word)
    {
        if (word.Length < 3)
            return false;
        var cyrillicCount = word.Count(IsCyrillicLowercase);
        return cyrillicCount >= word.Length / 2;
    }

    private static bool AllowedNonRussianCyrillicOrDigit(char c) =>
        c == 'i' || c == 'і' || c == 'ћ' || c == 'є' || c == 'љ' || c == 'њ' || (c >= '0' && c <= '9');

    private static bool IsCyrillicLowercase(char c) => c is >= 'а' and <= 'я';
}

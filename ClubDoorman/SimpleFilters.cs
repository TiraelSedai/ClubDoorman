namespace ClubDoorman;

public static class SimpleFilters
{
    private static readonly string[] StopWords = File.ReadAllLines("data/stop-words.txt");

    public static bool HasStopWords(string message) =>
        StopWords.Any(sw => message.Contains(sw, StringComparison.InvariantCultureIgnoreCase));

    public static bool TooManyEmojis(string message) => message.Length > 20 && message.Where(IsEmoji).Count() >= 10;

    private static bool IsEmoji(char character) => character is >= '\uD800' and <= '\uDFFF' or >= '\u2600' and <= '\u27BF';

    public static List<string> FindAllRussianWordsWithLookalikeSymbols(string message) =>
        TextProcessor
            .NormalizeText(message)
            .Split(null)
            .Where(word => IsRussianWord(word) && word.Any(c => !IsCyrillicLowercase(c) && !AllowedNonCyrillic(c)))
            .ToList();

    public static List<string> FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(string message) =>
        message.Split(null).Where(word => IsRussianWord(word) && word.Any(c => !IsCyrillicLowercase(c) && !AllowedNonCyrillic(c))).ToList();

    private static bool IsRussianWord(string word)
    {
        if (word.Length < 3)
            return false;
        var cyrillicCount = word.Count(IsCyrillicLowercase);
        return cyrillicCount >= word.Length / 2;
    }

    private static bool AllowedNonCyrillic(char c) => c == 'i' || (c >= '0' && c <= '9');

    private static bool IsCyrillicLowercase(char c) => c is >= 'а' and <= 'я';
}

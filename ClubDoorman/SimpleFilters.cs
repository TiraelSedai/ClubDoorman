namespace ClubDoorman;

public static class SimpleFilters
{
    private static readonly string[] StopWords = File.ReadAllLines("data/stop-words.txt");

    public static bool HasStopWords(string message) =>
        StopWords.Any(sw => message.Contains(sw, StringComparison.InvariantCultureIgnoreCase));

    public static bool TooManyEmojis(string message)
    {
        var emojiCount = message.Where(IsEmoji).Count();
        if (emojiCount / message.Length >= 0.5)
            return true;
        if (message.Length > 20 && emojiCount > 10)
            return true;
        return false;
    }

    private static bool IsEmoji(char character) => character is >= '\uD800' and <= '\uDFFF' or >= '\u2600' and <= '\u27BF';

    public static List<string> FindAllRussianWordsWithLookalikeSymbols(string message) =>
        TextProcessor
            .NormalizeText(message)
            .Split(null)
            .Where(word => IsRussianWord(word) && word.Any(c => !IsCyrillicLowercase(c) && !AllowedNonRussianCyrillicOrDigit(c)))
            .ToList();

    public static List<string> FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(string message) =>
        message
            .Split(null)
            .Where(word => IsRussianWord(word) && word.Any(c => !IsCyrillicLowercase(c) && !AllowedNonRussianCyrillicOrDigit(c)))
            .ToList();

    private static bool IsRussianWord(string word)
    {
        if (word.Length < 3)
            return false;
        var cyrillicCount = word.Count(IsCyrillicLowercase);
        return cyrillicCount >= word.Length / 2;
    }

    public static bool HasUnwantedChars(string text)
    {
        foreach (var c in text)
        {
            if (
                (c >= 0x4E00 && c <= 0x9FFF)
                || // CJK Unified Ideographs
                (c >= 0x3400 && c <= 0x4DBF)
                || // CJK Extension A
                (c >= 0x20000 && c <= 0x2A6DF)
                || // CJK Extension B
                (c >= 0x2A700 && c <= 0x2B73F)
                || // CJK Extension C
                (c >= 0x2B740 && c <= 0x2B81F)
                || // CJK Extension D
                (c >= 0x2B820 && c <= 0x2CEAF)
                || // CJK Extension E
                (c >= 0x3040 && c <= 0x309F)
                || // Hiragana
                (c >= 0x30A0 && c <= 0x30FF)
                || // Katakana
                (c >= 0x31F0 && c <= 0x31FF)
                || // Katakana Phonetic Extensions
                (c >= 0xAC00 && c <= 0xD7AF)
                || // Hangul Syllables
                (c >= 0x1100 && c <= 0x11FF)
                || // Hangul Jamo
                (c >= 0x3130 && c <= 0x318F)
                || // Hangul Compatibility Jamo
                (c >= 0x0E00 && c <= 0x0E7F)
                || // Тайский
                (c >= 0x0600 && c <= 0x06FF)
                || // Arabic
                (c >= 0x0750 && c <= 0x077F)
                || // Arabic Supplement
                (c >= 0x08A0 && c <= 0x08FF)
                || // Arabic Extended-A
                (c >= 0xFB50 && c <= 0xFDFF)
                || // Arabic Presentation Forms-A
                (c >= 0xFE70 && c <= 0xFEFF)
                || // Arabic Presentation Forms-B
                (c >= 0x0590 && c <= 0x05FF)
                || // Иврит
                (c >= 0x0900 && c <= 0x097F)
                || // Devanagari
                (c >= 0x0980 && c <= 0x09FF)
                || // Bengali
                (c >= 0x0A00 && c <= 0x0A7F)
                || // Gurmukhi
                (c >= 0x0A80 && c <= 0x0AFF)
                || // Gujarati
                (c >= 0x0B00 && c <= 0x0B7F)
                || // Oriya
                (c >= 0x0B80 && c <= 0x0BFF)
                || // Tamil
                (c >= 0x0C00 && c <= 0x0C7F)
                || // Telugu
                (c >= 0x0C80 && c <= 0x0CFF)
                || // Kannada
                (c >= 0x0D00 && c <= 0x0D7F)
                || // Malayalam
                (c >= 0x1000 && c <= 0x109F)
                || // Myanmar
                (c >= 0x10A0 && c <= 0x10FF)
                || // Georgian
                (c >= 0x1200 && c <= 0x137F)
                || // Ethiopic
                (c >= 0x13A0 && c <= 0x13FF)
                || // Cherokee
                (c >= 0x1700 && c <= 0x171F)
                || // Tagalog
                (c >= 0x1720 && c <= 0x173F)
                || // Hanunoo
                (c >= 0x1740 && c <= 0x175F)
                || // Buhid
                (c >= 0x1760 && c <= 0x177F)
                || // Tagbanwa
                (c >= 0x1800 && c <= 0x18AF)
                || // Mongolian
                (c >= 0x1900 && c <= 0x194F)
                || // Limbu
                (c >= 0x1950 && c <= 0x197F)
                || // Tai Le
                (c >= 0x1980 && c <= 0x19DF)
                || // New Tai Lue
                (c >= 0x19E0 && c <= 0x19FF)
                || // Khmer Symbols
                (c >= 0x1A00 && c <= 0x1A1F)
                || // Buginese
                (c >= 0x1A20 && c <= 0x1AAF)
            ) // Tai Tham
            {
                return true;
            }
        }

        return false;
    }

    private static bool AllowedNonRussianCyrillicOrDigit(char c) =>
        c == 'ё' || c == 'i' || c == 'і' || c == 'ћ' || c == 'є' || c == 'љ' || c == 'њ' || c == 'ј' || (c >= '0' && c <= '9');

    private static bool IsCyrillicLowercase(char c) => c is >= 'а' and <= 'я';
}

using System.Globalization;

namespace ClubDoorman;

public static class SimpleFilters
{
    private static readonly string[] StopWords = File.ReadAllLines("data/stop-words.txt");
    private static readonly string[] HelloWordsStop = ["привет", "hi", "hello", "الو", "سلام"];

    public static bool HasOnlyHelloWord(string message) => HelloWordsStop.Any(sw => message == sw);

    public static bool HasStopWords(string message) =>
        StopWords.Any(sw => message.Contains(sw, StringComparison.InvariantCultureIgnoreCase));

    public static bool TooManyEmojis(string message)
    {
        var (emojis, total) = CountEmojis(message);
        if (emojis / total >= 0.04)
            return true;

        if (emojis > 4 && total < 150)
            return true;

        return false;
    }

    public static bool JustOneEmoji(string message)
    {
        if (message.Length > 4)
            return false;
        var (emojis, total) = CountEmojis(message);
        return total == 1 && emojis == total;
    }

    private static (int emoji, int total) CountEmojis(string text)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        var emoji = 0;
        var total = 0;
        while (enumerator.MoveNext())
        {
            total++;
            var element = enumerator.GetTextElement();
            if (IsEmojiTextElement(element))
                emoji++;
        }

        return (emoji, total);
    }

    private static bool IsEmojiTextElement(string textElement)
    {
        foreach (var rune in textElement.EnumerateRunes())
        {
            int value = rune.Value;

            // Comprehensive emoji ranges
            if (
                (value >= 0x1F600 && value <= 0x1F64F)
                || // Emoticons
                (value >= 0x1F300 && value <= 0x1F5FF)
                || // Misc Symbols and Pictographs
                (value >= 0x1F680 && value <= 0x1F6FF)
                || // Transport and Map
                (value >= 0x1F1E6 && value <= 0x1F1FF)
                || // Regional indicator symbols
                (value >= 0x2600 && value <= 0x27BF)
                || // Misc symbols
                (value >= 0x2700 && value <= 0x27BF)
                || // Dingbats
                (value >= 0x1F900 && value <= 0x1F9FF)
                || // Supplemental Symbols and Pictographs
                (value >= 0x1FA00 && value <= 0x1FA6F)
                || // Extended-A
                (value >= 0x1FA70 && value <= 0x1FAFF)
                || // Extended-B
                (value >= 0xFE00 && value <= 0xFE0F)
                || // Variation selectors
                (value >= 0x2300 && value <= 0x23FF)
                || // Misc Technical
                (value >= 0x2B50 && value <= 0x2B50)
                || // Star
                (value >= 0x200D && value <= 0x200D)
            ) // Zero width joiner
            {
                return true;
            }
        }

        return false;
    }

    private static readonly List<string> _usernameBlacklist =
    [
        "Аврора",
        "Алиночка",
        "Анечка",
        "Аглая",
        "Alina",
        "Варвара",
        "Василиса",
        "Vasilisa",
        "Кристина",
        "Регина",
        "Стася",
        "Стефания",
        "Юлия",
    ];

    public static bool InUsernameSuspiciousList(string name) => _usernameBlacklist.Contains(name);

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
        c == 'ё' || c == 'ë' || c == 'i' || c == 'і' || c == 'ћ' || c == 'є' || c == 'љ' || c == 'њ' || c == 'ј' || (c >= '0' && c <= '9');

    private static bool IsCyrillicLowercase(char c) => c is >= 'а' and <= 'я';
}

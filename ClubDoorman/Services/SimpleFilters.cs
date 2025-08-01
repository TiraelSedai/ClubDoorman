using System.Text.RegularExpressions;

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
    
    /// <summary>
    /// Проверяет наличие любых ссылок в тексте
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    /// <returns>true, если найдены ссылки</returns>
    public static bool HasLinks(string message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        
        // Проверяем наличие любых URL-адресов
        // Паттерн для http/https/ftp/www и других протоколов
        var urlPattern = @"(https?://|www\.|ftp://|t\.me/|telegram\.me/)[^\s]+";
        var hasUrls = Regex.IsMatch(message, urlPattern, RegexOptions.IgnoreCase);
        
        // Проверяем наличие HTML-гиперссылок
        // Формат: <a href="...">текст</a>
        var htmlLinkPattern = @"<a\s+href=""[^""]+"">[^<]+</a>";
        var hasHtmlLinks = Regex.IsMatch(message, htmlLinkPattern, RegexOptions.IgnoreCase);
        
        // Подробное логирование для отладки
        if (hasUrls)
        {
            var matches = Regex.Matches(message, urlPattern, RegexOptions.IgnoreCase);
            Console.WriteLine($"[DEBUG] HasLinks: найдены URL в тексте '{message}': {string.Join(", ", matches.Cast<Match>().Select(m => m.Value))}");
        }
        
        if (hasHtmlLinks)
        {
            var matches = Regex.Matches(message, htmlLinkPattern, RegexOptions.IgnoreCase);
            Console.WriteLine($"[DEBUG] HasLinks: найдены HTML-ссылки в тексте '{message}': {string.Join(", ", matches.Cast<Match>().Select(m => m.Value))}");
        }
        
        return hasUrls || hasHtmlLinks;
    }
    
    /// <summary>
    /// Проверяет, является ли сообщение банальным приветствием
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    /// <returns>true, если сообщение является банальным приветствием</returns>
    public static bool IsBoringGreeting(string message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        
        var normalizedMessage = message.Trim().ToLowerInvariant();
        
        // Убираем знаки препинания и лишние пробелы
        var cleanMessage = Regex.Replace(normalizedMessage, @"[^\w\s]", "").Trim();
        cleanMessage = Regex.Replace(cleanMessage, @"\s+", " ");
        
        // Банальные приветствия
        var boringGreetings = new[]
        {
            "привет", "приветик", "привки", "прив", "приффки",
            "хай", "хайло", "хэй", "хелло", "hello", "hi", "hey",
            "добро утро", "доброе утро", "утро", "утречко",
            "добрый день", "день добрый", "дня доброго",
            "добрый вечер", "вечер добрый", "вечера доброго",
            "добро пожаловать", "добропожаловать",
            "здарова", "здарово", "здравствуйте", "здравствуй",
            "ку", "кукус", "кукусики",
            "йо", "йоу", "yo",
            "салам", "салом", "салют",
            "дратути", "драсте", "драсти"
        };
        
        // Проверяем точные совпадения
        if (boringGreetings.Contains(cleanMessage))
            return true;
            
        // Проверяем если сообщение состоит только из одного-двух слов и одно из них - приветствие
        var words = cleanMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 2)
        {
            foreach (var word in words)
            {
                if (boringGreetings.Contains(word))
                    return true;
            }
        }
        
        // Проверяем только эмодзи (включая несколько подряд)
        var emojiOnlyPattern = @"^[\s\p{So}\p{Sk}\u200d\ufe0f\u2600-\u27bf\ud800-\udfff]+$";
        if (Regex.IsMatch(normalizedMessage, emojiOnlyPattern) && normalizedMessage.Trim().Length > 0)
        {
            // Если сообщение содержит только эмодзи и не слишком длинное
            if (normalizedMessage.Trim().Length <= 20)
                return true;
        }
        
        // Проверяем комбинации типа "привет 🙂" или "👋 привет"
        if (words.Length <= 3)
        {
            var hasGreeting = words.Any(word => boringGreetings.Contains(word));
            var hasEmoji = Regex.IsMatch(normalizedMessage, @"[\p{So}\p{Sk}\u2600-\u27bf\ud800-\udfff]");
            
            if (hasGreeting && hasEmoji)
                return true;
        }
        
        return false;
    }
}

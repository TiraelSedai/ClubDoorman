namespace ClubDoorman.Test.TestData;

/// <summary>
/// Тестовые данные для модерации сообщений
/// </summary>
public static class SampleMessages
{
    // Нормальные сообщения
    public static class Valid
    {
        public const string SimpleText = "Привет, как дела?";
        public const string WithEmoji = "Привет! 👋 Как дела? 😊";
        public const string LongText = "Это довольно длинное сообщение с обычным текстом, которое не должно вызывать подозрений у системы модерации.";
        public const string WithLinks = "Посмотрите на этот сайт: https://example.com";
        public const string WithMentions = "@username, что думаешь об этом?";
    }

    // Спам сообщения
    public static class Spam
    {
        public const string SimpleSpam = "КУПИТЕ НАШИ ТОВАРЫ СО СКИДКОЙ 90%!!!";
        public const string WithEmojis = "🔥🔥🔥 СРОЧНО КУПИТЕ 🔥🔥🔥 СКИДКА 99% 💰💰💰";
        public const string WithLinks = "http://spam-site.com - ЛУЧШИЕ ЦЕНЫ ВСЕГО ЗА 1 ДОЛЛАР!";
        public const string WithPhone = "Звоните прямо сейчас: +7-999-123-45-67";
        public const string WithCaps = "ВНИМАНИЕ! ЭТО ВАЖНОЕ СООБЩЕНИЕ! НЕ ПРОПУСТИТЕ!";
    }

    // Подозрительные сообщения
    public static class Suspicious
    {
        public const string TooManyEmojis = "Привет! 😀😃😄😁😆😅😂🤣😊😇🙂🙃😉😌😍🥰😘😗😙😚😋😛😝😜🤪🤨🧐🤓😎🤩🥳😏😒😞😔😟😕🙁☹️😣😖😫😩🥺😢😭😤😠😡🤬🤯😳🥵🥶😱😨😰😥😓🤗🤔🤭🤫🤥😶😐😑😯😦😧😮😲🥱😴🤤😪😵🤐🥴🤢🤮🤧😷🤒🤕";
        public const string WithLookalikeSymbols = "Рrіvеt, kаk dеlа? (использует латинские буквы)";
        public const string WithStopWords = "купить продать срочно дешево дорого";
        public const string MixedLanguages = "Hello привет bonjour 你好";
    }

    // Граничные случаи
    public static class EdgeCases
    {
        public const string Empty = "";
        public const string Whitespace = "   \t\n\r   ";
        public const string SingleChar = "a";
        public const string VeryLong = new string('a', 10000);
        public const string Null = null!;
        public const string WithUnicode = "Привет 🌍 世界 🚀";
        public const string WithSpecialChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?";
    }
} 
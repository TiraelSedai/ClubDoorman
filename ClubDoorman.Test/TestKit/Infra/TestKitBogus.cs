using Bogus;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.TestKit.Infra;

/// <summary>
/// Расширение TestKit с Bogus для генерации реалистичных тестовых данных
/// <tags>bogus, realistic-data, faker, test-data</tags>
/// </summary>
public static class TestKitBogus
{
    private static readonly Faker _faker = new Faker("ru");

    #region User Generators

    /// <summary>
    /// Создает реалистичного пользователя с Bogus
    /// <tags>bogus, user, realistic, faker</tags>
    /// </summary>
    public static User CreateRealisticUser(long? userId = null)
    {
        var userFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => userId ?? f.Random.Long(100000000, 999999999))
            .RuleFor(u => u.IsBot, false)
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.Username, (f, u) => f.Internet.UserName(u.FirstName, u.LastName))
            .RuleFor(u => u.LanguageCode, f => f.PickRandom("ru", "en", "es", "de"))
            .RuleFor(u => u.IsPremium, f => f.Random.Bool(0.1f)) // 10% премиум пользователей
            .RuleFor(u => u.AddedToAttachmentMenu, f => f.Random.Bool(0.05f)); // 5% с attachment menu

        return userFaker.Generate();
    }

    /// <summary>
    /// Создает бота с реалистичными данными
    /// <tags>bogus, bot, realistic, faker</tags>
    /// </summary>
    public static User CreateRealisticBot(long? botId = null)
    {
        var botFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => botId ?? f.Random.Long(100000000, 999999999))
            .RuleFor(u => u.IsBot, true)
            .RuleFor(u => u.FirstName, f => f.PickRandom("TestBot", "HelperBot", "ServiceBot", "AdminBot"))
            .RuleFor(u => u.Username, (f, u) => u.FirstName.ToLowerInvariant())
            .RuleFor(u => u.CanJoinGroups, f => f.Random.Bool(0.8f))
            .RuleFor(u => u.CanReadAllGroupMessages, f => f.Random.Bool(0.6f))
            .RuleFor(u => u.SupportsInlineQueries, f => f.Random.Bool(0.3f));

        return botFaker.Generate();
    }

    /// <summary>
    /// Создает подозрительного пользователя (потенциальный спаммер)
    /// <tags>bogus, suspicious-user, spammer, faker</tags>
    /// </summary>
    public static User CreateSuspiciousUser(long? userId = null)
    {
        var suspiciousFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => userId ?? f.Random.Long(100000000, 999999999))
            .RuleFor(u => u.IsBot, false)
            .RuleFor(u => u.FirstName, f => f.PickRandom(
                "🔥CRYPTO_EXPERT🔥", "💰MONEY_MAKER💰", "📈TRADER_PRO📈", 
                "Anna", "Maria", "Elena")) // Иногда нормальные имена
            .RuleFor(u => u.LastName, f => f.Random.Bool() ? f.Name.LastName() : null)
            .RuleFor(u => u.Username, (string?)null) // Часто без username
            .RuleFor(u => u.LanguageCode, f => f.PickRandom("en", "ru", null))
            .RuleFor(u => u.IsPremium, f => f.Random.Bool(0.05f)); // Реже премиум

        return suspiciousFaker.Generate();
    }

    #endregion

    #region Chat Generators

    /// <summary>
    /// Создает реалистичную группу с Bogus
    /// <tags>bogus, group, realistic, faker</tags>
    /// </summary>
    public static Chat CreateRealisticGroup(long? chatId = null)
    {
        var groupFaker = new Faker<Chat>()
            .RuleFor(c => c.Id, f => chatId ?? f.Random.Long(-1000000000000, -1000000000))
            .RuleFor(c => c.Type, ChatType.Group)
            .RuleFor(c => c.Title, f => f.Company.CompanyName())
            .RuleFor(c => c.Username, f => f.Random.Bool(0.3f) ? 
                f.Internet.UserName().ToLowerInvariant() : null); // 30% с username

        return groupFaker.Generate();
    }

    /// <summary>
    /// Создает реалистичный супергруппу
    /// <tags>bogus, supergroup, realistic, faker</tags>
    /// </summary>
    public static Chat CreateRealisticSupergroup(long? chatId = null)
    {
        var supergroupFaker = new Faker<Chat>()
            .RuleFor(c => c.Id, f => chatId ?? f.Random.Long(-1000000000000, -1000000000))
            .RuleFor(c => c.Type, ChatType.Supergroup)
            .RuleFor(c => c.Title, f => f.Company.CompanyName())
            .RuleFor(c => c.Username, f => f.Random.Bool(0.7f) ? 
                f.Internet.UserName().ToLowerInvariant() : null); // 70% с username

        return supergroupFaker.Generate();
    }

    /// <summary>
    /// Создает приватный чат
    /// <tags>bogus, private-chat, realistic, faker</tags>
    /// </summary>
    public static Chat CreateRealisticPrivateChat(long? chatId = null)
    {
        var privateFaker = new Faker<Chat>()
            .RuleFor(c => c.Id, f => chatId ?? f.Random.Long(100000000, 999999999))
            .RuleFor(c => c.Type, ChatType.Private)
            .RuleFor(c => c.FirstName, f => f.Name.FirstName())
            .RuleFor(c => c.LastName, f => f.Name.LastName())
            .RuleFor(c => c.Username, f => f.Random.Bool(0.6f) ? 
                f.Internet.UserName() : null); // 60% с username

        return privateFaker.Generate();
    }

    #endregion

    #region Message Generators

    /// <summary>
    /// Создает реалистичное сообщение
    /// <tags>bogus, message, realistic, faker</tags>
    /// </summary>
    public static Message CreateRealisticMessage(User? from = null, Chat? chat = null)
    {
        from ??= CreateRealisticUser();
        chat ??= CreateRealisticGroup();

        var messageFaker = new Faker<Message>()
            .RuleFor(m => m.From, from)
            .RuleFor(m => m.Chat, chat)
            .RuleFor(m => m.Date, f => f.Date.Recent(30)) // Последние 30 дней
            .RuleFor(m => m.Text, f => f.PickRandom(
                f.Lorem.Sentence(),
                "Привет всем!",
                "Как дела?",
                "Отличная статья!",
                "Согласен с автором",
                "+1",
                "👍",
                f.Lorem.Paragraph()));

        return messageFaker.Generate();
    }

    /// <summary>
    /// Создает спам-сообщение
    /// <tags>bogus, spam-message, realistic, faker</tags>
    /// </summary>
    public static Message CreateSpamMessage(User? from = null, Chat? chat = null)
    {
        from ??= CreateSuspiciousUser();
        chat ??= CreateRealisticGroup();

        var spamTexts = new[]
        {
            "🔥🔥🔥 СРОЧНО! ЗАРАБОТАЙ 1000000$ ЗА ДЕНЬ! 🔥🔥🔥 Переходи по ссылке https://scam.com",
            "💰💰💰 КРИПТОИНВЕСТИЦИИ! ПРИБЫЛЬ 500% В МЕСЯЦ! 💰💰💰",
            "🚀НОВАЯ МОНЕТА! КУПИ СЕЙЧАС! ЗАВТРА БУДЕТ ПОЗДНО!🚀",
            "❗️ВНИМАНИЕ❗️ Я ЗАРАБОТАЛ МИЛЛИОН ЗА НЕДЕЛЮ! УЗНАЙ КАК ➡️ bit.ly/scam",
            "🎁БЕСПЛАТНЫЕ ДЕНЬГИ! ЖМИ СЮДА!🎁"
        };

        var messageFaker = new Faker<Message>()
            .RuleFor(m => m.From, from)
            .RuleFor(m => m.Chat, chat)
            .RuleFor(m => m.Date, f => f.Date.Recent(1)) // Недавно
            .RuleFor(m => m.Text, f => f.PickRandom(spamTexts));

        return messageFaker.Generate();
    }

    /// <summary>
    /// Создает сообщение с медиа
    /// <tags>bogus, media-message, realistic, faker</tags>
    /// </summary>
    public static Message CreateMediaMessage(User? from = null, Chat? chat = null)
    {
        from ??= CreateRealisticUser();
        chat ??= CreateRealisticGroup();

        var message = CreateRealisticMessage(from, chat);
        
        // Добавляем случайный тип медиа
        var mediaType = _faker.PickRandom("photo", "video", "document", "sticker");
        switch (mediaType)
        {
            case "photo":
                message.Photo = new PhotoSize[]
                {
                    new PhotoSize { FileId = $"photo_{_faker.Random.AlphaNumeric(20)}", Width = 1280, Height = 720 }
                };
                break;
            case "video":
                message.Video = new Video { FileId = $"video_{_faker.Random.AlphaNumeric(20)}", Width = 1920, Height = 1080, Duration = 120 };
                break;
            case "document":
                message.Document = new Document { FileId = $"doc_{_faker.Random.AlphaNumeric(20)}", FileName = $"{_faker.System.FileName()}.pdf" };
                break;
            case "sticker":
                message.Sticker = new Sticker { FileId = $"sticker_{_faker.Random.AlphaNumeric(20)}", Width = 512, Height = 512 };
                break;
        }

        return message;
    }

    #endregion

    #region Collection Generators

    /// <summary>
    /// Создает список случайных пользователей
    /// <tags>bogus, users, collection, faker</tags>
    /// </summary>
    public static List<User> CreateUserList(int count = 5)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateRealisticUser())
            .ToList();
    }

    /// <summary>
    /// Создает историю сообщений для чата
    /// <tags>bogus, message-history, conversation, faker</tags>
    /// </summary>
    public static List<Message> CreateConversation(Chat chat, List<User> participants, int messageCount = 10)
    {
        var messages = new List<Message>();
        
        for (int i = 0; i < messageCount; i++)
        {
            var from = _faker.PickRandom(participants);
            var message = CreateRealisticMessage(from, chat);
            message.Date = DateTime.UtcNow.AddMinutes(-messageCount + i); // Хронологический порядок
            messages.Add(message);
        }

        return messages;
    }

    #endregion
    
    #region Backward Compatibility Methods
    
    /// <summary>
    /// Создает спам-сообщение с реалистичными паттернами (alias для CreateSpamMessage)
    /// <tags>bogus, spam-message, realistic, faker</tags>
    /// </summary>
    public static Message CreateRealisticSpamMessage(User? from = null, Chat? chat = null) => CreateSpamMessage(from, chat);
    
    /// <summary>
    /// Создает сообщение от бота
    /// <tags>bogus, bot-message, faker</tags>
    /// </summary>
    public static Message CreateBotMessage(User? from = null, Chat? chat = null)
    {
        var botUser = from ?? CreateRealisticBot();
        var messageChat = chat ?? CreateRealisticGroup();
        
        var messageFaker = new Faker<Message>()
            .RuleFor(m => m.MessageId, f => f.Random.Int(1, 10000))
            .RuleFor(m => m.Date, f => f.Date.Recent(7))
            .RuleFor(m => m.Chat, messageChat)
            .RuleFor(m => m.From, botUser)
            .RuleFor(m => m.Text, f => f.PickRandom("Bot message", "Service notification", "Automated response"))
            .RuleFor(m => m.IsAutomaticForward, false)
            .RuleFor(m => m.HasProtectedContent, false)
            .RuleFor(m => m.IsTopicMessage, false);

        return messageFaker.Generate();
    }
    
    /// <summary>
    /// Создает сообщение с командой /start
    /// <tags>bogus, start-command, faker</tags>
    /// </summary>
    public static Message CreateStartCommandMessage(User? from = null, Chat? chat = null)
    {
        var messageUser = from ?? CreateRealisticUser();
        var messageChat = chat ?? CreateRealisticGroup();
        
        var messageFaker = new Faker<Message>()
            .RuleFor(m => m.MessageId, f => f.Random.Int(1, 10000))
            .RuleFor(m => m.Date, f => f.Date.Recent(7))
            .RuleFor(m => m.Chat, messageChat)
            .RuleFor(m => m.From, messageUser)
            .RuleFor(m => m.Text, "/start")
            .RuleFor(m => m.IsAutomaticForward, false)
            .RuleFor(m => m.HasProtectedContent, false)
            .RuleFor(m => m.IsTopicMessage, false);

        return messageFaker.Generate();
    }
    
    /// <summary>
    /// Создает реалистичный канал
    /// <tags>bogus, channel, realistic, faker</tags>
    /// </summary>
    public static Chat CreateRealisticChannel(long? chatId = null)
    {
        var channelFaker = new Faker<Chat>()
            .RuleFor(c => c.Id, f => chatId ?? f.Random.Long(-1000000000000, -1000000000))
            .RuleFor(c => c.Type, ChatType.Channel)
            .RuleFor(c => c.Title, f => f.Company.CompanyName())
            .RuleFor(c => c.Username, f => f.Internet.UserName());

        return channelFaker.Generate();
    }
    
    /// <summary>
    /// Проверяет, содержит ли текст спам-паттерны
    /// <tags>bogus, spam-check, utility</tags>
    /// </summary>
    public static bool IsSpamText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        
        var spamEmojis = new[] { "🔥", "💰", "🎁", "⚡", "💎", "🚀", "📱", "❗️" };
        var spamWords = new[] { "внимание", "заработал", "миллион", "деньги", "быстро", "срочно", "бесплатно", "скам", "bit.ly" };
        
        var lowerText = text.ToLowerInvariant();
        
        return spamEmojis.Any(p => text.Contains(p)) || 
               spamWords.Any(word => lowerText.Contains(word));
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Получает базовый Faker для дополнительных генераций
    /// <tags>bogus, faker, utility, base</tags>
    /// </summary>
    public static Faker GetFaker() => _faker;

    /// <summary>
    /// Создает случайный текст на русском языке
    /// <tags>bogus, russian-text, faker, utility</tags>
    /// </summary>
    public static string CreateRussianText(int sentences = 1)
    {
        return _faker.Lorem.Sentences(sentences);
    }

    /// <summary>
    /// Создает случайный URL
    /// <tags>bogus, url, faker, utility</tags>
    /// </summary>
    public static string CreateRandomUrl()
    {
        return _faker.Internet.Url();
    }

    /// <summary>
    /// Создает случайную дату в диапазоне
    /// <tags>bogus, date, faker, utility</tags>
    /// </summary>
    public static DateTime CreateRandomDate(int daysBack = 30)
    {
        return _faker.Date.Recent(daysBack);
    }

    #endregion
} 
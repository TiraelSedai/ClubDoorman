using Bogus;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.TestKit;

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

    #endregion

    #region Chat Generators

    /// <summary>
    /// Создает реалистичную группу
    /// <tags>bogus, chat, group, realistic, faker</tags>
    /// </summary>
    public static Chat CreateRealisticGroup(long? chatId = null)
    {
        var groupFaker = new Faker<Chat>()
            .RuleFor(c => c.Id, f => chatId ?? f.Random.Long(-1000000000000, -100000000000))
            .RuleFor(c => c.Type, ChatType.Group)
            .RuleFor(c => c.Title, f => f.Company.CompanyName())
            .RuleFor(c => c.Username, f => f.Internet.UserName());
            // Дополнительные свойства не поддерживаются в Telegram.Bot.Types.Chat

        return groupFaker.Generate();
    }

    /// <summary>
    /// Создает реалистичную супергруппу
    /// <tags>bogus, chat, supergroup, realistic, faker</tags>
    /// </summary>
    public static Chat CreateRealisticSupergroup(long? chatId = null)
    {
        var supergroupFaker = new Faker<Chat>()
            .RuleFor(c => c.Id, f => chatId ?? f.Random.Long(-1000000000000, -100000000000))
            .RuleFor(c => c.Type, ChatType.Supergroup)
            .RuleFor(c => c.Title, f => f.Company.CompanyName())
            .RuleFor(c => c.Username, f => f.Internet.UserName());
            // Дополнительные свойства не поддерживаются в Telegram.Bot.Types.Chat

        return supergroupFaker.Generate();
    }

    /// <summary>
    /// Создает реалистичный канал
    /// <tags>bogus, chat, channel, realistic, faker</tags>
    /// </summary>
    public static Chat CreateRealisticChannel(long? chatId = null)
    {
        var channelFaker = new Faker<Chat>()
            .RuleFor(c => c.Id, f => chatId ?? f.Random.Long(-1000000000000, -100000000000))
            .RuleFor(c => c.Type, ChatType.Channel)
            .RuleFor(c => c.Title, f => f.Company.CompanyName())
            .RuleFor(c => c.Username, f => f.Internet.UserName());
            // Дополнительные свойства не поддерживаются в Telegram.Bot.Types.Chat

        return channelFaker.Generate();
    }

    #endregion

    #region Message Generators

    /// <summary>
    /// Создает реалистичное сообщение
    /// <tags>bogus, message, realistic, faker</tags>
    /// </summary>
    public static Message CreateRealisticMessage(User? from = null, Chat? chat = null, string? text = null)
    {
        var messageFaker = new Faker<Message>()
            .RuleFor(m => m.MessageId, f => f.Random.Int(1, 1000000))
            .RuleFor(m => m.Date, f => f.Date.Recent(7))
            .RuleFor(m => m.From, f => from ?? CreateRealisticUser())
            .RuleFor(m => m.Chat, f => chat ?? CreateRealisticGroup())
            .RuleFor(m => m.Text, f => text ?? f.Lorem.Sentence())
            .RuleFor(m => m.Entities, f => f.Random.Bool(0.3f) ? CreateRandomEntities() : null)
            .RuleFor(m => m.ReplyToMessage, f => f.Random.Bool(0.1f) ? CreateRealisticMessage() : null)
            .RuleFor(m => m.ForwardFrom, f => f.Random.Bool(0.05f) ? CreateRealisticUser() : null)
            .RuleFor(m => m.ViaBot, f => f.Random.Bool(0.02f) ? CreateRealisticBot() : null);

        return messageFaker.Generate();
    }

    /// <summary>
    /// Создает спам-сообщение с реалистичными паттернами
    /// <tags>bogus, message, spam, realistic, faker</tags>
    /// </summary>
    public static Message CreateRealisticSpamMessage(User? from = null, Chat? chat = null)
    {
        var spamTexts = new[]
        {
            "🔥 НЕВЕРОЯТНОЕ ПРЕДЛОЖЕНИЕ! 🔥",
            "💰 ЗАРАБОТАЙ 100000₽ ЗА ДЕНЬ! 💰",
            "🎁 БЕСПЛАТНЫЙ ПОДАРОК! НАЖМИ СЕЙЧАС! 🎁",
            "⚡ СРОЧНО! АКЦИЯ ТОЛЬКО СЕГОДНЯ! ⚡",
            "💎 ЭКСКЛЮЗИВНЫЙ ДОСТУП! 💎",
            "🚀 РАСКРУТИ СВОЙ БИЗНЕС! 🚀",
            "📱 СКАЧАЙ ПРИЛОЖЕНИЕ И ПОЛУЧИ БОНУС! 📱"
        };

        return CreateRealisticMessage(
            from: from,
            chat: chat,
            text: _faker.PickRandom(spamTexts)
        );
    }

    /// <summary>
    /// Создает сообщение с длинным текстом
    /// <tags>bogus, message, long-text, realistic, faker</tags>
    /// </summary>
    public static Message CreateLongMessage(User? from = null, Chat? chat = null)
    {
        return CreateRealisticMessage(
            from: from,
            chat: chat,
            text: _faker.Lorem.Paragraphs(3)
        );
    }

    #endregion

    #region Helper Methods

    private static MessageEntity[] CreateRandomEntities()
    {
        var entityTypes = new[] 
        { 
            MessageEntityType.Bold, 
            MessageEntityType.Italic, 
            MessageEntityType.Code, 
            MessageEntityType.Pre, 
            MessageEntityType.TextLink, 
            MessageEntityType.Mention, 
            MessageEntityType.Hashtag 
        };
        var entities = new List<MessageEntity>();
        var count = _faker.Random.Int(1, 3);
        for (int i = 0; i < count; i++)
        {
            entities.Add(new MessageEntity
            {
                Type = _faker.PickRandom(entityTypes),
                Offset = _faker.Random.Int(0, 50),
                Length = _faker.Random.Int(1, 20)
            });
        }
        return entities.ToArray();
    }

    /// <summary>
    /// Проверяет, содержит ли текст спам-паттерны
    /// <tags>bogus, message, spam, text-check</tags>
    /// </summary>
    public static bool IsSpamText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var spamPatterns = new[] { "🔥", "💰", "🎁", "⚡", "💎", "🚀", "📱" };
        return spamPatterns.Any(p => text.Contains(p));
    }

    #endregion
} 
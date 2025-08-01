using Bogus;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Test.TestKit.Infra;

namespace ClubDoorman.Test.TestKit;

/// <summary>
/// Расширение TestKit с Bogus для генерации реалистичных тестовых данных
/// <tags>bogus, realistic-data, faker, test-data</tags>
/// </summary>
public static class TestKitBogus
{
    #region User Generators

    /// <summary>
    /// Создает реалистичного пользователя с Bogus
    /// <tags>bogus, user, realistic, faker</tags>
    /// </summary>
    public static User CreateRealisticUser(long? userId = null) => Infra.TestKitBogus.CreateRealisticUser(userId);

    /// <summary>
    /// Создает бота с реалистичными данными
    /// <tags>bogus, bot, realistic, faker</tags>
    /// </summary>
    public static User CreateRealisticBot(long? botId = null) => Infra.TestKitBogus.CreateRealisticBot(botId);

    /// <summary>
    /// Создает подозрительного пользователя (потенциальный спаммер)
    /// <tags>bogus, suspicious-user, spammer, faker</tags>
    /// </summary>
    public static User CreateSuspiciousUser(long? userId = null) => Infra.TestKitBogus.CreateSuspiciousUser(userId);

    #endregion

    #region Chat Generators

    /// <summary>
    /// Создает реалистичную группу с Bogus
    /// <tags>bogus, group, realistic, faker</tags>
    /// </summary>
    public static Chat CreateRealisticGroup(long? chatId = null) => Infra.TestKitBogus.CreateRealisticGroup(chatId);

    /// <summary>
    /// Создает реалистичный супергруппу
    /// <tags>bogus, supergroup, realistic, faker</tags>
    /// </summary>
    public static Chat CreateRealisticSupergroup(long? chatId = null) => Infra.TestKitBogus.CreateRealisticSupergroup(chatId);

    /// <summary>
    /// Создает приватный чат
    /// <tags>bogus, private-chat, realistic, faker</tags>
    /// </summary>
    public static Chat CreateRealisticPrivateChat(long? chatId = null) => Infra.TestKitBogus.CreateRealisticPrivateChat(chatId);

    #endregion

    #region Message Generators

    /// <summary>
    /// Создает реалистичное сообщение
    /// <tags>bogus, message, realistic, faker</tags>
    /// </summary>
    public static Message CreateRealisticMessage(User? from = null, Chat? chat = null) => Infra.TestKitBogus.CreateRealisticMessage(from, chat);

    /// <summary>
    /// Создает спам-сообщение
    /// <tags>bogus, spam-message, realistic, faker</tags>
    /// </summary>
    public static Message CreateSpamMessage(User? from = null, Chat? chat = null) => Infra.TestKitBogus.CreateSpamMessage(from, chat);

    /// <summary>
    /// Создает сообщение с медиа
    /// <tags>bogus, media-message, realistic, faker</tags>
    /// </summary>
    public static Message CreateMediaMessage(User? from = null, Chat? chat = null) => Infra.TestKitBogus.CreateMediaMessage(from, chat);

    #endregion

    #region Collection Generators

    /// <summary>
    /// Создает список случайных пользователей
    /// <tags>bogus, users, collection, faker</tags>
    /// </summary>
    public static List<User> CreateUserList(int count = 5) => Infra.TestKitBogus.CreateUserList(count);

    /// <summary>
    /// Создает историю сообщений для чата
    /// <tags>bogus, message-history, conversation, faker</tags>
    /// </summary>
    public static List<Message> CreateConversation(Chat chat, List<User> participants, int messageCount = 10) => 
        Infra.TestKitBogus.CreateConversation(chat, participants, messageCount);

    #endregion

    #region Utility Methods

    /// <summary>
    /// Получает базовый Faker для дополнительных генераций
    /// <tags>bogus, faker, utility, base</tags>
    /// </summary>
    public static Faker GetFaker() => Infra.TestKitBogus.GetFaker();

    /// <summary>
    /// Создает случайный текст на русском языке
    /// <tags>bogus, russian-text, faker, utility</tags>
    /// </summary>
    public static string CreateRussianText(int sentences = 1) => Infra.TestKitBogus.CreateRussianText(sentences);

    /// <summary>
    /// Создает случайный URL
    /// <tags>bogus, url, faker, utility</tags>
    /// </summary>
    public static string CreateRandomUrl() => Infra.TestKitBogus.CreateRandomUrl();

    /// <summary>
    /// Создает случайную дату в диапазоне
    /// <tags>bogus, date, faker, utility</tags>
    /// </summary>
    public static DateTime CreateRandomDate(int daysBack = 30) => Infra.TestKitBogus.CreateRandomDate(daysBack);

    #endregion
    
    #region Backward Compatibility Methods
    
    /// <summary>
    /// Создает спам-сообщение с реалистичными паттернами (alias)
    /// <tags>bogus, spam-message, realistic, faker</tags>
    /// </summary>
    public static Message CreateRealisticSpamMessage(User? from = null, Chat? chat = null) => Infra.TestKitBogus.CreateRealisticSpamMessage(from, chat);
    
    /// <summary>
    /// Создает реалистичный канал
    /// <tags>bogus, channel, realistic, faker</tags>
    /// </summary>
    public static Chat CreateRealisticChannel(long? chatId = null) => Infra.TestKitBogus.CreateRealisticChannel(chatId);
    
    /// <summary>
    /// Проверяет, содержит ли текст спам-паттерны
    /// <tags>bogus, spam-check, utility</tags>
    /// </summary>
    public static bool IsSpamText(string? text) => Infra.TestKitBogus.IsSpamText(text);

    #endregion
} 
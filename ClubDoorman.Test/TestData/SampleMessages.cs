using Telegram.Bot.Types;

namespace ClubDoorman.TestData;

/// <summary>
/// Фабрика для создания тестовых сообщений и пользователей
/// </summary>
public static class SampleMessages
{
    public static Message CreateValidMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = 123456789, Type = Telegram.Bot.Types.Enums.ChatType.Group },
            From = CreateValidUser(),
            Text = "Привет! Это нормальное сообщение."
        };
    }

    public static Message CreateSpamMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = 123456789, Type = Telegram.Bot.Types.Enums.ChatType.Group },
            From = CreateValidUser(),
            Text = "КУПИТЕ НАШИ ТОВАРЫ ПО СУПЕР ЦЕНЕ!!! 🔥🔥🔥"
        };
    }

    public static Message CreateMimicryMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = 123456789, Type = Telegram.Bot.Types.Enums.ChatType.Group },
            From = CreateValidUser(),
            Text = "Привет всем! Как дела?"
        };
    }

    public static Message CreateBadMessage()
    {
        return new Message
        {
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = 123456789, Type = Telegram.Bot.Types.Enums.ChatType.Group },
            From = CreateValidUser(),
            Text = "Известное спам-сообщение"
        };
    }

    public static User CreateValidUser()
    {
        return new User
        {
            Id = 12345,
            IsBot = false,
            FirstName = "Иван",
            LastName = "Иванов",
            Username = "ivan_ivanov"
        };
    }

    public static User CreateInvalidUser()
    {
        return new User
        {
            Id = 67890,
            IsBot = false,
            FirstName = "ОченьДлинноеИмяПользователяКотороеПревышаетДопустимуюДлинуИДолжноБытьЗаблокированоПоПравиламМодерации",
            LastName = "ОченьДлиннаяФамилияПользователяКотороеПревышаетДопустимуюДлинуИДолжноБытьЗаблокированоПоПравиламМодерации",
            Username = "very_long_username_that_exceeds_maximum_length_and_should_be_banned_by_moderation_rules"
        };
    }
} 
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Mocks;

/// <summary>
/// Моки для Telegram Bot API
/// </summary>
public static class MockTelegram
{
    /// <summary>
    /// Создает мок ITelegramBotClient с базовой настройкой
    /// </summary>
    public static Mock<ITelegramBotClient> CreateMockBotClient()
    {
        var mock = new Mock<ITelegramBotClient>();
        
        // Базовые настройки для успешных операций
        mock.Setup(x => x.GetMeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 123456789, Username = "test_bot", FirstName = "Test Bot" });
            
        return mock;
    }

    /// <summary>
    /// Создает тестовое сообщение
    /// </summary>
    public static Message CreateTestMessage(
        string text = "Test message",
        long chatId = 123456789,
        long userId = 987654321,
        string username = "test_user",
        string firstName = "Test",
        string lastName = "User")
    {
        return new Message
        {
            MessageId = 1,
            Date = DateTime.UtcNow,
            Chat = new Chat { Id = chatId, Type = ChatType.Private },
            From = new User 
            { 
                Id = userId, 
                Username = username, 
                FirstName = firstName, 
                LastName = lastName 
            },
            Text = text,
            Type = MessageType.Text
        };
    }

    /// <summary>
    /// Создает тестовое обновление с сообщением
    /// </summary>
    public static Update CreateTestUpdate(Message? message = null)
    {
        return new Update
        {
            Id = 1,
            Message = message ?? CreateTestMessage(),
            Type = UpdateType.Message
        };
    }

    /// <summary>
    /// Создает тестового пользователя
    /// </summary>
    public static User CreateTestUser(
        long userId = 987654321,
        string username = "test_user",
        string firstName = "Test",
        string lastName = "User")
    {
        return new User
        {
            Id = userId,
            Username = username,
            FirstName = firstName,
            LastName = lastName
        };
    }

    /// <summary>
    /// Создает тестовый чат
    /// </summary>
    public static Chat CreateTestChat(
        long chatId = 123456789,
        ChatType chatType = ChatType.Private,
        string title = "Test Chat")
    {
        return new Chat
        {
            Id = chatId,
            Type = chatType,
            Title = title
        };
    }
} 
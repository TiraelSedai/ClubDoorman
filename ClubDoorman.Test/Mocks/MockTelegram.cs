using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Moq;

namespace ClubDoorman.Test.Mocks;

/// <summary>
/// Моки для Telegram Bot API
/// </summary>
public static class MockTelegram
{
    /// <summary>
    /// Создает мок ITelegramBotClient
    /// </summary>
    public static Mock<ITelegramBotClient> CreateMockBotClient()
    {
        var mock = new Mock<ITelegramBotClient>();
        
        // Настройка базовых методов
        mock.Setup(x => x.GetMe(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User 
            { 
                Id = 123456789, 
                IsBot = true, 
                FirstName = "TestBot",
                Username = "test_bot"
            });
            
        return mock;
    }

    /// <summary>
    /// Создает тестовое сообщение
    /// </summary>
    public static Message CreateTestMessage(string text = "Test message")
    {
        var user = CreateTestUser();
        var chat = CreateTestChat();
        
        // Создаем сообщение с базовыми свойствами
        var message = new Message
        {
            Date = DateTime.UtcNow,
            Chat = chat,
            From = user,
            Text = text
        };
        
        return message;
    }

    /// <summary>
    /// Создает тестового пользователя
    /// </summary>
    public static User CreateTestUser(string? username = "test_user", string? firstName = "Test", string? lastName = "User")
    {
        return new User
        {
            Id = 12345,
            IsBot = false,
            FirstName = firstName ?? "Test",
            LastName = lastName,
            Username = username
        };
    }

    /// <summary>
    /// Создает тестовый чат
    /// </summary>
    public static Chat CreateTestChat(string? title = "Test Chat", ChatType type = ChatType.Group)
    {
        return new Chat
        {
            Id = -1001234567890,
            Type = type,
            Title = title
        };
    }

    /// <summary>
    /// Создает тестовый Update
    /// </summary>
    public static Update CreateTestUpdate(Message? message = null)
    {
        return new Update
        {
            Message = message ?? CreateTestMessage()
        };
    }
} 
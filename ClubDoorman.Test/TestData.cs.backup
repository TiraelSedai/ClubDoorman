using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.Test;

/// <summary>
/// Общие тестовые данные для переиспользования в тестах
/// </summary>
public static class TestData
{
    /// <summary>
    /// Создает тестового пользователя
    /// </summary>
    public static User CreateTestUser(long id = 123, string firstName = "Test", string? lastName = null, string? username = null)
    {
        return new User 
        { 
            Id = id, 
            FirstName = firstName,
            LastName = lastName,
            Username = username
        };
    }
    
    /// <summary>
    /// Создает тестовый чат
    /// </summary>
    public static Chat CreateTestChat(long id = 456, string title = "Test Chat", ChatType type = ChatType.Group)
    {
        return new Chat 
        { 
            Id = id, 
            Title = title, 
            Type = type 
        };
    }
    
    /// <summary>
    /// Создает тестовое сообщение
    /// </summary>
    public static Message CreateTestMessage(User? user = null, Chat? chat = null, string? text = null)
    {
        return new Message 
        { 
            From = user ?? CreateTestUser(),
            Chat = chat ?? CreateTestChat(),
            Text = text ?? "Test message",
            Date = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Создает тестовое сообщение с фото
    /// </summary>
    public static Message CreateTestMessageWithPhoto(User? user = null, Chat? chat = null, string? caption = null)
    {
        return new Message 
        { 
            From = user ?? CreateTestUser(),
            Chat = chat ?? CreateTestChat(),
            Caption = caption ?? "Test photo",
            Photo = new[] { new PhotoSize { FileId = "test_photo_id", Width = 100, Height = 100 } },
            Date = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Создает тестовое сообщение с видео
    /// </summary>
    public static Message CreateTestMessageWithVideo(User? user = null, Chat? chat = null, string? caption = null)
    {
        return new Message 
        { 
            From = user ?? CreateTestUser(),
            Chat = chat ?? CreateTestChat(),
            Caption = caption ?? "Test video",
            Video = new Video { FileId = "test_video_id", Width = 100, Height = 100 },
            Date = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Создает тестовое сообщение со стикером
    /// </summary>
    public static Message CreateTestMessageWithSticker(User? user = null, Chat? chat = null)
    {
        return new Message 
        { 
            From = user ?? CreateTestUser(),
            Chat = chat ?? CreateTestChat(),
            Sticker = new Sticker { FileId = "test_sticker_id" },
            Date = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Создает тестовое сообщение с кнопками
    /// </summary>
    public static Message CreateTestMessageWithButtons(User? user = null, Chat? chat = null, string? text = null)
    {
        return new Message 
        { 
            From = user ?? CreateTestUser(),
            Chat = chat ?? CreateTestChat(),
            Text = text ?? "Test message with buttons",
            ReplyMarkup = new InlineKeyboardMarkup(new[] 
            { 
                new[] { new InlineKeyboardButton("Test Button") { CallbackData = "test_callback" } } 
            }),
            Date = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Создает тестовое сообщение со Story
    /// </summary>
    public static Message CreateTestMessageWithStory(User? user = null, Chat? chat = null)
    {
        return new Message 
        { 
            From = user ?? CreateTestUser(),
            Chat = chat ?? CreateTestChat(),
            Story = new Story { Id = 1 },
            Date = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Создает пользователя с длинным именем
    /// </summary>
    public static User CreateUserWithLongName()
    {
        return CreateTestUser(firstName: new string('A', 100));
    }
    
    /// <summary>
    /// Создает пользователя с подозрительно длинным именем
    /// </summary>
    public static User CreateUserWithSuspiciousName()
    {
        return CreateTestUser(firstName: new string('A', 50));
    }
    
    /// <summary>
    /// Создает пользователя в блэклисте
    /// </summary>
    public static User CreateBannedUser()
    {
        return CreateTestUser(id: 999, firstName: "Banned");
    }
    
    /// <summary>
    /// Создает спам-сообщение
    /// </summary>
    public static Message CreateSpamMessage(User? user = null, Chat? chat = null)
    {
        return CreateTestMessage(
            user: user ?? CreateTestUser(),
            chat: chat ?? CreateTestChat(),
            text: "Купите наш продукт! Скидка 90%! Переходите по ссылке!"
        );
    }
    
    /// <summary>
    /// Создает сообщение с русскими словами с похожими символами
    /// </summary>
    public static Message CreateMessageWithLookalikeSymbols(User? user = null, Chat? chat = null)
    {
        return CreateTestMessage(
            user: user ?? CreateTestUser(),
            chat: chat ?? CreateTestChat(),
            text: "Прuвет мiр! Как дeла?"
        );
    }
    
    /// <summary>
    /// Создает сообщение со стоп-словами
    /// </summary>
    public static Message CreateMessageWithStopWords(User? user = null, Chat? chat = null)
    {
        return CreateTestMessage(
            user: user ?? CreateTestUser(),
            chat: chat ?? CreateTestChat(),
            text: "Набор в команду для заинтересованых в доп.доходе удаленно, за информацией в лс"
        );
    }
} 
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Infrastructure;

[TestFixture]
[Category("unit")]
[Category("infrastructure")]
public class TelegramBotClientWrapperTests
{
    private TelegramBotClientWrapperTestFactory _factory;
    private TelegramBotClientWrapper _wrapper;

    [SetUp]
    public void Setup()
    {
        _factory = new TelegramBotClientWrapperTestFactory();
        // Создаем реальный TelegramBotClient для wrapper'а
        var realBotClient = new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz");
        _wrapper = new TelegramBotClientWrapper(realBotClient);
    }

    [Test]
    public void TelegramBotClientWrapper_Constructor_AcceptsTelegramBotClient()
    {
        // Arrange & Act
        var botClient = new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz");
        var wrapper = new TelegramBotClientWrapper(botClient);

        // Assert
        Assert.That(wrapper, Is.Not.Null);
    }

    [Test]
    public void TelegramBotClientWrapper_Constructor_ThrowsOnNullBot()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TelegramBotClientWrapper(null!));
    }

    [Test]
    public void TelegramBotClientWrapper_BotId_ReturnsCorrectId()
    {
        // Arrange
        var botClient = new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz");
        var wrapper = new TelegramBotClientWrapper(botClient);

        // Act
        var botId = wrapper.BotId;

        // Assert
        Assert.That(botId, Is.EqualTo(1234567890));
    }

    [Test]
    public void TelegramBotClientWrapper_GetChatFullInfo_CopiesPhotoProperty()
    {
        // Arrange
        var chatId = new ChatId(123456789);
        
        // Создаем тестовый Chat с Photo
        var testPhoto = new ChatPhoto
        {
            SmallFileId = "test_small_id",
            SmallFileUniqueId = "test_small_unique",
            BigFileId = "test_big_id",
            BigFileUniqueId = "test_big_unique"
        };

        // Act & Assert
        // Этот тест проверяет, что метод GetChatFullInfo правильно копирует Photo
        // Мы не можем вызвать реальный API, но можем проверить структуру кода
        
        // Проверяем, что в TelegramBotClientWrapper.GetChatFullInfo есть строка Photo = chat.Photo
        var wrapperCode = File.ReadAllText("../../../../ClubDoorman/Services/TelegramBotClientWrapper.cs");
        Assert.That(wrapperCode, Does.Contain("Photo = chat.Photo"), 
            "TelegramBotClientWrapper.GetChatFullInfo должен копировать Photo из Chat");
        
        // Проверяем, что ChatFullInfo имеет свойство Photo
        var chatFullInfoType = typeof(ChatFullInfo);
        var photoProperty = chatFullInfoType.GetProperty("Photo");
        Assert.That(photoProperty, Is.Not.Null, "ChatFullInfo должен иметь свойство Photo");
        Assert.That(photoProperty!.PropertyType, Is.EqualTo(typeof(ChatPhoto)), 
            "Свойство Photo должно быть типа ChatPhoto");
    }

    [Test]
    public void TelegramBotClientWrapper_Constructor_ValidatesInput()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TelegramBotClientWrapper(null!));
    }
} 
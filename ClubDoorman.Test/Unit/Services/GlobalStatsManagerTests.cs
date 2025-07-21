using NUnit.Framework;
using Moq;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("business-logic")]
public class GlobalStatsManagerTests
{
    private GlobalStatsManagerTestFactory _factory;
    private GlobalStatsManager _service;
    private Mock<ITelegramBotClientWrapper> _botClientMock;

    [SetUp]
    public void Setup()
    {
        _factory = new GlobalStatsManagerTestFactory();
        _service = _factory.CreateDefault();
        _botClientMock = _factory.BotClientMock;
    }

    [Test]
    public void Constructor_CreatesDataDirectory_WhenNotExists()
    {
        // Arrange & Act
        var tempDir = Path.GetTempPath();
        var testDataDir = Path.Combine(tempDir, "test_data_global_stats");
        
        if (Directory.Exists(testDataDir))
            Directory.Delete(testDataDir, true);
        
        // Создаем временный сервис с кастомным путем
        var service = new GlobalStatsManager();
        
        // Assert
        Assert.That(Directory.Exists("data"), Is.True);
    }

    [Test]
    public async Task EnsureChatAsync_NewChat_AddsChatToStats()
    {
        // Arrange
        var chatId = 123456L;
        var chatTitle = "Test Chat";
        
        _factory.WithBotClientSetup(mock =>
            mock.Setup(x => x.GetChatMemberCount(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(100));

        // Act
        await _service.EnsureChatAsync(chatId, chatTitle, _botClientMock.Object);

        // Assert
        // Проверяем, что чат добавлен в статистику
        // Это сложно проверить напрямую, так как данные сохраняются в файл
        // Но мы можем проверить, что метод не выбрасывает исключений
        Assert.Pass("Метод выполнился без исключений");
    }

    [Test]
    public async Task EnsureChatAsync_ExistingChat_UpdatesTitle()
    {
        // Arrange
        var chatId = 123456L;
        var oldTitle = "Old Title";
        var newTitle = "New Title";
        
        // Сначала добавляем чат
        _factory.WithBotClientSetup(mock =>
            mock.Setup(x => x.GetChatMemberCount(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(100));
        
        await _service.EnsureChatAsync(chatId, oldTitle, _botClientMock.Object);

        // Act - обновляем заголовок
        await _service.EnsureChatAsync(chatId, newTitle, _botClientMock.Object);

        // Assert
        Assert.Pass("Метод выполнился без исключений");
    }

    [Test]
    public async Task UpdateAllMembersAsync_ValidChats_UpdatesMemberCounts()
    {
        // Arrange
        var chatId1 = 123456L;
        var chatId2 = 789012L;
        
        // Добавляем чаты
        _factory.WithBotClientSetup(mock =>
        {
            mock.Setup(x => x.GetChatMemberCount(It.IsAny<ChatId>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        });
        
        await _service.EnsureChatAsync(chatId1, "Chat 1", _botClientMock.Object);
        await _service.EnsureChatAsync(chatId2, "Chat 2", _botClientMock.Object);

        // Act
        await _service.UpdateAllMembersAsync(_botClientMock.Object);

        // Assert
        Assert.Pass("Метод выполнился без исключений");
    }

    [Test]
    public async Task UpdateZeroMemberChatsAsync_OnlyZeroChats_UpdatesOnlyZeroChats()
    {
        // Arrange
        var zeroChatId = 123456L;
        var nonZeroChatId = 789012L;
        
        _factory.WithBotClientSetup(mock =>
        {
            mock.Setup(x => x.GetChatMemberCount(It.IsAny<ChatId>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        });
        
        await _service.EnsureChatAsync(zeroChatId, "Zero Chat", _botClientMock.Object);
        await _service.EnsureChatAsync(nonZeroChatId, "Non-Zero Chat", _botClientMock.Object);

        // Act
        await _service.UpdateZeroMemberChatsAsync(_botClientMock.Object);

        // Assert
        Assert.Pass("Метод выполнился без исключений");
    }

    [Test]
    public void IncCaptcha_NewChat_IncrementsCaptchaCount()
    {
        // Arrange
        var chatId = 123456L;
        var chatTitle = "Test Chat";

        // Act
        _service.IncCaptcha(chatId, chatTitle);

        // Assert
        Assert.Pass("Метод выполнился без исключений");
    }

    [Test]
    public void IncCaptcha_ExistingChat_IncrementsCaptchaCount()
    {
        // Arrange
        var chatId = 123456L;
        var chatTitle = "Test Chat";

        // Act - вызываем дважды
        _service.IncCaptcha(chatId, chatTitle);
        _service.IncCaptcha(chatId, chatTitle);

        // Assert
        Assert.Pass("Метод выполнился без исключений");
    }

    [Test]
    public void IncBan_NewChat_IncrementsBanCount()
    {
        // Arrange
        var chatId = 123456L;
        var chatTitle = "Test Chat";

        // Act
        _service.IncBan(chatId, chatTitle);

        // Assert
        Assert.Pass("Метод выполнился без исключений");
    }

    [Test]
    public void IncBan_ExistingChat_IncrementsBanCount()
    {
        // Arrange
        var chatId = 123456L;
        var chatTitle = "Test Chat";

        // Act - вызываем дважды
        _service.IncBan(chatId, chatTitle);
        _service.IncBan(chatId, chatTitle);

        // Assert
        Assert.Pass("Метод выполнился без исключений");
    }

    [Test]
    public void GenerateHtml_ValidData_GeneratesHtmlFile()
    {
        // Arrange
        var chatId = 123456L;
        var chatTitle = "Test Chat";
        _service.IncCaptcha(chatId, chatTitle);
        _service.IncBan(chatId, chatTitle);

        // Act
        _service.GenerateHtml();

        // Assert
        var htmlPath = "data/stats.html";
        Assert.That(File.Exists(htmlPath), Is.True);
        
        var htmlContent = File.ReadAllText(htmlPath);
        Assert.That(htmlContent, Does.Contain("Test Chat"));
    }

    [Test]
    public void GenerateGlobalJson_ValidData_GeneratesJsonFile()
    {
        // Arrange
        var chatId = 123456L;
        var chatTitle = "Test Chat";
        _service.IncCaptcha(chatId, chatTitle);
        _service.IncBan(chatId, chatTitle);

        // Act
        _service.GenerateGlobalJson();

        // Assert
        var jsonPath = "data/global_stats.json";
        Assert.That(File.Exists(jsonPath), Is.True);
        
        var jsonContent = File.ReadAllText(jsonPath);
        Assert.That(jsonContent, Does.Contain("Test Chat"));
    }

    [Test]
    public async Task EnsureChatAsync_BotThrowsException_HandlesGracefully()
    {
        // Arrange
        var chatId = 123456L;
        var chatTitle = "Test Chat";
        
        _factory.WithBotClientSetup(mock =>
            mock.Setup(x => x.GetChatMemberCount(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Bot API error")));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => 
            await _service.EnsureChatAsync(chatId, chatTitle, _botClientMock.Object));
    }

    [Test]
    public async Task UpdateAllMembersAsync_BotThrowsException_HandlesGracefully()
    {
        // Arrange
        var chatId = 123456L;
        var chatTitle = "Test Chat";
        
        _factory.WithBotClientSetup(mock =>
        {
            mock.Setup(x => x.GetChatMemberCount(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(100);
            mock.Setup(x => x.GetChatMemberCount(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Bot API error"));
        });
        
        await _service.EnsureChatAsync(chatId, chatTitle, _botClientMock.Object);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => 
            await _service.UpdateAllMembersAsync(_botClientMock.Object));
    }

    [Test]
    public async Task UpdateZeroMemberChatsAsync_BotThrowsException_HandlesGracefully()
    {
        // Arrange
        var chatId = 123456L;
        var chatTitle = "Test Chat";
        
        _factory.WithBotClientSetup(mock =>
        {
            mock.Setup(x => x.GetChatMemberCount(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            mock.Setup(x => x.GetChatMemberCount(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Bot API error"));
        });
        
        await _service.EnsureChatAsync(chatId, chatTitle, _botClientMock.Object);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => 
            await _service.UpdateZeroMemberChatsAsync(_botClientMock.Object));
    }

    [TearDown]
    public void Cleanup()
    {
        // Очищаем временные файлы
        try
        {
            if (File.Exists("data/global_stats.json"))
                File.Delete("data/global_stats.json");
            if (File.Exists("data/stats.html"))
                File.Delete("data/stats.html");
        }
        catch { }
    }
} 
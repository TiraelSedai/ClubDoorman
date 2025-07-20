using System.Reflection;
using NUnit.Framework;
using Telegram.Bot.Types;
using ClubDoorman.Services;

namespace ClubDoorman.Test.Unit.Infrastructure;

/// <summary>
/// Тесты для метода GetChatLink в классе Worker
/// Тестируем текущую реализацию перед рефакторингом
/// </summary>
[TestFixture]
[Category("unit")]
[Category("infrastructure")]
public class WorkerGetChatLinkTests
{
    private MethodInfo _getChatLinkMethod;
    private MethodInfo _getChatLinkWithIdMethod;
    private IChatLinkFormatter _chatLinkFormatter;

    [OneTimeSetUp]
    public void SetUp()
    {
        // Создаем экземпляр новой реализации для тестирования
        _chatLinkFormatter = new ChatLinkFormatter();
    }

    [Test]
    public void GetChatLink_PublicGroupWithUsername_ReturnsMarkdownLink()
    {
        // Arrange
        var chat = new Chat { Id = 123456789, Username = "testgroup", Title = "Test Group" };
        
        // Act
        var result = _chatLinkFormatter.GetChatLink(chat);
        
        // Assert
        Assert.That(result, Is.EqualTo("[Test Group](https://t.me/testgroup)"));
    }

    [Test]
    public void GetChatLink_SupergroupWithoutUsername_ReturnsTelegramLink()
    {
        // Arrange
        var chat = new Chat { Id = -100123456789, Title = "Super Group" };
        
        // Act
        var result = _chatLinkFormatter.GetChatLink(chat);
        
        // Assert
        Assert.That(result, Is.EqualTo("[Super Group](https://t.me/c/123456789)"));
    }

    [Test]
    public void GetChatLink_RegularGroupWithoutUsername_ReturnsBoldTitle()
    {
        // Arrange
        var chat = new Chat { Id = -123456789, Title = "Regular Group" };
        
        // Act
        var result = _chatLinkFormatter.GetChatLink(chat);
        
        // Assert
        Assert.That(result, Is.EqualTo("*Regular Group*"));
    }

    [Test]
    public void GetChatLink_ChannelWithoutUsername_ReturnsBoldTitle()
    {
        // Arrange
        var chat = new Chat { Id = 123456789, Title = "Test Channel" };
        
        // Act
        var result = _chatLinkFormatter.GetChatLink(chat);
        
        // Assert
        Assert.That(result, Is.EqualTo("*Test Channel*"));
    }

    [Test]
    public void GetChatLink_NullTitle_UsesDefaultTitle()
    {
        // Arrange
        var chat = new Chat { Id = 123456789, Username = "testgroup", Title = null };
        
        // Act
        var result = _chatLinkFormatter.GetChatLink(chat);
        
        // Assert
        Assert.That(result, Is.EqualTo("[Неизвестный чат](https://t.me/testgroup)"));
    }

    [Test]
    public void GetChatLink_EmptyTitle_ReturnsEmptyBrackets()
    {
        // Arrange
        var chat = new Chat { Id = 123456789, Username = "testgroup", Title = "" };
        
        // Act
        var result = _chatLinkFormatter.GetChatLink(chat);
        
        // Assert
        Assert.That(result, Is.EqualTo("[](https://t.me/testgroup)"));
    }

    [Test]
    public void GetChatLink_TitleWithMarkdownSymbols_EscapesCorrectly()
    {
        // Arrange
        var chat = new Chat { Id = 123456789, Username = "testgroup", Title = "Test *Bold* _Italic_ [Link]" };
        
        // Act
        var result = _chatLinkFormatter.GetChatLink(chat);
        
        // Assert
        Assert.That(result, Is.EqualTo("[Test \\*Bold\\* \\_Italic\\_ \\[Link\\]](https://t.me/testgroup)"));
    }

    // Тесты для перегрузки с chatId и chatTitle
    [Test]
    public void GetChatLink_WithChatIdAndTitle_UsernameStartsWithAt_ReturnsMarkdownLink()
    {
        // Arrange
        var chatId = 123456789L;
        var chatTitle = "@testgroup";
        
        // Act
        var result = _chatLinkFormatter.GetChatLink(chatId, chatTitle);
        
        // Assert
        Assert.That(result, Is.EqualTo("[@testgroup](https://t.me/testgroup)"));
    }

    [Test]
    public void GetChatLink_WithChatIdAndTitle_Supergroup_ReturnsTelegramLink()
    {
        // Arrange
        var chatId = -100123456789L;
        var chatTitle = "Super Group";
        
        // Act
        var result = _chatLinkFormatter.GetChatLink(chatId, chatTitle);
        
        // Assert
        Assert.That(result, Is.EqualTo("[Super Group](https://t.me/c/123456789)"));
    }

    [Test]
    public void GetChatLink_WithChatIdAndTitle_RegularGroup_ReturnsBoldTitle()
    {
        // Arrange
        var chatId = -123456789L;
        var chatTitle = "Regular Group";
        
        // Act
        var result = _chatLinkFormatter.GetChatLink(chatId, chatTitle);
        
        // Assert
        Assert.That(result, Is.EqualTo("*Regular Group*"));
    }

    [Test]
    public void GetChatLink_WithChatIdAndTitle_Channel_ReturnsBoldTitle()
    {
        // Arrange
        var chatId = 123456789L;
        var chatTitle = "Test Channel";
        
        // Act
        var result = _chatLinkFormatter.GetChatLink(chatId, chatTitle);
        
        // Assert
        Assert.That(result, Is.EqualTo("*Test Channel*"));
    }

    [Test]
    public void GetChatLink_WithChatIdAndTitle_NullTitle_UsesDefaultTitle()
    {
        // Arrange
        var chatId = 123456789L;
        string? chatTitle = null;
        
        // Act
        var result = _chatLinkFormatter.GetChatLink(chatId, chatTitle);
        
        // Assert
        Assert.That(result, Is.EqualTo("*Неизвестный чат*"));
    }

    [Test]
    public void GetChatLink_WithChatIdAndTitle_EmptyTitle_ReturnsEmptyBold()
    {
        // Arrange
        var chatId = 123456789L;
        var chatTitle = "";
        
        // Act
        var result = _chatLinkFormatter.GetChatLink(chatId, chatTitle);
        
        // Assert
        Assert.That(result, Is.EqualTo("**"));
    }

    [Test]
    public void GetChatLink_WithChatIdAndTitle_TitleWithMarkdownSymbols_EscapesCorrectly()
    {
        // Arrange
        var chatId = 123456789L;
        var chatTitle = "Test *Bold* _Italic_ [Link]";
        
        // Act
        var result = _chatLinkFormatter.GetChatLink(chatId, chatTitle);
        
        // Assert
        Assert.That(result, Is.EqualTo("*Test \\*Bold\\* \\_Italic\\_ \\[Link\\]*"));
    }
} 
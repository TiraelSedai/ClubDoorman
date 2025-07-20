using NUnit.Framework;
using Telegram.Bot.Types;
using ClubDoorman.Services;

namespace ClubDoorman.Test.Unit.Infrastructure;

[TestFixture]
[Category("unit")]
[Category("infrastructure")]
public class StatisticsServiceGetChatLinkTests
{
    private IChatLinkFormatter _chatLinkFormatter;

    [OneTimeSetUp]
    public void SetUp()
    {
        _chatLinkFormatter = new ChatLinkFormatter();
    }

    [Test]
    public void GetChatLink_PublicGroupWithUsername_ReturnsMarkdownLink()
    {
        var chat = new Chat { Id = 123456789, Username = "testgroup", Title = "Test Group" };
        var result = _chatLinkFormatter.GetChatLink(chat);
        Assert.That(result, Is.EqualTo("[Test Group](https://t.me/testgroup)"));
    }

    [Test]
    public void GetChatLink_SupergroupWithoutUsername_ReturnsTelegramLink()
    {
        var chat = new Chat { Id = -100123456789, Title = "Super Group" };
        var result = _chatLinkFormatter.GetChatLink(chat);
        Assert.That(result, Is.EqualTo("[Super Group](https://t.me/c/123456789)"));
    }

    [Test]
    public void GetChatLink_RegularGroupWithoutUsername_ReturnsBoldTitle()
    {
        var chat = new Chat { Id = -123456789, Title = "Regular Group" };
        var result = _chatLinkFormatter.GetChatLink(chat);
        Assert.That(result, Is.EqualTo("*Regular Group*"));
    }

    [Test]
    public void GetChatLink_ChannelWithoutUsername_ReturnsBoldTitle()
    {
        var chat = new Chat { Id = 123456789, Title = "Test Channel" };
        var result = _chatLinkFormatter.GetChatLink(chat);
        Assert.That(result, Is.EqualTo("*Test Channel*"));
    }

    [Test]
    public void GetChatLink_NullTitle_UsesDefaultTitle()
    {
        var chat = new Chat { Id = 123456789, Username = "testgroup", Title = null };
        var result = _chatLinkFormatter.GetChatLink(chat);
        Assert.That(result, Is.EqualTo("[Неизвестный чат](https://t.me/testgroup)"));
    }

    [Test]
    public void GetChatLink_EmptyTitle_ReturnsEmptyBrackets()
    {
        var chat = new Chat { Id = 123456789, Username = "testgroup", Title = "" };
        var result = _chatLinkFormatter.GetChatLink(chat);
        Assert.That(result, Is.EqualTo("[](https://t.me/testgroup)"));
    }

    [Test]
    public void GetChatLink_TitleWithMarkdownSymbols_EscapesCorrectly()
    {
        var chat = new Chat { Id = 123456789, Username = "testgroup", Title = "Test *Bold* _Italic_ [Link]" };
        var result = _chatLinkFormatter.GetChatLink(chat);
        Assert.That(result, Is.EqualTo("[Test \\*Bold\\* \\_Italic\\_ \\[Link\\]](https://t.me/testgroup)"));
    }
} 
using ClubDoorman.Services;
using ClubDoorman.Models;
using ClubDoorman.Infrastructure;
using ClubDoorman.Test.TestInfrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ClubDoorman.Test;

[TestFixture]
public class ModerationServiceSimpleTests : TestBase
{
    // Унифицированный setup через TestKit.Specialized.ModerationScenarios
    private TK.Specialized.ModerationScenarios.ModerationSetup _setup = null!;
    
    // Удобные ссылки на компоненты setup'а (для совместимости с существующими тестами)
    private ModerationService _moderationService => _setup.Service;
    private SpamHamClassifier _classifier => _setup.SpamClassifier;
    private MimicryClassifier _mimicryClassifier => _setup.MimicryClassifier;
    private BadMessageManager _badMessageManager => _setup.BadMessageManager;
    private Mock<IUserManager> _mockUserManager => _setup.UserManagerMock;
    private IAiChecks _mockAiChecks => _setup.AiChecks;
    private Mock<ILogger<ModerationService>> _mockLogger => _setup.LoggerMock;
    private SuspiciousUsersStorage _mockSuspiciousUsersStorage => _setup.SuspiciousUsersStorage;
    private Mock<ITelegramBotClient> _mockBotClient => _setup.BotClientMock;

    [SetUp]
    public void SetUp()
    {
        Console.WriteLine("Setting up test...");
        
        // Заменяем 45 строк дублированного кода на один вызов TestKit scenarios
        _setup = TK.Specialized.ModerationScenarios.CompleteSetup();
        
        Console.WriteLine("Setup completed");
    }

    [Test]
    public async Task CheckUserName_WithNullUser_ThrowsArgumentNullException()
    {
        Console.WriteLine("Starting CheckUserName_WithNullUser_ThrowsArgumentNullException");
        
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await _moderationService.CheckUserNameAsync(null!));
        
        Assert.That(exception.ParamName, Is.EqualTo("user"));
        Console.WriteLine("Completed CheckUserName_WithNullUser_ThrowsArgumentNullException");
    }

    [Test]
    public async Task CheckUserName_WithNormalName_ReturnsAllow()
    {
        Console.WriteLine("Starting CheckUserName_WithNormalName_ReturnsAllow");
        
        // Arrange
        var user = new User { FirstName = "John", LastName = "Doe" };

        // Act
        var result = await _moderationService.CheckUserNameAsync(user);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.EqualTo("Имя пользователя корректно"));
        Console.WriteLine("Completed CheckUserName_WithNormalName_ReturnsAllow");
    }

    [Test]
    public async Task CheckUserName_WithLongName_ReturnsReport()
    {
        Console.WriteLine("Starting CheckUserName_WithLongName_ReturnsReport");
        
        // Arrange
        var user = new User { FirstName = new string('A', 50), LastName = "Doe" };

        // Act
        var result = await _moderationService.CheckUserNameAsync(user);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Report));
        Assert.That(result.Reason, Does.Contain("Подозрительно длинное имя"));
        Console.WriteLine("Completed CheckUserName_WithLongName_ReturnsReport");
    }

    [Test]
    public async Task CheckUserName_WithExtremelyLongName_ReturnsBan()
    {
        Console.WriteLine("Starting CheckUserName_WithExtremelyLongName_ReturnsBan");
        
        // Arrange
        var user = new User { FirstName = new string('A', 100), LastName = "Doe" };

        // Act
        var result = await _moderationService.CheckUserNameAsync(user);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Does.Contain("Экстремально длинное имя"));
        Console.WriteLine("Completed CheckUserName_WithExtremelyLongName_ReturnsBan");
    }

    [Test]
    public async Task CheckMessage_WithNullMessage_ThrowsArgumentNullException()
    {
        Console.WriteLine("Starting CheckMessage_WithNullMessage_ThrowsArgumentNullException");
        
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await _moderationService.CheckMessageAsync(null!));
        
        Assert.That(exception.ParamName, Is.EqualTo("message"));
        Console.WriteLine("Completed CheckMessage_WithNullMessage_ThrowsArgumentNullException");
    }

    [Test]
    public async Task CheckMessage_WithBannedUser_ReturnsBan()
    {
        Console.WriteLine("Starting CheckMessage_WithBannedUser_ReturnsBan");
        
        // Arrange
        var user = new User { Id = 123, FirstName = "Test" };
        var chat = new Chat { Id = 456, Type = ChatType.Group };
        var message = new Message { From = user, Chat = chat, Text = "Hello" };

        _mockUserManager.Setup(x => x.InBanlist(user.Id))
            .ReturnsAsync(true);

        // Act
        var result = await _moderationService.CheckMessageAsync(message);

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Is.EqualTo("Пользователь в блэклисте спамеров"));
        Console.WriteLine("Completed CheckMessage_WithBannedUser_ReturnsBan");
    }
} 
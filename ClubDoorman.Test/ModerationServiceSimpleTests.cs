using ClubDoorman.Services;
using ClubDoorman.Models;
using ClubDoorman.Infrastructure;
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
    private ModerationService _moderationService;
    private SpamHamClassifier _classifier;
    private MimicryClassifier _mimicryClassifier;
    private BadMessageManager _badMessageManager;
    private Mock<IUserManager> _mockUserManager;
    private AiChecks _mockAiChecks;
    private Mock<ILogger<ModerationService>> _mockLogger;
    private SuspiciousUsersStorage _mockSuspiciousUsersStorage;
    private Mock<ITelegramBotClient> _mockBotClient;

    [SetUp]
    public void SetUp()
    {
        Console.WriteLine("Setting up test...");
        var classifierLogger = new Mock<ILogger<SpamHamClassifier>>();
        _classifier = new SpamHamClassifier(classifierLogger.Object);
        var mimicryLogger = new Mock<ILogger<MimicryClassifier>>();
        _mimicryClassifier = new MimicryClassifier(mimicryLogger.Object);
        _badMessageManager = new BadMessageManager();
        _mockUserManager = new Mock<IUserManager>();
        _mockLogger = new Mock<ILogger<ModerationService>>();
        
        // Создаем реальный SuspiciousUsersStorage с логгером
        var mockSuspiciousLogger = new Mock<ILogger<SuspiciousUsersStorage>>();
        _mockSuspiciousUsersStorage = new SuspiciousUsersStorage(mockSuspiciousLogger.Object);
        
        // Создаем реальный AiChecks с TelegramBotClient и логгером
        var bot = new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"); // Тестовый токен
        var mockAiLogger = new Mock<ILogger<AiChecks>>();
        _mockAiChecks = new AiChecks(new TelegramBotClientWrapper(bot), mockAiLogger.Object);
        
        _mockBotClient = new Mock<ITelegramBotClient>();

        _moderationService = new ModerationService(
            _classifier,
            _mimicryClassifier,
            _badMessageManager,
            _mockUserManager.Object,
            _mockAiChecks,
            _mockSuspiciousUsersStorage,
            _mockBotClient.Object,
            new Mock<IMessageService>().Object,
            _mockLogger.Object
        );
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
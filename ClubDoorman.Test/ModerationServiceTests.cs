using ClubDoorman.Services;
using ClubDoorman.Models;
using ClubDoorman.Infrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace ClubDoorman.Test;

[TestFixture]
public class ModerationServiceTests : TestBase
{
    private ModerationService _moderationService;
    private SpamHamClassifier _classifier;
    private MimicryClassifier _mimicryClassifier;
    private BadMessageManager _badMessageManager;
    private Mock<IUserManager> _mockUserManager;
    private AiChecks _aiChecks;
    private Mock<ILogger<ModerationService>> _mockLogger;
    private SuspiciousUsersStorage _mockSuspiciousUsersStorage;
    private Mock<ITelegramBotClient> _mockBotClient;

    public override void SetUp()
    {
        base.SetUp();
        Console.WriteLine("Setting up ModerationServiceTests...");
        
        // Инициализируем ML классификатор
        var mockSpamLogger = new Mock<ILogger<SpamHamClassifier>>();
        _classifier = new SpamHamClassifier(mockSpamLogger.Object);
        
        // Инициализируем MimicryClassifier
        var mockMimicryLogger = new Mock<ILogger<MimicryClassifier>>();
        _mimicryClassifier = new MimicryClassifier(mockMimicryLogger.Object);
        
        // Инициализируем BadMessageManager
        _badMessageManager = new BadMessageManager();
        
        // Создаем моки
        _mockUserManager = new Mock<IUserManager>();
        _mockBotClient = new Mock<ITelegramBotClient>();
        
        // Создаем реальный SuspiciousUsersStorage с логгером
        var mockSuspiciousLogger = new Mock<ILogger<SuspiciousUsersStorage>>();
        _mockSuspiciousUsersStorage = new SuspiciousUsersStorage(mockSuspiciousLogger.Object);
        
        // В реальном приложении токен будет в конфигурации
        var bot = new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"); // Тестовый токен
        var mockAiLogger = new Mock<ILogger<AiChecks>>();
        _aiChecks = new AiChecks(bot, mockAiLogger.Object);
        _mockLogger = new Mock<ILogger<ModerationService>>();

        _moderationService = new ModerationService(
            _classifier,
            _mimicryClassifier,
            _badMessageManager,
            _mockUserManager.Object,
            _aiChecks,
            _mockSuspiciousUsersStorage,
            _mockBotClient.Object,
            _mockLogger.Object
        );
        
        Console.WriteLine("ModerationServiceTests setup completed");
    }

    [Test]
    public void CheckUserName_WithNullUser_ThrowsArgumentNullException()
    {
        Console.WriteLine("Starting CheckUserName_WithNullUser_ThrowsArgumentNullException");
        
        ExecuteWithTimeout((cancellationToken) =>
        {
            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await _moderationService.CheckUserNameAsync(null!));
            
            Assert.That(exception.ParamName, Is.EqualTo("user"));
            Console.WriteLine("Completed CheckUserName_WithNullUser_ThrowsArgumentNullException");
        });
    }

    [Test]
    public async Task CheckUserName_WithNormalName_ReturnsAllow()
    {
        Console.WriteLine("Starting CheckUserName_WithNormalName_ReturnsAllow");
        
        await ExecuteWithTimeout(async (cancellationToken) =>
        {
            // Arrange
            var user = new User { FirstName = "John", LastName = "Doe" };

            // Act
            var result = await _moderationService.CheckUserNameAsync(user);

            // Assert
            Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
            Assert.That(result.Reason, Is.EqualTo("Имя пользователя корректно"));
            Console.WriteLine("Completed CheckUserName_WithNormalName_ReturnsAllow");
        });
    }

    [Test]
    public async Task CheckUserName_WithLongName_ReturnsReport()
    {
        Console.WriteLine("Starting CheckUserName_WithLongName_ReturnsReport");
        
        await ExecuteWithTimeout(async (cancellationToken) =>
        {
            // Arrange
            var user = new User { FirstName = new string('A', 50), LastName = "Doe" };

            // Act
            var result = await _moderationService.CheckUserNameAsync(user);

            // Assert
            Assert.That(result.Action, Is.EqualTo(ModerationAction.Report));
            Assert.That(result.Reason, Does.Contain("Подозрительно длинное имя"));
            Console.WriteLine("Completed CheckUserName_WithLongName_ReturnsReport");
        });
    }

    [Test]
    public async Task CheckUserName_WithExtremelyLongName_ReturnsBan()
    {
        Console.WriteLine("Starting CheckUserName_WithExtremelyLongName_ReturnsBan");
        
        await ExecuteWithTimeout(async (cancellationToken) =>
        {
            // Arrange
            var user = new User { FirstName = new string('A', 100), LastName = "Doe" };

            // Act
            var result = await _moderationService.CheckUserNameAsync(user);

            // Assert
            Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
            Assert.That(result.Reason, Does.Contain("Экстремально длинное имя"));
            Console.WriteLine("Completed CheckUserName_WithExtremelyLongName_ReturnsBan");
        });
    }

    [Test]
    public void CheckMessage_WithNullMessage_ThrowsArgumentNullException()
    {
        Console.WriteLine("Starting CheckMessage_WithNullMessage_ThrowsArgumentNullException");
        
        ExecuteWithTimeout((cancellationToken) =>
        {
            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await _moderationService.CheckMessageAsync(null!));
            
            Assert.That(exception.ParamName, Is.EqualTo("message"));
            Console.WriteLine("Completed CheckMessage_WithNullMessage_ThrowsArgumentNullException");
        });
    }

    [Test]
    public async Task CheckMessage_WithBannedUser_ReturnsBan()
    {
        Console.WriteLine("Starting CheckMessage_WithBannedUser_ReturnsBan");
        
        await ExecuteWithTimeout(async (cancellationToken) =>
        {
            // Arrange
            var user = TestData.CreateTestUser();
            var chat = TestData.CreateTestChat();
            var message = TestData.CreateTestMessage(user, chat, "Hello");

            _mockUserManager.Setup(x => x.InBanlist(user.Id))
                .ReturnsAsync(true);

            // Act
            var result = await _moderationService.CheckMessageAsync(message);

            // Assert
            Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
            Assert.That(result.Reason, Is.EqualTo("Пользователь в блэклисте спамеров"));
            Console.WriteLine("Completed CheckMessage_WithBannedUser_ReturnsBan");
        });
    }

    [Test]
    public async Task CheckMessage_WithReplyMarkup_ReturnsBan()
    {
        Console.WriteLine("Starting CheckMessage_WithReplyMarkup_ReturnsBan");
        
        await ExecuteWithTimeout(async (cancellationToken) =>
        {
            // Arrange
            var user = TestData.CreateTestUser();
            var chat = TestData.CreateTestChat();
            var message = TestData.CreateTestMessageWithButtons(user, chat, "Hello");

            _mockUserManager.Setup(x => x.InBanlist(user.Id))
                .ReturnsAsync(false);

            // Act
            var result = await _moderationService.CheckMessageAsync(message);

            // Assert
            Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
            Assert.That(result.Reason, Is.EqualTo("Сообщение с кнопками"));
            Console.WriteLine("Completed CheckMessage_WithReplyMarkup_ReturnsBan");
        });
    }

    [Test]
    public async Task CheckMessage_WithStory_ReturnsDelete()
    {
        Console.WriteLine("Starting CheckMessage_WithStory_ReturnsDelete");
        
        await ExecuteWithTimeout(async (cancellationToken) =>
        {
            // Arrange
            var user = TestData.CreateTestUser();
            var chat = TestData.CreateTestChat();
            var message = TestData.CreateTestMessageWithStory(user, chat);

            _mockUserManager.Setup(x => x.InBanlist(user.Id))
                .ReturnsAsync(false);

            // Act
            var result = await _moderationService.CheckMessageAsync(message);

            // Assert
            Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
            Assert.That(result.Reason, Is.EqualTo("Сторис"));
            Console.WriteLine("Completed CheckMessage_WithStory_ReturnsDelete");
        });
    }

    [Test]
    public async Task CheckMessage_WithNormalMessage_ProcessesCorrectly()
    {
        Console.WriteLine("Starting CheckMessage_WithNormalMessage_ProcessesCorrectly");
        
        await ExecuteWithTimeout(async (cancellationToken) =>
        {
            // Arrange
            var user = TestData.CreateTestUser();
            var chat = TestData.CreateTestChat();
            var message = TestData.CreateTestMessage(user, chat, "Hello, this is a normal message");

            _mockUserManager.Setup(x => x.InBanlist(user.Id))
                .ReturnsAsync(false);
            
            // Act
            var result = await _moderationService.CheckMessageAsync(message);

            // Assert - проверяем, что сообщение обрабатывается корректно
            Assert.That(result.Action, Is.Not.EqualTo(ModerationAction.Ban));
            Console.WriteLine("Completed CheckMessage_WithNormalMessage_ProcessesCorrectly");
        });
    }

    [Test]
    public void IsUserApproved_WithValidUserId_ReturnsExpectedResult()
    {
        Console.WriteLine("Starting IsUserApproved_WithValidUserId_ReturnsExpectedResult");
        
        ExecuteWithTimeout((cancellationToken) =>
        {
            // Arrange
            var userId = 123L;
            var chatId = 456L;

            _mockUserManager.Setup(x => x.Approved(userId, It.IsAny<long?>())).Returns(true);
            // Act
            var result = _moderationService.IsUserApproved(userId, chatId);

            // Assert
            Assert.That(result, Is.True);
            _mockUserManager.Verify(x => x.Approved(userId, It.IsAny<long?>()), Times.Once);
            Console.WriteLine("Completed IsUserApproved_WithValidUserId_ReturnsExpectedResult");
        });
    }

    [Test]
    public async Task IncrementGoodMessageCount_WithValidUser_ApprovesUserAfterThreeMessages()
    {
        Console.WriteLine("Starting IncrementGoodMessageCount_WithValidUser_ApprovesUserAfterThreeMessages");
        
        await ExecuteWithTimeout(async (cancellationToken) =>
        {
            // Arrange
            var user = new User { Id = 123, FirstName = "Test" };
            var chat = new Chat { Id = 456, Title = "Test Chat" };

            _mockUserManager.Setup(x => x.Approve(user.Id, null))
                .Returns(ValueTask.CompletedTask);

            // Act - Send 3 good messages
            await _moderationService.IncrementGoodMessageCountAsync(user, chat, "Good message 1");
            await _moderationService.IncrementGoodMessageCountAsync(user, chat, "Good message 2");
            await _moderationService.IncrementGoodMessageCountAsync(user, chat, "Good message 3");

            // Assert
            _mockUserManager.Verify(x => x.Approve(user.Id, null), Times.Once);
            Console.WriteLine("Completed IncrementGoodMessageCount_WithValidUser_ApprovesUserAfterThreeMessages");
        });
    }
} 
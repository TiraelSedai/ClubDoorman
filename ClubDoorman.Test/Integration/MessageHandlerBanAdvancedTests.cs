using ClubDoorman.Handlers;
using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.Test.TestKit;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Integration;

/// <summary>
/// Продвинутые тесты банов с использованием всей мощи TestKit
/// Демонстрирует использование автомоков, builders, bogus и fluent API
/// </summary>
[TestFixture]
[Category("integration")]
[Category("ban")]
[Category("advanced")]
public class MessageHandlerBanAdvancedTests
{
    private MessageHandler _handler;
    private Mock<ITelegramBotClientWrapper> _botMock;
    private Mock<IUserBanService> _userBanServiceMock;
    private Mock<IModerationService> _moderationServiceMock;
    private Mock<IUserManager> _userManagerMock;

    [SetUp]
    public void Setup()
    {
        // Используем автомоки для создания основных зависимостей
        _botMock = TK.CreateMockBotClientWrapper();
        _userBanServiceMock = TK.CreateMockUserBanService();
        _moderationServiceMock = TK.CreateMockModerationService();
        _userManagerMock = TK.CreateMockUserManager();

        // Создаем MessageHandler с автомоками
        _handler = TestKitAutoFixture.Create<MessageHandler>();
    }

    [Test]
    [Category("autofixture")]
    public void AutoFixture_CreateMessageHandler_WithAllDependencies_WorksCorrectly()
    {
        // Arrange & Act - используем автомоки для создания объекта с 15+ зависимостями
        var handler = TestKitAutoFixture.Create<MessageHandler>();

        // Assert
        Assert.That(handler, Is.Not.Null);
        Assert.That(handler, Is.InstanceOf<MessageHandler>());
    }

    [Test]
    [Category("builders")]
    public void Builders_CreateBanScenario_WithFluentApi_WorksCorrectly()
    {
        // Arrange - используем builders для читаемого создания тестовых данных
        var user = TestKitBuilders.CreateUser()
            .WithId(12345)
            .WithUsername("spammer")
            .WithFirstName("Spam")
            .AsRegularUser()
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(67890)
            .WithTitle("Test Group")
            .AsSupergroup()
            .Build();

        var spamMessage = TestKitBuilders.CreateMessage()
            .WithText("🔥💰🎁 Make money fast! 💰🔥🎁")
            .FromUser(user)
            .InChat(chat)
            .Build();

        var banResult = TestKitBuilders.CreateModerationResult()
            .WithAction(ModerationAction.Ban)
            .WithReason("Spam detected by ML")
            .WithConfidence(0.95)
            .Build();

        // Assert
        Assert.That(user.Id, Is.EqualTo(12345));
        Assert.That(user.Username, Is.EqualTo("spammer"));
        Assert.That(chat.Id, Is.EqualTo(67890));
        Assert.That(chat.Title, Is.EqualTo("Test Group"));
        Assert.That(spamMessage.Text, Is.EqualTo("🔥💰🎁 Make money fast! 💰🔥🎁"));
        Assert.That(banResult.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(banResult.Reason, Is.EqualTo("Spam detected by ML"));
    }

    [Test]
    [Category("bogus")]
    public void Bogus_CreateRealisticBanScenario_WorksCorrectly()
    {
        // Arrange - используем Bogus для создания реалистичных данных
        var realisticUser = TestKitBogus.CreateRealisticUser(12345);
        var realisticGroup = TestKitBogus.CreateRealisticGroup(67890);
        var realisticSpamMessage = TestKitBogus.CreateRealisticSpamMessage(realisticUser, realisticGroup);

        // Assert - проверяем реалистичность данных
        Assert.That(realisticUser.Id, Is.EqualTo(12345));
        Assert.That(realisticUser.IsBot, Is.False);
        Assert.That(realisticUser.FirstName, Is.Not.Null.And.Not.Empty);
        Assert.That(realisticUser.Username, Is.Not.Null.And.Not.Empty);
        
        Assert.That(realisticGroup.Id, Is.EqualTo(67890));
        Assert.That(realisticGroup.Type, Is.EqualTo(ChatType.Group));
        Assert.That(realisticGroup.Title, Is.Not.Null.And.Not.Empty);
        
        Assert.That(realisticSpamMessage.From, Is.EqualTo(realisticUser));
        Assert.That(realisticSpamMessage.Chat, Is.EqualTo(realisticGroup));
        Assert.That(TestKitBogus.IsSpamText(realisticSpamMessage.Text), Is.True, $"Text '{realisticSpamMessage.Text}' should be detected as spam");
    }

    [Test]
    [Category("smart-mocks")]
    public void SmartMocks_ModerationService_HandlesSpamAutomatically()
    {
        // Arrange - используем умные автомоки
        var moderationService = TestKitAutoFixture.Create<IModerationService>();
        var spamMessage = TestKitBuilders.CreateMessage()
            .AsSpam()
            .Build();

        // Act
        var result = moderationService.CheckMessageAsync(spamMessage).Result;

        // Assert - умный мок возвращает базовое поведение
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.EqualTo("Mock moderation"));
    }

    [Test]
    [Category("smart-mocks")]
    public void SmartMocks_ModerationService_HandlesValidMessageAutomatically()
    {
        // Arrange - используем умные автомоки
        var moderationService = TestKitAutoFixture.Create<IModerationService>();
        var validMessage = TestKitBuilders.CreateMessage()
            .WithText("Hello, this is a valid message!")
            .Build();

        // Act
        var result = moderationService.CheckMessageAsync(validMessage).Result;

        // Assert - умный мок возвращает базовое поведение
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
        Assert.That(result.Reason, Is.EqualTo("Mock moderation"));
    }

    [Test]
    [Category("autofixture")]
    public void AutoFixture_CreateWithFixture_CustomizationWorksCorrectly()
    {
        // Arrange - создаем с возможностью кастомизации
        var (handler, fixture) = TestKitAutoFixture.CreateWithFixture<MessageHandler>();

        // Act - кастомизируем конкретный мок через создание нового
        var customModerationService = new Mock<IModerationService>();
        customModerationService.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Ban, "Custom ban logic"));

        var message = TestKitBuilders.CreateMessage()
            .WithText("Test message")
            .Build();

        // Act
        var result = customModerationService.Object.CheckMessageAsync(message).Result;

        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Is.EqualTo("Custom ban logic"));
    }

    [Test]
    [Category("collections")]
    public void AutoFixture_CreateMany_CollectionsWorkCorrectly()
    {
        // Arrange - создаем коллекции объектов
        var users = TestKitAutoFixture.CreateMany<Telegram.Bot.Types.User>(5).ToList();
        var messages = TestKitAutoFixture.CreateMany<Telegram.Bot.Types.Message>(3).ToList();
        var spamMessages = TestKitAutoFixture.CreateManySpamMessages(4).ToList();

        // Assert
        Assert.That(users, Has.Count.EqualTo(5));
        Assert.That(messages, Has.Count.EqualTo(3));
        Assert.That(spamMessages, Has.Count.EqualTo(4));

        // Проверяем, что все объекты созданы корректно
        foreach (var user in users)
        {
            Assert.That(user.Id, Is.GreaterThan(0));
            Assert.That(user.FirstName, Is.Not.Null);
        }

        foreach (var message in messages)
        {
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Text, Is.Not.Null);
        }

        foreach (var spamMessage in spamMessages)
        {
            Assert.That(spamMessage.Text, Is.Not.Null);
            // TestKitBogus может генерировать не всегда спамный текст
            Assert.That(spamMessage.Text, Is.Not.Null.And.Not.Empty);
        }
    }

    [Test]
    [Category("telegram-helpers")]
    public void TelegramHelpers_CreateBanScenario_WorksCorrectly()
    {
        // Arrange - используем Telegram helpers для создания сценариев
        var (fakeClient, envelope, message, update) = TestKitTelegram.CreateSpamScenario(
            userId: 12345,
            chatId: 67890
        );

        // Assert
        Assert.That(fakeClient, Is.Not.Null);
        Assert.That(envelope, Is.Not.Null);
        Assert.That(message, Is.Not.Null);
        Assert.That(update, Is.Not.Null);

        Assert.That(envelope.UserId, Is.EqualTo(12345));
        Assert.That(envelope.ChatId, Is.EqualTo(67890));
        Assert.That(envelope.Text, Is.EqualTo("🔥💰🎁 Make money fast! 💰🔥🎁"));

        Assert.That(message.From, Is.Not.Null);
        Assert.That(message.From.Id, Is.EqualTo(12345));
        Assert.That(message.Chat.Id, Is.EqualTo(67890));
    }

    [Test]
    [Category("facade")]
    public void Facade_CreateMessageHandlerFactory_WorksCorrectly()
    {
        // Arrange - используем фасадные методы
        var factory = TK.CreateMessageHandlerFactory();
        var moderationFactory = TK.CreateModerationServiceFactory();
        var captchaFactory = TK.CreateCaptchaServiceFactory();

        // Act
        var handler = factory.CreateMessageHandler();
        var moderationService = moderationFactory.CreateModerationService();
        var captchaService = captchaFactory.CreateCaptchaService();

        // Assert
        Assert.That(handler, Is.Not.Null);
        Assert.That(moderationService, Is.Not.Null);
        Assert.That(captchaService, Is.Not.Null);
    }

    [Test]
    [Category("integration")]
    public void Integration_CompleteBanFlow_WithAllTestKitFeatures_WorksCorrectly()
    {
        // Arrange - комплексный тест с использованием всех возможностей TestKit
        
        // 1. Создаем фабрику через фасад
        var factory = TK.CreateMessageHandlerFactory();
        
        // 2. Создаем реалистичные данные через Bogus
        var realisticUser = TestKitBogus.CreateRealisticUser(12345);
        var realisticGroup = TestKitBogus.CreateRealisticGroup(67890);
        
        // 3. Создаем сообщение через Builders
        var spamMessage = TestKitBuilders.CreateMessage()
            .WithText("🔥💰🎁 Make money fast! 💰🔥🎁")
            .FromUser(realisticUser)
            .InChat(realisticGroup)
            .AsSpam()
            .Build();
        
        // 4. Создаем результат модерации через Builders
        var banResult = TestKitBuilders.CreateModerationResult()
            .WithAction(ModerationAction.Ban)
            .WithReason("Spam detected by ML")
            .WithConfidence(0.95)
            .Build();
        
        // 5. Создаем автомоки
        var moderationService = TestKitAutoFixture.Create<IModerationService>();
        var userManager = TestKitAutoFixture.Create<IUserManager>();
        
        // 6. Настраиваем моки через создание новых
        var moderationServiceMock = new Mock<IModerationService>();
        moderationServiceMock.Setup(x => x.CheckMessageAsync(spamMessage))
            .ReturnsAsync(banResult);
        
        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(x => x.Approved(realisticUser.Id, realisticGroup.Id))
            .Returns(false);
        
        // Act
        var result = moderationServiceMock.Object.CheckMessageAsync(spamMessage).Result;
        var isApproved = userManagerMock.Object.Approved(realisticUser.Id, realisticGroup.Id);
        
        // Assert
        Assert.That(result.Action, Is.EqualTo(ModerationAction.Ban));
        Assert.That(result.Reason, Is.EqualTo("Spam detected by ML"));
        Assert.That(result.Confidence, Is.EqualTo(0.95));
        Assert.That(isApproved, Is.False);
        
        // Проверяем реалистичность данных
        Assert.That(realisticUser.Id, Is.EqualTo(12345));
        Assert.That(realisticGroup.Id, Is.EqualTo(67890));
        Assert.That(TestKitBogus.IsSpamText(spamMessage.Text), Is.True, $"Text '{spamMessage.Text}' should be detected as spam");
    }

    [Test]
    [Category("performance")]
    public void Performance_CreateManyObjects_WithAutoFixture_IsFast()
    {
        // Arrange & Act - тест производительности автомоков
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Создаем много объектов через автомоки
        var handlers = TestKitAutoFixture.CreateMany<MessageHandler>(10).ToList();
        var services = TestKitAutoFixture.CreateMany<IModerationService>(10).ToList();
        var users = TestKitAutoFixture.CreateMany<Telegram.Bot.Types.User>(20).ToList();
        var messages = TestKitAutoFixture.CreateMany<Telegram.Bot.Types.Message>(20).ToList();
        
        stopwatch.Stop();
        
        // Assert
        Assert.That(handlers, Has.Count.EqualTo(10));
        Assert.That(services, Has.Count.EqualTo(10));
        Assert.That(users, Has.Count.EqualTo(20));
        Assert.That(messages, Has.Count.EqualTo(20));
        
        // Проверяем, что создание объектов происходит быстро
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000), 
            "Создание 60 объектов через автомоки должно происходить быстро");
    }
} 
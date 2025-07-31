using ClubDoorman.Handlers;
using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.TestKit;

/// <summary>
/// Тесты для новых билдеров
/// <tags>tests, builders, fluent-api, test-infrastructure</tags>
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
[Category("builders")]
public class TestKitBuilderTests
{
    [Test]
    public void MessageHandlerBuilder_WithStandardMocks_CreatesValidHandler()
    {
        // Arrange & Act
        var handler = TK.CreateMessageHandlerBuilder()
            .WithStandardMocks()
            .Build();

        // Assert
        Assert.That(handler, Is.Not.Null);
        Assert.That(handler, Is.InstanceOf<MessageHandler>());
    }

    [Test]
    public void MessageHandlerBuilder_WithBanMocks_CreatesHandlerWithBanConfiguration()
    {
        // Arrange & Act
        var builder = TK.CreateMessageHandlerBuilder()
            .WithBanMocks();
        
        var handler = builder.Build();

        // Assert
        Assert.That(handler, Is.Not.Null);
        Assert.That(builder.BotMock, Is.Not.Null);
        Assert.That(builder.ModerationServiceMock, Is.Not.Null);
    }

    [Test]
    public void MessageHandlerBuilder_WithModerationService_CreatesHandlerWithCustomModeration()
    {
        // Arrange & Act
        var handler = TK.CreateMessageHandlerBuilder()
            .WithModerationService(builder => builder
                .ThatBansUsers("Custom reason")
                .WithConfidence(0.9))
            .WithStandardMocks()
            .Build();

        // Assert
        Assert.That(handler, Is.Not.Null);
    }

    [Test]
    public void MessageHandlerBuilder_WithUserManager_CreatesHandlerWithCustomUserManager()
    {
        // Arrange & Act
        var handler = TK.CreateMessageHandlerBuilder()
            .WithUserManager(builder => builder
                .ThatApprovesUser(12345)
                .ThatIsNotInBanlist(12345))
            .WithStandardMocks()
            .Build();

        // Assert
        Assert.That(handler, Is.Not.Null);
    }

    [Test]
    public void ModerationServiceMockBuilder_ThatBansUsers_CreatesCorrectMock()
    {
        // Arrange & Act
        var mock = TK.CreateModerationServiceMock()
            .ThatBansUsers("Spam detected")
            .Build();

        // Assert
        Assert.That(mock, Is.Not.Null);
        Assert.That(mock.Object, Is.InstanceOf<IModerationService>());
    }

    [Test]
    public void ModerationServiceMockBuilder_ThatAllowsMessages_CreatesCorrectMock()
    {
        // Arrange & Act
        var mock = TK.CreateModerationServiceMock()
            .ThatAllowsMessages()
            .Build();

        // Assert
        Assert.That(mock, Is.Not.Null);
        Assert.That(mock.Object, Is.InstanceOf<IModerationService>());
    }

    [Test]
    public void UserManagerMockBuilder_ThatApprovesUser_CreatesCorrectMock()
    {
        // Arrange & Act
        var mock = TK.CreateUserManagerMock()
            .ThatApprovesUser(12345)
            .ThatIsNotInBanlist(12345)
            .Build();

        // Assert
        Assert.That(mock, Is.Not.Null);
        Assert.That(mock.Object, Is.InstanceOf<IUserManager>());
    }

    [Test]
    public void CaptchaServiceMockBuilder_ThatSucceeds_CreatesCorrectMock()
    {
        // Arrange & Act
        var mock = TK.CreateCaptchaServiceMock()
            .ThatSucceeds()
            .Build();

        // Assert
        Assert.That(mock, Is.Not.Null);
        Assert.That(mock.Object, Is.InstanceOf<ICaptchaService>());
    }

    [Test]
    public void AiChecksMockBuilder_ThatApprovesPhoto_CreatesCorrectMock()
    {
        // Arrange & Act
        var mock = TK.CreateAiChecksMock()
            .ThatApprovesPhoto()
            .Build();

        // Assert
        Assert.That(mock, Is.Not.Null);
        Assert.That(mock.Object, Is.InstanceOf<IAiChecks>());
    }

    [Test]
    public void TelegramBotMockBuilder_ThatSendsMessageSuccessfully_CreatesCorrectMock()
    {
        // Arrange & Act
        var mock = TK.CreateTelegramBotMock()
            .ThatSendsMessageSuccessfully()
            .Build();

        // Assert
        Assert.That(mock, Is.Not.Null);
        Assert.That(mock.Object, Is.InstanceOf<ITelegramBotClientWrapper>());
    }

    [Test]
    public void MessageHandlerBuilder_ComplexScenario_CreatesValidHandler()
    {
        // Arrange & Act
        var handler = TK.CreateMessageHandlerBuilder()
            .WithModerationService(builder => builder
                .ThatDeletesMessages("Spam detected")
                .WithConfidence(0.8))
            .WithUserManager(builder => builder
                .ThatRejectsUser(12345)
                .ThatIsInBanlist(12345))
            .WithCaptchaService(builder => builder
                .ThatFails())
            .WithAiChecks(builder => builder
                .ThatRejectsPhoto())
            .WithTelegramBot(builder => builder
                .ThatSendsMessageSuccessfully())
            .Build();

        // Assert
        Assert.That(handler, Is.Not.Null);
        Assert.That(handler, Is.InstanceOf<MessageHandler>());
    }
} 
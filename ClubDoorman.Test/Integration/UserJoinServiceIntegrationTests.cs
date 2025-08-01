using NUnit.Framework;
using ClubDoorman.Handlers;
using ClubDoorman.Services.UserJoin;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Integration;

/// <summary>
/// Интеграционные тесты для UserJoinService
/// <tags>integration, user-join-service, new-members, proxy</tags>
/// </summary>
[TestFixture]
[Category("integration")]
[Category("user-join-service")]
public class UserJoinServiceIntegrationTests
{
    private UserJoinServiceBuilder _builder = null!;
    private IUserJoinService _userJoinService = null!;
    private Mock<IMessageHandler> _messageHandlerMock = null!;
    private Mock<ILogger<UserJoinService>> _loggerMock = null!;

    [SetUp]
    public void Setup()
    {
        _builder = TK.CreateUserJoinServiceBuilder()
            .WithStandardMocks();
        _userJoinService = _builder.Build();
        _messageHandlerMock = _builder.MessageHandlerMock;
        _loggerMock = _builder.LoggerMock;
    }

    /// <summary>
    /// POC: Проверка создания UserJoinService через билдер
    /// <tags>poc, builder, user-join-service</tags>
    /// </summary>
    [Test]
    public void CreateUserJoinService_WithBuilder_ReturnsValidService()
    {
        // Arrange & Act
        var userJoinService = TK.CreateUserJoinServiceBuilder()
            .WithStandardMocks()
            .Build();

        // Assert
        Assert.That(userJoinService, Is.Not.Null);
        Assert.That(userJoinService, Is.InstanceOf<IUserJoinService>());
    }

    /// <summary>
    /// POC: Проверка доступа к MessageHandler через билдер
    /// <tags>poc, builder, message-handler</tags>
    /// </summary>
    [Test]
    public void Builder_ProvidesAccessToMessageHandler()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithStandardMocks();

        // Act & Assert
        Assert.That(builder.MessageHandlerMock, Is.Not.Null);
        Assert.That(builder.LoggerMock, Is.Not.Null);
    }

    /// <summary>
    /// POC: Проверка проксирования вызова HandleNewMembersAsync
    /// <tags>poc, proxy, handle-new-members</tags>
    /// </summary>
    [Test]
    public async Task HandleNewMembersAsync_ValidMessage_ProxiesToMessageHandler()
    {
        // Arrange
        var (_, envelope, message, _) = TK.CreateNewUserScenario();

        // Act
        await _userJoinService.HandleNewMembersAsync(message, CancellationToken.None);

        // Assert
        _messageHandlerMock.Verify(x => x.HandleNewMembersAsync(message, CancellationToken.None), Times.Once);
    }

    /// <summary>
    /// POC: Проверка проксирования вызова ProcessNewUserAsync
    /// <tags>poc, proxy, process-new-user</tags>
    /// </summary>
    [Test]
    public async Task ProcessNewUserAsync_ValidUser_ProxiesToMessageHandler()
    {
        // Arrange
        var (_, envelope, message, _) = TK.CreateNewUserScenario();
        var user = message.NewChatMembers?.FirstOrDefault() ?? TK.CreateUser();

        // Act
        await _userJoinService.ProcessNewUserAsync(message, user, CancellationToken.None);

        // Assert
        _messageHandlerMock.Verify(x => x.ProcessNewUserAsync(message, user, CancellationToken.None), Times.Once);
    }
} 
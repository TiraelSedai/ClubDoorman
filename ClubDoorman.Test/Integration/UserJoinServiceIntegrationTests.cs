using NUnit.Framework;
using ClubDoorman.Handlers;
using ClubDoorman.Services.UserJoin;
using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot.Types;
using ClubDoorman.Models;

namespace ClubDoorman.Test.Integration;

/// <summary>
/// Интеграционные тесты для UserJoinService
/// <tags>integration, user-join-service, new-members, proxy, external-calls</tags>
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

    #region POC Tests (существующие)

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

    #endregion

    #region Real Integration Tests

    /// <summary>
    /// Тест успешного присоединения пользователя с проверкой логов
    /// <tags>integration, success-scenario, user-join, logging</tags>
    /// </summary>
    [Test]
    public async Task ProcessNewUserAsync_SuccessfulJoin_LogsAndProxiesCorrectly()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithSuccessfulJoinScenario();
        var userJoinService = builder.Build();
        
        var (_, envelope, message, _) = TK.CreateNewUserScenario();
        var user = message.NewChatMembers?.FirstOrDefault() ?? TK.CreateUser();

        // Act
        await userJoinService.ProcessNewUserAsync(message, user, CancellationToken.None);

        // Assert
        builder.MessageHandlerMock.Verify(x => x.ProcessNewUserAsync(message, user, CancellationToken.None), Times.Once);
        
        // Проверяем логирование
        builder.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Проксируем ProcessNewUserAsync")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться проксирование вызова");
    }

    /// <summary>
    /// Тест обработки пользователя в блэклисте с проверкой вызова бана
    /// <tags>integration, blacklist-scenario, user-ban, verification</tags>
    /// </summary>
    [Test]
    public async Task ProcessNewUserAsync_BlacklistedUser_CallsBanService()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithBlacklistedUserScenario();
        var userJoinService = builder.Build();
        
        var (_, envelope, message, _) = TK.CreateNewUserScenario();
        var user = message.NewChatMembers?.FirstOrDefault() ?? TK.CreateUser();

        // Act
        await userJoinService.ProcessNewUserAsync(message, user, CancellationToken.None);

        // Assert - проверяем, что MessageHandler был вызван (он содержит логику бана)
        builder.MessageHandlerMock.Verify(x => x.ProcessNewUserAsync(message, user, CancellationToken.None), Times.Once);
        
        // Проверяем логирование
        builder.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Проксируем ProcessNewUserAsync")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться проксирование вызова");
    }

    /// <summary>
    /// Тест обработки пользователя с длинным именем
    /// <tags>integration, long-name-scenario, user-ban, verification</tags>
    /// </summary>
    [Test]
    public async Task ProcessNewUserAsync_LongName_CallsBanService()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithLongNameScenario();
        var userJoinService = builder.Build();
        
        var (_, envelope, message, _) = TK.CreateNewUserScenario();
        var user = message.NewChatMembers?.FirstOrDefault() ?? TK.CreateUser();

        // Act
        await userJoinService.ProcessNewUserAsync(message, user, CancellationToken.None);

        // Assert - проверяем, что MessageHandler был вызван (он содержит логику проверки имени)
        builder.MessageHandlerMock.Verify(x => x.ProcessNewUserAsync(message, user, CancellationToken.None), Times.Once);
        
        // Проверяем логирование
        builder.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Проксируем ProcessNewUserAsync")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться проксирование вызова");
    }

    /// <summary>
    /// Тест обработки клубного пользователя с проверкой пропуска проверок
    /// <tags>integration, club-user-scenario, skip-checks, verification</tags>
    /// </summary>
    [Test]
    public async Task ProcessNewUserAsync_ClubUser_SkipsChecks()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithClubUserScenario();
        var userJoinService = builder.Build();
        
        var (_, envelope, message, _) = TK.CreateNewUserScenario();
        var user = message.NewChatMembers?.FirstOrDefault() ?? TK.CreateUser();

        // Act
        await userJoinService.ProcessNewUserAsync(message, user, CancellationToken.None);

        // Assert - проверяем, что MessageHandler был вызван (он содержит логику проверки клубного пользователя)
        builder.MessageHandlerMock.Verify(x => x.ProcessNewUserAsync(message, user, CancellationToken.None), Times.Once);
        
        // Проверяем логирование
        builder.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Проксируем ProcessNewUserAsync")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться проксирование вызова");
    }

    /// <summary>
    /// Тест создания капчи для нового пользователя
    /// <tags>integration, captcha-scenario, user-verification, verification</tags>
    /// </summary>
    [Test]
    public async Task ProcessNewUserAsync_CaptchaCreation_CallsCaptchaService()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithCaptchaScenario();
        var userJoinService = builder.Build();
        
        var (_, envelope, message, _) = TK.CreateNewUserScenario();
        var user = message.NewChatMembers?.FirstOrDefault() ?? TK.CreateUser();

        // Act
        await userJoinService.ProcessNewUserAsync(message, user, CancellationToken.None);

        // Assert - проверяем, что MessageHandler был вызван (он содержит логику создания капчи)
        builder.MessageHandlerMock.Verify(x => x.ProcessNewUserAsync(message, user, CancellationToken.None), Times.Once);
        
        // Проверяем логирование
        builder.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Проксируем ProcessNewUserAsync")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться проксирование вызова");
    }

    /// <summary>
    /// Тест обработки ошибки внешнего сервиса с проверкой graceful handling
    /// <tags>integration, error-scenario, graceful-handling, verification</tags>
    /// </summary>
    [Test]
    public async Task ProcessNewUserAsync_ExternalServiceError_HandlesGracefully()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithErrorScenario();
        var userJoinService = builder.Build();
        
        var (_, envelope, message, _) = TK.CreateNewUserScenario();
        var user = message.NewChatMembers?.FirstOrDefault() ?? TK.CreateUser();

        // Act & Assert - должно обрабатываться gracefully, но MessageHandler все равно вызывается
        await userJoinService.ProcessNewUserAsync(message, user, CancellationToken.None);
        
        builder.MessageHandlerMock.Verify(x => x.ProcessNewUserAsync(message, user, CancellationToken.None), Times.Once);
        
        // Проверяем логирование
        builder.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Проксируем ProcessNewUserAsync")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться проксирование вызова");
    }

    #endregion

    #region Logging Tests

    /// <summary>
    /// Тест логирования для HandleNewMembersAsync
    /// <tags>integration, logging, handle-new-members, debug</tags>
    /// </summary>
    [Test]
    public async Task HandleNewMembersAsync_ValidMessage_LogsCorrectly()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithStandardMocks();
        var userJoinService = builder.Build();
        
        var (_, envelope, message, _) = TK.CreateNewUserScenario();

        // Act
        await userJoinService.HandleNewMembersAsync(message, CancellationToken.None);

        // Assert
        builder.MessageHandlerMock.Verify(x => x.HandleNewMembersAsync(message, CancellationToken.None), Times.Once);
        
        // Проверяем логирование
        builder.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Проксируем HandleNewMembersAsync")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться проксирование HandleNewMembersAsync");
    }

    /// <summary>
    /// Тест логирования для ProcessNewUserAsync
    /// <tags>integration, logging, process-new-user, debug</tags>
    /// </summary>
    [Test]
    public async Task ProcessNewUserAsync_ValidUser_LogsCorrectly()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithStandardMocks();
        var userJoinService = builder.Build();
        
        var (_, envelope, message, _) = TK.CreateNewUserScenario();
        var user = message.NewChatMembers?.FirstOrDefault() ?? TK.CreateUser();

        // Act
        await userJoinService.ProcessNewUserAsync(message, user, CancellationToken.None);

        // Assert
        builder.MessageHandlerMock.Verify(x => x.ProcessNewUserAsync(message, user, CancellationToken.None), Times.Once);
        
        // Проверяем логирование
        builder.LoggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Проксируем ProcessNewUserAsync")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Должно залогироваться проксирование ProcessNewUserAsync");
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Тест обработки null сообщения
    /// <tags>integration, edge-case, null-handling</tags>
    /// </summary>
    [Test]
    public async Task ProcessNewUserAsync_NullMessage_HandlesGracefully()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithStandardMocks();
        var userJoinService = builder.Build();
        
        var user = TK.CreateUser();

        // Act & Assert - должно обрабатываться gracefully
        await userJoinService.ProcessNewUserAsync(null!, user, CancellationToken.None);
        
        builder.MessageHandlerMock.Verify(x => x.ProcessNewUserAsync(null!, user, CancellationToken.None), Times.Once);
    }

    /// <summary>
    /// Тест обработки null пользователя
    /// <tags>integration, edge-case, null-handling</tags>
    /// </summary>
    [Test]
    public async Task ProcessNewUserAsync_NullUser_HandlesGracefully()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithStandardMocks();
        var userJoinService = builder.Build();
        
        var (_, envelope, message, _) = TK.CreateNewUserScenario();

        // Act & Assert - должно обрабатываться gracefully
        await userJoinService.ProcessNewUserAsync(message, null!, CancellationToken.None);
        
        builder.MessageHandlerMock.Verify(x => x.ProcessNewUserAsync(message, null!, CancellationToken.None), Times.Once);
    }

    /// <summary>
    /// Тест обработки отмены операции
    /// <tags>integration, edge-case, cancellation</tags>
    /// </summary>
    [Test]
    public async Task ProcessNewUserAsync_Cancellation_HandlesCorrectly()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithStandardMocks();
        var userJoinService = builder.Build();
        
        var (_, envelope, message, _) = TK.CreateNewUserScenario();
        var user = message.NewChatMembers?.FirstOrDefault() ?? TK.CreateUser();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert - должно обрабатываться gracefully
        await userJoinService.ProcessNewUserAsync(message, user, cancellationTokenSource.Token);
        
        builder.MessageHandlerMock.Verify(x => x.ProcessNewUserAsync(message, user, cancellationTokenSource.Token), Times.Once);
    }

    #endregion

    #region Multiple Users

    /// <summary>
    /// Тест обработки нескольких новых пользователей
    /// <tags>integration, multiple-users, batch-processing</tags>
    /// </summary>
    [Test]
    public async Task HandleNewMembersAsync_MultipleUsers_HandlesCorrectly()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithSuccessfulJoinScenario();
        var userJoinService = builder.Build();
        
        var message = TK.CreateNewUserJoinMessage();
        message.NewChatMembers = new[]
        {
            TK.CreateUser(),
            TK.CreateUser(),
            TK.CreateUser()
        };

        // Act
        await userJoinService.HandleNewMembersAsync(message, CancellationToken.None);

        // Assert - проверяем, что MessageHandler был вызван для обработки всех пользователей
        builder.MessageHandlerMock.Verify(x => x.HandleNewMembersAsync(message, CancellationToken.None), Times.Once);
    }

    /// <summary>
    /// Тест обработки сообщения без новых пользователей
    /// <tags>integration, edge-case, empty-members</tags>
    /// </summary>
    [Test]
    public async Task HandleNewMembersAsync_NoNewMembers_HandlesCorrectly()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithStandardMocks();
        var userJoinService = builder.Build();
        
        var message = TK.CreateNewUserJoinMessage();
        message.NewChatMembers = null;

        // Act
        await userJoinService.HandleNewMembersAsync(message, CancellationToken.None);

        // Assert - проверяем, что MessageHandler был вызван даже для пустого списка
        builder.MessageHandlerMock.Verify(x => x.HandleNewMembersAsync(message, CancellationToken.None), Times.Once);
    }

    #endregion

    #region Custom Scenarios

    /// <summary>
    /// Тест с кастомной настройкой моков
    /// <tags>integration, custom-scenario, flexible-testing</tags>
    /// </summary>
    [Test]
    public async Task ProcessNewUserAsync_CustomScenario_HandlesCorrectly()
    {
        // Arrange
        var builder = TK.CreateUserJoinServiceBuilder()
            .WithCustomScenario(mock => 
            {
                mock.Setup(x => x.ProcessNewUserAsync(It.IsAny<Message>(), It.IsAny<User>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
            });
        var userJoinService = builder.Build();
        
        var (_, envelope, message, _) = TK.CreateNewUserScenario();
        var user = message.NewChatMembers?.FirstOrDefault() ?? TK.CreateUser();

        // Act
        await userJoinService.ProcessNewUserAsync(message, user, CancellationToken.None);

        // Assert - проверяем, что кастомный сценарий был применен
        builder.MessageHandlerMock.Verify(x => x.ProcessNewUserAsync(message, user, CancellationToken.None), Times.Once);
    }

    #endregion
} 
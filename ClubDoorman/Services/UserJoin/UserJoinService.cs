using ClubDoorman.Handlers;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Services.UserJoin;

/// <summary>
/// Сервис для обработки присоединения новых пользователей
/// <tags>user-join, new-members, captcha, moderation, proxy</tags>
/// </summary>
public class UserJoinService : IUserJoinService
{
    private readonly IMessageHandler _messageHandler;
    private readonly ILogger<UserJoinService> _logger;

    /// <summary>
    /// Создает экземпляр UserJoinService
    /// <tags>user-join, constructor, dependency-injection</tags>
    /// </summary>
    /// <param name="messageHandler">Обработчик сообщений</param>
    /// <param name="logger">Логгер</param>
    public UserJoinService(IMessageHandler messageHandler, ILogger<UserJoinService> logger)
    {
        _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Обрабатывает присоединение новых пользователей
    /// <tags>user-join, new-members, processing, proxy</tags>
    /// </summary>
    /// <param name="message">Сообщение о новых участниках</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task HandleNewMembersAsync(Message message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("UserJoinService: Проксируем HandleNewMembersAsync к MessageHandler");
        await _messageHandler.HandleNewMembersAsync(message, cancellationToken);
    }

    /// <summary>
    /// Обрабатывает одного нового пользователя
    /// <tags>user-join, single-user, processing, proxy</tags>
    /// </summary>
    /// <param name="userJoinMessage">Сообщение о присоединении</param>
    /// <param name="user">Пользователь</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task ProcessNewUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("UserJoinService: Проксируем ProcessNewUserAsync к MessageHandler");
        await _messageHandler.ProcessNewUserAsync(userJoinMessage, user, cancellationToken);
    }
} 
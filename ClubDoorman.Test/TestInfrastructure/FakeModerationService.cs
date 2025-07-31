using ClubDoorman.Models;
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Фейковый сервис модерации для тестирования
/// Позволяет настраивать результаты модерации сообщений
/// </summary>
public class FakeModerationService : IModerationService
{
    private readonly ILogger<FakeModerationService> _logger;
    private readonly ISpamHamClassifier _classifier;
    private readonly IMimicryClassifier _mimicryClassifier;
    private readonly IBadMessageManager _badMessageManager;
    private readonly IUserManager _userManager;
    private readonly IAiChecks _aiChecks;
    private readonly ISuspiciousUsersStorage _suspiciousUsersStorage;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IMessageService _messageService;

    // Настраиваемые результаты
    public ModerationResult NextResult { get; set; } = new ModerationResult(ModerationAction.Allow, "Безопасно");
    public TimeSpan ResponseTime { get; set; } = TimeSpan.FromMilliseconds(100);
    public bool ShouldThrowException { get; set; } = false;
    public Exception? ExceptionToThrow { get; set; }

    // История вызовов
    public List<ModerationRequest> ModerationRequests { get; } = new();

    public FakeModerationService(
        ISpamHamClassifier classifier,
        IMimicryClassifier mimicryClassifier,
        IBadMessageManager badMessageManager,
        IUserManager userManager,
        IAiChecks aiChecks,
        ISuspiciousUsersStorage suspiciousUsersStorage,
        ITelegramBotClientWrapper bot,
        IMessageService messageService,
        ILogger<FakeModerationService> logger)
    {
        _classifier = classifier;
        _mimicryClassifier = mimicryClassifier;
        _badMessageManager = badMessageManager;
        _userManager = userManager;
        _aiChecks = aiChecks;
        _suspiciousUsersStorage = suspiciousUsersStorage;
        _bot = bot;
        _messageService = messageService;
        _logger = logger;
    }

    /// <summary>
    /// Настройка результата модерации
    /// </summary>
    public FakeModerationService SetResult(ModerationResult result)
    {
        NextResult = result;
        return this;
    }

    /// <summary>
    /// Настройка времени ответа
    /// </summary>
    public FakeModerationService SetResponseTime(TimeSpan responseTime)
    {
        ResponseTime = responseTime;
        return this;
    }

    /// <summary>
    /// Настройка исключения
    /// </summary>
    public FakeModerationService SetException(Exception exception)
    {
        ShouldThrowException = true;
        ExceptionToThrow = exception;
        return this;
    }

    /// <summary>
    /// Сброс настроек к значениям по умолчанию
    /// </summary>
    public FakeModerationService Reset()
    {
        NextResult = new ModerationResult(ModerationAction.Allow, "Безопасно");
        ResponseTime = TimeSpan.FromMilliseconds(100);
        ShouldThrowException = false;
        ExceptionToThrow = null;
        return this;
    }

    /// <summary>
    /// Очистка истории
    /// </summary>
    public FakeModerationService ClearHistory()
    {
        ModerationRequests.Clear();
        return this;
    }

    public async Task<ModerationResult> CheckMessageAsync(Message message)
    {
        var request = new ModerationRequest
        {
            MessageId = message.MessageId,
            ChatId = message.Chat?.Id ?? 0,
            UserId = message.From?.Id ?? 0,
            Text = message.Text ?? "",
            Timestamp = DateTime.UtcNow
        };

        ModerationRequests.Add(request);

        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake moderation exception");
        }

        await Task.Delay(ResponseTime);

        _logger.LogInformation("FakeModerationService: модерация сообщения {MessageId} от пользователя {UserId}, действие: {Action}", 
            message.MessageId, message.From?.Id, NextResult.Action);

        return NextResult;
    }

    public async Task<ModerationResult> CheckUserNameAsync(User user)
    {
        var request = new ModerationRequest
        {
            UserId = user.Id,
            Text = $"{user.FirstName} {user.LastName}",
            Timestamp = DateTime.UtcNow
        };

        ModerationRequests.Add(request);

        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake user moderation exception");
        }

        await Task.Delay(ResponseTime);

        _logger.LogInformation("FakeModerationService: модерация пользователя {UserId}, действие: {Action}", 
            user.Id, NextResult.Action);

        return NextResult;
    }

    public async Task ExecuteModerationActionAsync(Message message, ModerationResult result)
    {
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake moderation action exception");
        }

        await Task.Delay(ResponseTime);
        _logger.LogInformation("FakeModerationService: выполнено действие модерации для сообщения {MessageId}", message.MessageId);
    }

    public bool IsUserApproved(long userId, long? chatId = null)
    {
        return NextResult.Action == ModerationAction.Allow;
    }

    public async Task IncrementGoodMessageCountAsync(User user, Chat chat, string messageText)
    {
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake increment good message exception");
        }

        await Task.Delay(ResponseTime);
        _logger.LogInformation("FakeModerationService: увеличен счетчик хороших сообщений для пользователя {UserId}", user.Id);
    }

    public bool SetAiDetectForSuspiciousUser(long userId, long chatId, bool enabled)
    {
        _logger.LogInformation("FakeModerationService: установлен AI-детект для пользователя {UserId} в чате {ChatId}: {Enabled}", 
            userId, chatId, enabled);
        return true;
    }

    public (int TotalSuspicious, int WithAiDetect, int GroupsCount) GetSuspiciousUsersStats()
    {
        return (10, 5, 3); // Фейковая статистика
    }

    public List<(long UserId, long ChatId)> GetAiDetectUsers()
    {
        return new List<(long, long)> { (12345, -100123456789) }; // Фейковый список
    }

    public async Task<bool> CheckAiDetectAndNotifyAdminsAsync(User user, Chat chat, Message message)
    {
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake AI detect check exception");
        }

        await Task.Delay(ResponseTime);
        _logger.LogInformation("FakeModerationService: проверен AI-детект для пользователя {UserId}", user.Id);
        return NextResult.Action != ModerationAction.Allow;
    }

    public async Task<bool> UnrestrictAndApproveUserAsync(long userId, long chatId)
    {
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake unrestrict and approve exception");
        }

        await Task.Delay(ResponseTime);
        _logger.LogInformation("FakeModerationService: сняты ограничения и одобрен пользователь {UserId} в чате {ChatId}", 
            userId, chatId);
        return true;
    }

    public void CleanupUserFromAllLists(long userId, long chatId)
    {
        _logger.LogInformation("FakeModerationService: очищен пользователь {UserId} из всех списков в чате {ChatId}", 
            userId, chatId);
    }

    public async Task<bool> BanAndCleanupUserAsync(long userId, long chatId, int? messageIdToDelete = null)
    {
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake ban and cleanup exception");
        }

        await Task.Delay(ResponseTime);
        _logger.LogInformation("FakeModerationService: забанен и очищен пользователь {UserId} в чате {ChatId}", 
            userId, chatId);
        return true;
    }
}

/// <summary>
/// Запрос на модерацию
/// </summary>
public record ModerationRequest
{
    public int MessageId { get; init; }
    public long ChatId { get; init; }
    public long UserId { get; init; }
    public string Text { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
} 
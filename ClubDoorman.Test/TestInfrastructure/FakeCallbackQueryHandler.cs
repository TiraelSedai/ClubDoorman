using ClubDoorman.Handlers;
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Фейковый обработчик callback query для тестирования
/// Позволяет настраивать результаты обработки кнопок
/// </summary>
public class FakeCallbackQueryHandler : ICallbackQueryHandler
{
    private readonly ILogger<FakeCallbackQueryHandler> _logger;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IUserManager _userManager;
    private readonly IAppConfig _appConfig;

    // Настраиваемые результаты
    public bool ShouldAnswerCallback { get; set; } = true;
    public bool ShouldThrowException { get; set; } = false;
    public Exception? ExceptionToThrow { get; set; }
    public TimeSpan ResponseTime { get; set; } = TimeSpan.FromMilliseconds(100);

    // История вызовов
    public List<CallbackQueryRequest> CallbackRequests { get; } = new();
    public List<CallbackQueryResult> CallbackResults { get; } = new();

    public FakeCallbackQueryHandler(
        ITelegramBotClientWrapper bot,
        ILogger<FakeCallbackQueryHandler> logger,
        IUserManager userManager,
        IAppConfig appConfig)
    {
        _bot = bot;
        _logger = logger;
        _userManager = userManager;
        _appConfig = appConfig;
    }

    /// <summary>
    /// Настройка ответа на callback
    /// </summary>
    public FakeCallbackQueryHandler SetShouldAnswerCallback(bool shouldAnswer)
    {
        ShouldAnswerCallback = shouldAnswer;
        return this;
    }

    /// <summary>
    /// Настройка исключения
    /// </summary>
    public FakeCallbackQueryHandler SetException(Exception exception)
    {
        ShouldThrowException = true;
        ExceptionToThrow = exception;
        return this;
    }

    /// <summary>
    /// Настройка времени ответа
    /// </summary>
    public FakeCallbackQueryHandler SetResponseTime(TimeSpan responseTime)
    {
        ResponseTime = responseTime;
        return this;
    }

    /// <summary>
    /// Сброс настроек к значениям по умолчанию
    /// </summary>
    public FakeCallbackQueryHandler Reset()
    {
        ShouldAnswerCallback = true;
        ShouldThrowException = false;
        ExceptionToThrow = null;
        ResponseTime = TimeSpan.FromMilliseconds(100);
        return this;
    }

    /// <summary>
    /// Очистка истории
    /// </summary>
    public FakeCallbackQueryHandler ClearHistory()
    {
        CallbackRequests.Clear();
        CallbackResults.Clear();
        return this;
    }

    public bool CanHandle(CallbackQuery callbackQuery)
    {
        // Фейковый обработчик может обработать любой callback
        return true;
    }

    public async Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var request = new CallbackQueryRequest
        {
            CallbackQueryId = callbackQuery.Id,
            Data = callbackQuery.Data,
            FromUserId = callbackQuery.From?.Id ?? 0,
            ChatId = callbackQuery.Message?.Chat?.Id ?? 0,
            Timestamp = DateTime.UtcNow
        };

        CallbackRequests.Add(request);

        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake callback query exception");
        }

        // Симулируем время обработки
        await Task.Delay(ResponseTime, cancellationToken);

        var result = new CallbackQueryResult
        {
            CallbackQueryId = callbackQuery.Id,
            Data = callbackQuery.Data,
            WasAnswered = ShouldAnswerCallback,
            Success = true,
            Timestamp = DateTime.UtcNow
        };

        CallbackResults.Add(result);

        // Симулируем обработку различных типов callback
        if (callbackQuery.Data != null)
        {
            if (callbackQuery.Data.StartsWith("approve_user_"))
            {
                var userId = long.Parse(callbackQuery.Data.Replace("approve_user_", ""));
                await HandleApproveUser(userId, callbackQuery, cancellationToken);
            }
            else if (callbackQuery.Data.StartsWith("ban_user_"))
            {
                var userId = long.Parse(callbackQuery.Data.Replace("ban_user_", ""));
                await HandleBanUser(userId, callbackQuery, cancellationToken);
            }
            else if (callbackQuery.Data.StartsWith("skip_user_"))
            {
                var userId = long.Parse(callbackQuery.Data.Replace("skip_user_", ""));
                await HandleSkipUser(userId, callbackQuery, cancellationToken);
            }
        }

        if (ShouldAnswerCallback)
        {
            await _bot.AnswerCallbackQuery(callbackQuery.Id, "Обработано", cancellationToken: cancellationToken);
        }

        _logger.LogInformation("FakeCallbackQueryHandler: обработан callback {CallbackId}, данные: {Data}, ответ: {Answered}", 
            callbackQuery.Id, callbackQuery.Data, ShouldAnswerCallback ? "да" : "нет");
    }

    private async Task HandleApproveUser(long userId, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        // Симулируем одобрение пользователя
        _logger.LogInformation("FakeCallbackQueryHandler: одобрен пользователь {UserId}", userId);
        await Task.CompletedTask;
    }

    private async Task HandleBanUser(long userId, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        // Симулируем бан пользователя
        _logger.LogInformation("FakeCallbackQueryHandler: забанен пользователь {UserId}", userId);
        await Task.CompletedTask;
    }

    private async Task HandleSkipUser(long userId, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        // Симулируем пропуск пользователя
        _logger.LogInformation("FakeCallbackQueryHandler: пропущен пользователь {UserId}", userId);
        await Task.CompletedTask;
    }
}

/// <summary>
/// Запрос на обработку callback query
/// </summary>
public record CallbackQueryRequest
{
    public string CallbackQueryId { get; init; } = string.Empty;
    public string? Data { get; init; }
    public long FromUserId { get; init; }
    public long ChatId { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Результат обработки callback query
/// </summary>
public record CallbackQueryResult
{
    public string CallbackQueryId { get; init; } = string.Empty;
    public string? Data { get; init; }
    public bool WasAnswered { get; init; }
    public bool Success { get; init; }
    public DateTime Timestamp { get; init; }
} 
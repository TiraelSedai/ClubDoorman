using ClubDoorman.Models;
using ClubDoorman.Models.Requests;
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Фейковый сервис капчи для тестирования
/// Позволяет настраивать результаты: успех, неудача, таймаут
/// </summary>
public class FakeCaptchaService : ICaptchaService
{
    private readonly ILogger<FakeCaptchaService> _logger;
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IMessageService _messageService;
    private readonly IAppConfig _appConfig;

    // Настраиваемые результаты
    public bool NextResult { get; set; } = true;
    public TimeSpan ResponseTime { get; set; } = TimeSpan.FromSeconds(1);
    public bool ShouldThrowException { get; set; } = false;
    public Exception? ExceptionToThrow { get; set; }

    // История вызовов
    public List<CaptchaRequest> CaptchaRequests { get; } = new();

    public FakeCaptchaService(
        ITelegramBotClientWrapper bot,
        ILogger<FakeCaptchaService> logger,
        IMessageService messageService,
        IAppConfig appConfig)
    {
        _bot = bot;
        _logger = logger;
        _messageService = messageService;
        _appConfig = appConfig;
    }

    /// <summary>
    /// Настройка результата капчи
    /// </summary>
    public FakeCaptchaService SetResult(bool result)
    {
        NextResult = result;
        return this;
    }

    /// <summary>
    /// Настройка времени ответа
    /// </summary>
    public FakeCaptchaService SetResponseTime(TimeSpan responseTime)
    {
        ResponseTime = responseTime;
        return this;
    }

    /// <summary>
    /// Настройка исключения
    /// </summary>
    public FakeCaptchaService SetException(Exception exception)
    {
        ShouldThrowException = true;
        ExceptionToThrow = exception;
        return this;
    }

    /// <summary>
    /// Сброс настроек к значениям по умолчанию
    /// </summary>
    public FakeCaptchaService Reset()
    {
        NextResult = true;
        ResponseTime = TimeSpan.FromSeconds(1);
        ShouldThrowException = false;
        ExceptionToThrow = null;
        return this;
    }

    /// <summary>
    /// Очистка истории
    /// </summary>
    public FakeCaptchaService ClearHistory()
    {
        CaptchaRequests.Clear();
        return this;
    }

    public async Task<CaptchaInfo?> CreateCaptchaAsync(CreateCaptchaRequest request)
    {
        var captchaRequest = new CaptchaRequest
        {
            UserId = request.User.Id,
            ChatId = request.Chat.Id,
            Timestamp = DateTime.UtcNow
        };

        CaptchaRequests.Add(captchaRequest);

        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake captcha exception");
        }

        // Симулируем время обработки
        await Task.Delay(ResponseTime);

        if (!NextResult)
        {
            return null; // Капча отключена
        }

        var captchaInfo = new CaptchaInfo(
            request.Chat.Id,
            request.Chat.Title,
            DateTime.UtcNow,
            request.User,
            1, // Правильный ответ
            new CancellationTokenSource(),
            null
        );

        _logger.LogInformation("FakeCaptchaService: создана капча для пользователя {UserId} в чате {ChatId}", 
            request.User.Id, request.Chat.Id);

        return captchaInfo;
    }

    public string GenerateKey(long chatId, long userId)
    {
        return $"fake_captcha_{chatId}_{userId}_{Guid.NewGuid():N}";
    }

    public CaptchaInfo? GetCaptchaInfo(string key)
    {
        // Для тестов AI-анализа возвращаем null, чтобы пользователь не считался в процессе капчи
        // Это позволяет MessageHandler продолжить обработку и вызвать AI-анализ
        return null;
    }

    public bool RemoveCaptcha(string key)
    {
        _logger.LogInformation("FakeCaptchaService: удалена капча {Key}", key);
        return true;
    }

    public async Task<bool> ValidateCaptchaAsync(string key, int answer)
    {
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake captcha validation exception");
        }

        await Task.Delay(ResponseTime);
        return NextResult;
    }

    public async Task BanExpiredCaptchaUsersAsync()
    {
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Fake ban expired users exception");
        }

        await Task.Delay(ResponseTime);
        _logger.LogInformation("FakeCaptchaService: забанены пользователи с истекшей капчей");
    }
}

/// <summary>
/// Запрос на создание капчи
/// </summary>
public record CaptchaRequest
{
    public long UserId { get; init; }
    public long ChatId { get; init; }
    public DateTime Timestamp { get; init; }
} 
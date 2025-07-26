using ClubDoorman.Services;
using ClubDoorman.Models;
using ClubDoorman.Infrastructure;
using Telegram.Bot.Types;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Фабрика для создания Mock AI сервисов для unit тестов
/// Позволяет тестировать логику без реальных API вызовов
/// </summary>
public static class MockAiChecksFactory
{
    /// <summary>
    /// Создает Mock AI сервис с предустановленными ответами
    /// </summary>
    public static IAiChecks CreateMockAiChecks(
        double spamProbability = 0.5,
        double attentionBaitProbability = 0.3,
        double eroticPhotoBaitProbability = 0.1,
        double suspiciousUserSpamProbability = 0.4,
        bool shouldThrowException = false,
        Exception? exceptionToThrow = null)
    {
        return new MockAiChecks(
            spamProbability,
            attentionBaitProbability,
            eroticPhotoBaitProbability,
            suspiciousUserSpamProbability,
            shouldThrowException,
            exceptionToThrow
        );
    }

    /// <summary>
    /// Создает Mock AI сервис для сценария "спам"
    /// </summary>
    public static IAiChecks CreateSpamScenario()
    {
        return CreateMockAiChecks(
            spamProbability: 0.9,
            attentionBaitProbability: 0.8,
            eroticPhotoBaitProbability: 0.7,
            suspiciousUserSpamProbability: 0.9
        );
    }

    /// <summary>
    /// Создает Mock AI сервис для сценария "нормальное сообщение"
    /// </summary>
    public static IAiChecks CreateNormalScenario()
    {
        return CreateMockAiChecks(
            spamProbability: 0.1,
            attentionBaitProbability: 0.1,
            eroticPhotoBaitProbability: 0.05,
            suspiciousUserSpamProbability: 0.1
        );
    }

    /// <summary>
    /// Создает Mock AI сервис для сценария "подозрительный пользователь"
    /// </summary>
    public static IAiChecks CreateSuspiciousUserScenario()
    {
        return CreateMockAiChecks(
            spamProbability: 0.3,
            attentionBaitProbability: 0.2,
            eroticPhotoBaitProbability: 0.1,
            suspiciousUserSpamProbability: 0.8
        );
    }

    /// <summary>
    /// Создает Mock AI сервис для сценария "ошибка API"
    /// </summary>
    public static IAiChecks CreateErrorScenario(Exception? exception = null)
    {
        return CreateMockAiChecks(
            shouldThrowException: true,
            exceptionToThrow: exception ?? new AiServiceException("Mock API error")
        );
    }
}

/// <summary>
/// Mock реализация IAiChecks для unit тестов
/// </summary>
public class MockAiChecks : IAiChecks
{
    private readonly double _spamProbability;
    private readonly double _attentionBaitProbability;
    private readonly double _eroticPhotoBaitProbability;
    private readonly double _suspiciousUserSpamProbability;
    private readonly bool _shouldThrowException;
    private readonly Exception? _exceptionToThrow;

    public MockAiChecks(
        double spamProbability,
        double attentionBaitProbability,
        double eroticPhotoBaitProbability,
        double suspiciousUserSpamProbability,
        bool shouldThrowException = false,
        Exception? exceptionToThrow = null)
    {
        _spamProbability = spamProbability;
        _attentionBaitProbability = attentionBaitProbability;
        _eroticPhotoBaitProbability = eroticPhotoBaitProbability;
        _suspiciousUserSpamProbability = suspiciousUserSpamProbability;
        _shouldThrowException = shouldThrowException;
        _exceptionToThrow = exceptionToThrow;
    }

    public void MarkUserOkay(long userId)
    {
        if (_shouldThrowException)
            throw _exceptionToThrow ?? new AiServiceException("Mock error");
        
        // Mock implementation - ничего не делаем
    }

    public ValueTask<SpamPhotoBio> GetAttentionBaitProbability(User user, Func<string, Task>? ifChanged = default)
    {
        if (_shouldThrowException)
            throw _exceptionToThrow ?? new AiServiceException("Mock error");

        var spamProbability = new SpamProbability 
        { 
            Probability = _attentionBaitProbability, 
            Reason = "Mock attention bait analysis" 
        };
        
        return ValueTask.FromResult(new SpamPhotoBio(spamProbability, new byte[0], "Mock user bio"));
    }

    public ValueTask<SpamPhotoBio> GetAttentionBaitProbability(User user, string? messageText, Func<string, Task>? ifChanged = default)
    {
        if (_shouldThrowException)
            throw _exceptionToThrow ?? new AiServiceException("Mock error");

        var spamProbability = new SpamProbability 
        { 
            Probability = _attentionBaitProbability, 
            Reason = "Mock attention bait analysis with message" 
        };
        
        return ValueTask.FromResult(new SpamPhotoBio(spamProbability, new byte[0], "Mock user bio"));
    }

    public ValueTask<SpamProbability> GetSpamProbability(Message message)
    {
        if (_shouldThrowException)
            throw _exceptionToThrow ?? new AiServiceException("Mock error");

        return ValueTask.FromResult(new SpamProbability 
        { 
            Probability = _spamProbability, 
            Reason = "Mock spam analysis" 
        });
    }

    public ValueTask<SpamProbability> GetSuspiciousUserSpamProbability(
        Message message, 
        User user, 
        List<string> firstMessages, 
        double mimicryScore)
    {
        if (_shouldThrowException)
            throw _exceptionToThrow ?? new AiServiceException("Mock error");

        return ValueTask.FromResult(new SpamProbability 
        { 
            Probability = _suspiciousUserSpamProbability, 
            Reason = "Mock suspicious user analysis" 
        });
    }
} 
using Telegram.Bot.Types;
using ClubDoorman.Models;

namespace ClubDoorman.Test.TestKit.Builders;

/// <summary>
/// Builders для создания тестовых данных в читаемом виде
/// <tags>builders, fluent-api, test-data, readable-tests</tags>
/// </summary>
public static class TestKitBuilders
{
    /// <summary>
    /// Создает builder для сообщения Telegram
    /// <tags>builders, message, telegram, fluent-api</tags>
    /// </summary>
    public static MessageBuilder CreateMessage() => new MessageBuilder();
    
    /// <summary>
    /// Создает builder для пользователя Telegram
    /// <tags>builders, user, telegram, fluent-api</tags>
    /// </summary>
    public static UserBuilder CreateUser() => new UserBuilder();
    
    /// <summary>
    /// Создает builder для чата Telegram
    /// <tags>builders, chat, telegram, fluent-api</tags>
    /// </summary>
    public static ChatBuilder CreateChat() => new ChatBuilder();
    
    /// <summary>
    /// Создает builder для результата модерации
    /// <tags>builders, moderation-result, moderation, fluent-api</tags>
    /// </summary>
    public static ModerationResultBuilder CreateModerationResult() => new ModerationResultBuilder();
} 
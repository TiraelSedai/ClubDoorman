using Telegram.Bot.Types;
using ClubDoorman.Test.TestKit;

namespace ClubDoorman.Test.TestKit.Builders;

/// <summary>
/// Builder для создания сложных тестовых сценариев
/// <tags>builders, scenario, telegram, fluent-api</tags>
/// </summary>
public class ScenarioBuilder
{
    private User? _user;
    private Chat? _chat;
    private Message? _message;
    
    /// <summary>
    /// Настраивает пользователя для сценария
    /// <tags>builders, scenario, user, fluent-api</tags>
    /// </summary>
    public ScenarioBuilder WithUser(Action<UserBuilder> configure)
    {
        var userBuilder = new UserBuilder();
        configure(userBuilder);
        _user = userBuilder.Build();
        return this;
    }
    
    /// <summary>
    /// Настраивает чат для сценария
    /// <tags>builders, scenario, chat, fluent-api</tags>
    /// </summary>
    public ScenarioBuilder WithChat(Action<ChatBuilder> configure)
    {
        var chatBuilder = new ChatBuilder();
        configure(chatBuilder);
        _chat = chatBuilder.Build();
        return this;
    }
    
    /// <summary>
    /// Настраивает сообщение для сценария
    /// <tags>builders, scenario, message, fluent-api</tags>
    /// </summary>
    public ScenarioBuilder WithMessage(Action<MessageBuilder> configure)
    {
        var messageBuilder = new MessageBuilder();
        configure(messageBuilder);
        _message = messageBuilder.Build();
        return this;
    }
    
    /// <summary>
    /// Создает спам-сценарий (подозрительный пользователь + спам-сообщение)
    /// <tags>builders, scenario, spam, fluent-api</tags>
    /// </summary>
    public ScenarioBuilder AsSpamScenario()
    {
        _user = TK.BuildUser().AsSuspicious().Build();
        _chat = TK.BuildChat().AsGroup().Build();
        _message = TK.BuildMessage()
            .AsSpam()
            .FromUser(_user)
            .InChat(_chat)
            .Build();
        return this;
    }
    
    /// <summary>
    /// Создает сценарий бана (нарушающий пользователь + причина бана)
    /// <tags>builders, scenario, ban, fluent-api</tags>
    /// </summary>
    public ScenarioBuilder AsBanScenario()
    {
        _user = TK.BuildUser().AsViolating().Build();
        _chat = TK.BuildChat().AsGroup().Build();
        _message = TK.BuildMessage()
            .AsSpam()
            .FromUser(_user)
            .InChat(_chat)
            .Build();
        return this;
    }
    
    /// <summary>
    /// Создает сценарий капчи (новый пользователь + требование капчи)
    /// <tags>builders, scenario, captcha, fluent-api</tags>
    /// </summary>
    public ScenarioBuilder AsCaptchaScenario()
    {
        _user = TK.BuildUser().AsNew().Build();
        _chat = TK.BuildChat().AsGroup().WithCaptchaRequired().Build();
        _message = TK.BuildMessage()
            .AsValid()
            .FromUser(_user)
            .InChat(_chat)
            .Build();
        return this;
    }
    
    /// <summary>
    /// Создает сценарий канала (сообщение от канала)
    /// <tags>builders, scenario, channel, fluent-api</tags>
    /// </summary>
    public ScenarioBuilder AsChannelScenario()
    {
        _chat = TK.BuildChat().AsChannel().Build();
        _message = TK.BuildMessage()
            .AsValid()
            .FromChannel()
            .InChat(_chat)
            .Build();
        return this;
    }
    
    /// <summary>
    /// Строит сценарий
    /// <tags>builders, scenario, build, fluent-api</tags>
    /// </summary>
    public TestScenario Build()
    {
        // Если компоненты не заданы, создаем дефолтные
        _user ??= TK.CreateValidUser();
        _chat ??= TK.CreateGroupChat();
        _message ??= TK.BuildMessage()
            .FromUser(_user)
            .InChat(_chat)
            .Build();
            
        return new TestScenario
        {
            User = _user,
            Chat = _chat,
            Message = _message
        };
    }
}

/// <summary>
/// Результат построения тестового сценария
/// <tags>scenario, test-data, telegram</tags>
/// </summary>
public class TestScenario
{
    public User User { get; init; } = null!;
    public Chat Chat { get; init; } = null!;
    public Message Message { get; init; } = null!;
}
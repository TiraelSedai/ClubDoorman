using Telegram.Bot.Types;
using ClubDoorman.Test.TestKit;

namespace ClubDoorman.Test.TestKit.Builders;

/// <summary>
/// Builder для создания сообщений Telegram
/// <tags>builders, message, telegram, fluent-api</tags>
/// </summary>
public class MessageBuilder
{
    private Message _message = TestKitBogus.CreateRealisticMessage();
    
    /// <summary>
    /// Устанавливает текст сообщения
    /// <tags>builders, message, text, fluent-api</tags>
    /// </summary>
    public MessageBuilder WithText(string text)
    {
        _message.Text = text;
        return this;
    }
    
    /// <summary>
    /// Устанавливает отправителя сообщения
    /// <tags>builders, message, user, fluent-api</tags>
    /// </summary>
    public MessageBuilder FromUser(long userId)
    {
        _message.From = TestKitBogus.CreateRealisticUser(userId);
        return this;
    }
    
    /// <summary>
    /// Устанавливает отправителя сообщения (полный объект)
    /// <tags>builders, message, user, fluent-api</tags>
    /// </summary>
    public MessageBuilder FromUser(User user)
    {
        _message.From = user;
        return this;
    }
    
    /// <summary>
    /// Устанавливает чат
    /// <tags>builders, message, chat, fluent-api</tags>
    /// </summary>
    public MessageBuilder InChat(long chatId)
    {
        _message.Chat = TestKitBogus.CreateRealisticGroup();
        _message.Chat.Id = chatId;
        return this;
    }
    
    /// <summary>
    /// Устанавливает чат (полный объект)
    /// <tags>builders, message, chat, fluent-api</tags>
    /// </summary>
    public MessageBuilder InChat(Chat chat)
    {
        _message.Chat = chat;
        return this;
    }
    

    
    /// <summary>
    /// Устанавливает сообщение как спам
    /// <tags>builders, message, spam, fluent-api</tags>
    /// </summary>
    public MessageBuilder AsSpam()
    {
        _message.Text = "🔥💰🎁 Make money fast! 💰🔥🎁";
        return this;
    }
    
    /// <summary>
    /// Устанавливает сообщение как валидное
    /// <tags>builders, message, valid, fluent-api</tags>
    /// </summary>
    public MessageBuilder AsValid()
    {
        _message.Text = "Hello, this is a valid message!";
        return this;
    }
    
    /// <summary>
    /// Строит сообщение
    /// <tags>builders, message, build, fluent-api</tags>
    /// </summary>
    public Message Build() => _message;
    
    /// <summary>
    /// Неявное преобразование в Message
    /// <tags>builders, message, conversion, fluent-api</tags>
    /// </summary>
    public static implicit operator Message(MessageBuilder builder) => builder.Build();
} 
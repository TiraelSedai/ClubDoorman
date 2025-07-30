using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Models;

namespace ClubDoorman.Test.TestKit;

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
    /// Устанавливает ID сообщения (только для чтения, не изменяет)
    /// <tags>builders, message, id, fluent-api</tags>
    /// </summary>
    public MessageBuilder WithMessageId(int messageId)
    {
        // MessageId readonly, нельзя изменить после создания
        // Используем TestKitBogus.CreateRealisticMessage() с нужным ID
        _message = TestKitBogus.CreateRealisticMessage();
        // Note: MessageId остается 0, так как это readonly свойство
        return this;
    }
    
    /// <summary>
    /// Делает сообщение спамом
    /// <tags>builders, message, spam, moderation, fluent-api</tags>
    /// </summary>
    public MessageBuilder AsSpam()
    {
        _message.Text = TestKitBogus.CreateRealisticSpamMessage().Text;
        return this;
    }
    
    /// <summary>
    /// Делает сообщение валидным
    /// </summary>
    public MessageBuilder AsValid()
    {
        _message.Text = "Hello, this is a valid message!";
        return this;
    }
    
    /// <summary>
    /// Создает сообщение
    /// </summary>
    public Message Build() => _message;
    
    /// <summary>
    /// Неявное преобразование в Message
    /// </summary>
    public static implicit operator Message(MessageBuilder builder) => builder.Build();
}

/// <summary>
/// Builder для создания пользователей Telegram
/// </summary>
public class UserBuilder
{
    private User _user = TestKitBogus.CreateRealisticUser();
    
    /// <summary>
    /// Устанавливает ID пользователя
    /// </summary>
    public UserBuilder WithId(long userId)
    {
        _user.Id = userId;
        return this;
    }
    
    /// <summary>
    /// Устанавливает имя пользователя
    /// </summary>
    public UserBuilder WithUsername(string username)
    {
        _user.Username = username;
        return this;
    }
    
    /// <summary>
    /// Устанавливает имя
    /// </summary>
    public UserBuilder WithFirstName(string firstName)
    {
        _user.FirstName = firstName;
        return this;
    }
    
    /// <summary>
    /// Делает пользователя ботом
    /// </summary>
    public UserBuilder AsBot()
    {
        _user.IsBot = true;
        return this;
    }
    
    /// <summary>
    /// Делает пользователя обычным пользователем
    /// </summary>
    public UserBuilder AsRegularUser()
    {
        _user.IsBot = false;
        return this;
    }
    
    /// <summary>
    /// Создает пользователя
    /// </summary>
    public User Build() => _user;
    
    /// <summary>
    /// Неявное преобразование в User
    /// </summary>
    public static implicit operator User(UserBuilder builder) => builder.Build();
}

/// <summary>
/// Builder для создания чатов Telegram
/// </summary>
public class ChatBuilder
{
    private Chat _chat = TestKitBogus.CreateRealisticGroup();
    
    /// <summary>
    /// Устанавливает ID чата
    /// </summary>
    public ChatBuilder WithId(long chatId)
    {
        _chat.Id = chatId;
        return this;
    }
    
    /// <summary>
    /// Устанавливает тип чата
    /// </summary>
    public ChatBuilder WithType(ChatType chatType)
    {
        _chat.Type = chatType;
        return this;
    }
    
    /// <summary>
    /// Устанавливает название чата
    /// </summary>
    public ChatBuilder WithTitle(string title)
    {
        _chat.Title = title;
        return this;
    }
    
    /// <summary>
    /// Делает чат группой
    /// </summary>
    public ChatBuilder AsGroup()
    {
        _chat.Type = ChatType.Group;
        return this;
    }
    
    /// <summary>
    /// Делает чат супергруппой
    /// </summary>
    public ChatBuilder AsSupergroup()
    {
        _chat.Type = ChatType.Supergroup;
        return this;
    }
    
    /// <summary>
    /// Делает чат приватным
    /// </summary>
    public ChatBuilder AsPrivate()
    {
        _chat.Type = ChatType.Private;
        return this;
    }
    
    /// <summary>
    /// Создает чат
    /// </summary>
    public Chat Build() => _chat;
    
    /// <summary>
    /// Неявное преобразование в Chat
    /// </summary>
    public static implicit operator Chat(ChatBuilder builder) => builder.Build();
}

/// <summary>
/// Builder для создания результатов модерации
/// </summary>
public class ModerationResultBuilder
{
    private ModerationAction _action = ModerationAction.Allow;
    private string _reason = "Valid message";
    private double? _confidence = null;
    
    /// <summary>
    /// Устанавливает действие модерации
    /// </summary>
    public ModerationResultBuilder WithAction(ModerationAction action)
    {
        _action = action;
        return this;
    }
    
    /// <summary>
    /// Устанавливает причину
    /// </summary>
    public ModerationResultBuilder WithReason(string reason)
    {
        _reason = reason;
        return this;
    }
    
    /// <summary>
    /// Устанавливает уровень уверенности
    /// </summary>
    public ModerationResultBuilder WithConfidence(double confidence)
    {
        _confidence = confidence;
        return this;
    }
    
    /// <summary>
    /// Делает результат "Разрешить"
    /// </summary>
    public ModerationResultBuilder AsAllow()
    {
        _action = ModerationAction.Allow;
        _reason = "Valid message";
        return this;
    }
    
    /// <summary>
    /// Делает результат "Удалить"
    /// </summary>
    public ModerationResultBuilder AsDelete()
    {
        _action = ModerationAction.Delete;
        _reason = "Spam detected";
        return this;
    }
    
    /// <summary>
    /// Делает результат "Забанить"
    /// </summary>
    public ModerationResultBuilder AsBan()
    {
        _action = ModerationAction.Ban;
        _reason = "User banned";
        return this;
    }
    
    /// <summary>
    /// Создает результат модерации
    /// </summary>
    public ModerationResult Build() => new ModerationResult(_action, _reason, _confidence);
    
    /// <summary>
    /// Неявное преобразование в ModerationResult
    /// </summary>
    public static implicit operator ModerationResult(ModerationResultBuilder builder) => builder.Build();
} 
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ClubDoorman.Test.TestKit;

namespace ClubDoorman.Test.TestKit.Builders;

/// <summary>
/// Builder для создания чатов Telegram
/// <tags>builders, chat, telegram, fluent-api</tags>
/// </summary>
public class ChatBuilder
{
    private Chat _chat = TestKitBogus.CreateRealisticGroup();
    
    /// <summary>
    /// Устанавливает ID чата
    /// <tags>builders, chat, id, fluent-api</tags>
    /// </summary>
    public ChatBuilder WithId(long chatId)
    {
        _chat.Id = chatId;
        return this;
    }
    
    /// <summary>
    /// Устанавливает тип чата
    /// <tags>builders, chat, type, fluent-api</tags>
    /// </summary>
    public ChatBuilder WithType(ChatType chatType)
    {
        _chat.Type = chatType;
        return this;
    }
    
    /// <summary>
    /// Устанавливает название чата
    /// <tags>builders, chat, title, fluent-api</tags>
    /// </summary>
    public ChatBuilder WithTitle(string title)
    {
        _chat.Title = title;
        return this;
    }
    
    /// <summary>
    /// Устанавливает чат как группу
    /// <tags>builders, chat, group, fluent-api</tags>
    /// </summary>
    public ChatBuilder AsGroup()
    {
        _chat.Type = ChatType.Group;
        return this;
    }
    
    /// <summary>
    /// Устанавливает чат как супергруппу
    /// <tags>builders, chat, supergroup, fluent-api</tags>
    /// </summary>
    public ChatBuilder AsSupergroup()
    {
        _chat.Type = ChatType.Supergroup;
        return this;
    }
    
    /// <summary>
    /// Устанавливает чат как приватный
    /// <tags>builders, chat, private, fluent-api</tags>
    /// </summary>
    public ChatBuilder AsPrivate()
    {
        _chat.Type = ChatType.Private;
        return this;
    }
    
    /// <summary>
    /// Строит чат
    /// <tags>builders, chat, build, fluent-api</tags>
    /// </summary>
    public Chat Build() => _chat;
    
    /// <summary>
    /// Неявное преобразование в Chat
    /// <tags>builders, chat, conversion, fluent-api</tags>
    /// </summary>
    public static implicit operator Chat(ChatBuilder builder) => builder.Build();
} 
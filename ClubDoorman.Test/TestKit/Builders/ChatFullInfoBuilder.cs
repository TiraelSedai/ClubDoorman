using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.TestKit.Builders;

/// <summary>
/// Builder для создания ChatFullInfo объектов Telegram
/// <tags>builders, chat-full-info, telegram, fluent-api</tags>
/// </summary>
public class ChatFullInfoBuilder
{
    private ChatFullInfo _chatFullInfo = new();
    
    /// <summary>
    /// Устанавливает ID чата
    /// <tags>builders, chat-full-info, id, fluent-api</tags>
    /// </summary>
    public ChatFullInfoBuilder WithId(long chatId)
    {
        _chatFullInfo.Id = chatId;
        return this;
    }
    
    /// <summary>
    /// Устанавливает тип чата как приватный
    /// <tags>builders, chat-full-info, private, fluent-api</tags>
    /// </summary>
    public ChatFullInfoBuilder AsPrivate()
    {
        _chatFullInfo.Type = ChatType.Private;
        return this;
    }
    
    /// <summary>
    /// Устанавливает тип чата как группу
    /// <tags>builders, chat-full-info, group, fluent-api</tags>
    /// </summary>
    public ChatFullInfoBuilder AsGroup()
    {
        _chatFullInfo.Type = ChatType.Group;
        return this;
    }
    
    /// <summary>
    /// Устанавливает тип чата как супергруппу
    /// <tags>builders, chat-full-info, supergroup, fluent-api</tags>
    /// </summary>
    public ChatFullInfoBuilder AsSupergroup()
    {
        _chatFullInfo.Type = ChatType.Supergroup;
        return this;
    }
    
    /// <summary>
    /// Устанавливает тип чата как канал
    /// <tags>builders, chat-full-info, channel, fluent-api</tags>
    /// </summary>
    public ChatFullInfoBuilder AsChannel()
    {
        _chatFullInfo.Type = ChatType.Channel;
        return this;
    }
    
    /// <summary>
    /// Устанавливает заголовок чата
    /// <tags>builders, chat-full-info, title, fluent-api</tags>
    /// </summary>
    public ChatFullInfoBuilder WithTitle(string title)
    {
        _chatFullInfo.Title = title;
        return this;
    }
    
    /// <summary>
    /// Устанавливает username чата
    /// <tags>builders, chat-full-info, username, fluent-api</tags>
    /// </summary>
    public ChatFullInfoBuilder WithUsername(string username)
    {
        _chatFullInfo.Username = username;
        return this;
    }
    
    /// <summary>
    /// Устанавливает био чата
    /// <tags>builders, chat-full-info, bio, fluent-api</tags>
    /// </summary>
    public ChatFullInfoBuilder WithBio(string bio)
    {
        _chatFullInfo.Bio = bio;
        return this;
    }
    
    /// <summary>
    /// Устанавливает связанный чат ID
    /// <tags>builders, chat-full-info, linked-chat, fluent-api</tags>
    /// </summary>
    public ChatFullInfoBuilder WithLinkedChatId(long linkedChatId)
    {
        _chatFullInfo.LinkedChatId = linkedChatId;
        return this;
    }
    
    /// <summary>
    /// Устанавливает фото чата
    /// <tags>builders, chat-full-info, photo, fluent-api</tags>
    /// </summary>
    public ChatFullInfoBuilder WithPhoto(string smallFileId, string bigFileId)
    {
        _chatFullInfo.Photo = new ChatPhoto
        {
            SmallFileId = smallFileId,
            BigFileId = bigFileId
        };
        return this;
    }
    
    /// <summary>
    /// Создает ChatFullInfo объект
    /// <tags>builders, chat-full-info, build, fluent-api</tags>
    /// </summary>
    public ChatFullInfo Build() => _chatFullInfo;
} 
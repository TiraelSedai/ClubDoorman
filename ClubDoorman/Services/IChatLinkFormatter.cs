using Telegram.Bot.Types;

namespace ClubDoorman.Services;

/// <summary>
/// Интерфейс для форматирования ссылок на Telegram чаты в Markdown
/// </summary>
public interface IChatLinkFormatter
{
    /// <summary>
    /// Форматирует ссылку на чат в Markdown
    /// </summary>
    /// <param name="chat">Объект чата</param>
    /// <returns>Markdown ссылка или жирный текст</returns>
    string GetChatLink(Chat chat);
    
    /// <summary>
    /// Форматирует ссылку на чат в Markdown по ID и названию
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="chatTitle">Название чата</param>
    /// <returns>Markdown ссылка или жирный текст</returns>
    string GetChatLink(long chatId, string? chatTitle);
} 
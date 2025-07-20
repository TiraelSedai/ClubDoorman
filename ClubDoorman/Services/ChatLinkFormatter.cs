using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Extensions;

namespace ClubDoorman.Services;

/// <summary>
/// Реализация форматирования ссылок на Telegram чаты в Markdown
/// Точная копия логики из Worker.GetChatLink
/// </summary>
public class ChatLinkFormatter : IChatLinkFormatter
{
    /// <summary>
    /// Форматирует ссылку на чат в Markdown
    /// Точная копия логики Worker.GetChatLink(Chat chat)
    /// </summary>
    public string GetChatLink(Chat chat)
    {
        var escapedTitle = Markdown.Escape(chat.Title ?? "Неизвестный чат");
        if (!string.IsNullOrEmpty(chat.Username))
        {
            // Публичная группа или канал
            return $"[{escapedTitle}](https://t.me/{chat.Username})";
        }
        var formattedId = chat.Id.ToString();
        if (formattedId.StartsWith("-100"))
        {
            // Супергруппа без username
            formattedId = formattedId[4..];
            return $"[{escapedTitle}](https://t.me/c/{formattedId})";
        }
        else if (formattedId.StartsWith("-"))
        {
            // Обычная группа без username
            return $"*{escapedTitle}*";
        }
        else
        {
            // Канал без username
            return $"*{escapedTitle}*";
        }
    }

    /// <summary>
    /// Форматирует ссылку на чат в Markdown по ID и названию
    /// Точная копия логики Worker.GetChatLink(long chatId, string? chatTitle)
    /// </summary>
    public string GetChatLink(long chatId, string? chatTitle)
    {
        // Для обратной совместимости: используем только если нет объекта Chat
        var escapedTitle = Markdown.Escape(chatTitle ?? "Неизвестный чат");
        var formattedId = chatId.ToString();
        if (formattedId.StartsWith("-100"))
        {
            formattedId = formattedId[4..];
            return $"[{escapedTitle}](https://t.me/c/{formattedId})";
        }
        else if (formattedId.StartsWith("-"))
        {
            return $"*{escapedTitle}*";
        }
        else
        {
                    if (chatTitle?.StartsWith("@") == true)
        {
            var username = chatTitle[1..];
                return $"[{escapedTitle}](https://t.me/{username})";
            }
            return $"*{escapedTitle}*";
        }
    }
} 
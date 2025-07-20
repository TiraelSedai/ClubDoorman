using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Extensions;

namespace ClubDoorman.Services;

/// <summary>
/// Реализация форматирования ссылок на Telegram чаты в Markdown
/// </summary>
public class ChatLinkFormatter : IChatLinkFormatter
{
    private const string SupergroupPrefix = "-100";
    private const string GroupPrefix = "-";
    private const string DefaultTitle = "Неизвестный чат";
    private const string TelegramBaseUrl = "https://t.me";

    /// <summary>
    /// Форматирует ссылку на чат в Markdown
    /// </summary>
    public string GetChatLink(Chat chat)
    {
        var escapedTitle = Markdown.Escape(chat.Title ?? DefaultTitle);
        
        if (!string.IsNullOrEmpty(chat.Username))
        {
            return FormatPublicChatLink(escapedTitle, chat.Username);
        }
        
        return FormatPrivateChatLink(escapedTitle, chat.Id);
    }

    /// <summary>
    /// Форматирует ссылку на чат в Markdown по ID и названию
    /// </summary>
    public string GetChatLink(long chatId, string? chatTitle)
    {
        var escapedTitle = Markdown.Escape(chatTitle ?? DefaultTitle);
        
        // Специальная обработка для username в названии
        if (chatTitle?.StartsWith("@") == true)
        {
            var username = chatTitle[1..];
            return FormatPublicChatLink(escapedTitle, username);
        }
        
        return FormatPrivateChatLink(escapedTitle, chatId);
    }

    private static string FormatPublicChatLink(string escapedTitle, string username)
    {
        return $"[{escapedTitle}]({TelegramBaseUrl}/{username})";
    }

    private static string FormatPrivateChatLink(string escapedTitle, long chatId)
    {
        var formattedId = chatId.ToString();
        
        if (formattedId.StartsWith(SupergroupPrefix))
        {
            // Супергруппа: убираем префикс -100 и создаем ссылку на приватный чат
            var cleanId = formattedId[SupergroupPrefix.Length..];
            return $"[{escapedTitle}]({TelegramBaseUrl}/c/{cleanId})";
        }
        
        // Обычная группа или канал без username - показываем как жирный текст
        return $"*{escapedTitle}*";
    }
} 
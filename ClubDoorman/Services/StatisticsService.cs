using System.Collections.Concurrent;
using System.Text;
using ClubDoorman.Models;
using ClubDoorman.Infrastructure;
using Telegram.Bot;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис статистики
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly ConcurrentDictionary<long, ChatStats> _stats = new();
    private readonly TelegramBotClient _bot;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(TelegramBotClient bot, ILogger<StatisticsService> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public void IncrementCaptcha(long chatId)
    {
        // Здесь можем добавить логику инкремента капчи если нужно
        _logger.LogDebug("Инкремент капчи для чата {ChatId}", chatId);
    }

    public void IncrementBlacklistBan(long chatId)
    {
        var stats = _stats.GetOrAdd(chatId, new ChatStats(null));
        Interlocked.Increment(ref stats.BlacklistBanned);
        _logger.LogDebug("Инкремент блэклист бана для чата {ChatId}", chatId);
    }

    public void IncrementKnownBadMessage(long chatId)
    {
        var stats = _stats.GetOrAdd(chatId, new ChatStats(null));
        Interlocked.Increment(ref stats.KnownBadMessage);
        _logger.LogDebug("Инкремент известного плохого сообщения для чата {ChatId}", chatId);
    }

    public void IncrementLongNameBan(long chatId)
    {
        var stats = _stats.GetOrAdd(chatId, new ChatStats(null));
        Interlocked.Increment(ref stats.LongNameBanned);
        _logger.LogDebug("Инкремент бана за длинное имя для чата {ChatId}", chatId);
    }

    public IDictionary<long, ChatStats> GetAllStats()
    {
        return _stats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void ClearStats()
    {
        _stats.Clear();
        _logger.LogInformation("Статистика очищена");
    }

    public async Task<string> GenerateReportAsync()
    {
        var report = _stats.ToArray();
        var sb = new StringBuilder();
        sb.AppendLine("📊 *Статистика за последние 24 часа:*");
        
        foreach (var (chatId, stats) in report.OrderBy(x => x.Value.ChatTitle))
        {
            var sum = stats.KnownBadMessage + stats.BlacklistBanned + stats.StoppedCaptcha + stats.LongNameBanned;
            if (sum == 0) continue;
            
            try
            {
                var chat = await _bot.GetChat(chatId);
                var chatLink = GetChatLink(chat);
                var chatType = ChatSettingsManager.GetChatType(chat.Id);
                
                sb.AppendLine();
                sb.AppendLine($"{chatLink} (`{chat.Id}`) [{chatType}]:");
                sb.AppendLine($"▫️ Всего блокировок: *{sum}*");
                if (stats.BlacklistBanned > 0)
                    sb.AppendLine($"▫️ По блеклистам: *{stats.BlacklistBanned}*");
                if (stats.StoppedCaptcha > 0)
                    sb.AppendLine($"▫️ Не прошли капчу: *{stats.StoppedCaptcha}*");
                if (stats.KnownBadMessage > 0)
                    sb.AppendLine($"▫️ Известные спам-сообщения: *{stats.KnownBadMessage}*");
                if (stats.LongNameBanned > 0)
                    sb.AppendLine($"▫️ За длинные имена: *{stats.LongNameBanned}*");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось получить информацию о чате {ChatId}", chatId);
                sb.AppendLine($"Чат {chatId}: {sum} блокировок");
            }
        }

        if (sb.Length <= 35) // Если нет данных, кроме заголовка
        {
            sb.AppendLine("\nНичего интересного не произошло 🎉");
        }

        return sb.ToString();
    }

    private static string GetChatLink(Telegram.Bot.Types.Chat chat)
    {
        var escapedTitle = EscapeMarkdown(chat.Title ?? "Неизвестный чат");
        if (!string.IsNullOrEmpty(chat.Username))
        {
            return $"[{escapedTitle}](https://t.me/{chat.Username})";
        }
        var formattedId = chat.Id.ToString();
        if (formattedId.StartsWith("-100"))
        {
            formattedId = formattedId.Substring(4);
            return $"[{escapedTitle}](https://t.me/c/{formattedId})";
        }
        else if (formattedId.StartsWith("-"))
        {
            return $"*{escapedTitle}*";
        }
        else
        {
            return $"*{escapedTitle}*";
        }
    }

    private static string EscapeMarkdown(string text)
    {
        return text.Replace("*", "\\*")
                   .Replace("_", "\\_")
                   .Replace("[", "\\[")
                   .Replace("]", "\\]")
                   .Replace("(", "\\(")
                   .Replace(")", "\\)");
    }
} 
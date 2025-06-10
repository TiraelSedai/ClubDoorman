using System.Text.Json;
using System.Text;
using Telegram.Bot;

namespace ClubDoorman;

public class GlobalStatsManager
{
    private readonly string _jsonPath = "data/global_stats.json";
    private readonly string _htmlPath = "data/stats.html";
    private readonly object _lock = new();
    private StatsRoot _stats;

    public GlobalStatsManager()
    {
        if (!Directory.Exists("data"))
            Directory.CreateDirectory("data");
        _stats = Load();
    }

    // Гарантирует, что чат есть в статистике, и обновляет title
    public async Task EnsureChatAsync(long chatId, string chatTitle, ITelegramBotClient bot)
    {
        bool isNew = false;
        lock (_lock)
        {
            if (!_stats.Chats.ContainsKey(chatId))
            {
                _stats.Chats[chatId] = new ChatStat { Title = chatTitle };
                isNew = true;
            }
            else if (!string.IsNullOrWhiteSpace(chatTitle))
            {
                _stats.Chats[chatId].Title = chatTitle;
            }
            Save();
        }
        if (isNew)
        {
            try
            {
                var count = await bot.GetChatMemberCountAsync(chatId);
                lock (_lock)
                {
                    _stats.Chats[chatId].Members = count;
                    Save();
                }
            }
            catch { }
        }
    }

    // Обновляет количество участников для всех чатов
    public async Task UpdateAllMembersAsync(ITelegramBotClient bot)
    {
        var chatIds = _stats.Chats.Keys.ToList();
        foreach (var chatId in chatIds)
        {
            try
            {
                var count = await bot.GetChatMemberCountAsync(chatId);
                lock (_lock)
                {
                    if (_stats.Chats.TryGetValue(chatId, out var chat))
                        chat.Members = count;
                }
            }
            catch { }
        }
        lock (_lock) { Save(); }
    }

    // Инкрементирует показ капчи
    public void IncCaptcha(long chatId, string chatTitle = "")
    {
        lock (_lock)
        {
            if (!_stats.Chats.ContainsKey(chatId))
                _stats.Chats[chatId] = new ChatStat { Title = chatTitle };
            _stats.Chats[chatId].CaptchaShown++;
            Save();
        }
    }

    // Инкрементирует бан
    public void IncBan(long chatId, string chatTitle = "")
    {
        lock (_lock)
        {
            if (!_stats.Chats.ContainsKey(chatId))
                _stats.Chats[chatId] = new ChatStat { Title = chatTitle };
            _stats.Chats[chatId].Bans++;
            Save();
        }
    }

    public void GenerateHtml()
    {
        lock (_lock)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><head><meta charset=\"UTF-8\"><title>Статистика ClubDoorman</title></head><body>");
            sb.AppendLine("<h1>Статистика ClubDoorman</h1>");
            sb.AppendLine($"<b>Обслуживаемых чатов: {_stats.Chats.Count}</b><br><br>");
            sb.AppendLine("<table border=1 cellpadding=4><tr><th>Группа</th><th>Участников</th><th>Показано капч</th><th>Банов</th></tr>");
            foreach (var chat in _stats.Chats.Values.OrderByDescending(c => c.Members))
            {
                sb.AppendLine($"<tr><td>{System.Web.HttpUtility.HtmlEncode(chat.Title)}</td><td>{chat.Members}</td><td>{chat.CaptchaShown}</td><td>{chat.Bans}</td></tr>");
            }
            sb.AppendLine("</table></body></html>");
            File.WriteAllText(_htmlPath, sb.ToString());
        }
    }

    private StatsRoot Load()
    {
        try
        {
            if (File.Exists(_jsonPath))
            {
                var json = File.ReadAllText(_jsonPath);
                var stats = JsonSerializer.Deserialize<StatsRoot>(json);
                if (stats != null)
                    return stats;
            }
        }
        catch { }
        return new StatsRoot();
    }

    private void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(_jsonPath, JsonSerializer.Serialize(_stats, options));
        GenerateHtml();
    }
}

public class StatsRoot
{
    public Dictionary<long, ChatStat> Chats { get; set; } = new();
}

public class ChatStat
{
    public string Title { get; set; } = string.Empty;
    public int Members { get; set; } = 0;
    public int CaptchaShown { get; set; } = 0;
    public int Bans { get; set; } = 0;
} 
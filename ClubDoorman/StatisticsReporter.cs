using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace ClubDoorman;

public sealed class Stats
{
    public Stats() { }

    public Stats(string? Title)
    {
        ChatTitle = Title;
    }

    /// <summary>
    /// Chat Id, database Id.
    /// </summary>
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; set; }
    public string? ChatTitle { get; set; }
    public int StoppedCaptcha { get; set; }
    public int BlacklistBanned { get; set; }
    public int KnownBadMessage { get; set; }
    public int Autoban { get; set; }
    public int Channels { get; set; }
}

internal class StatisticsReporter
{
    private readonly ITelegramBotClient _bot;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Config _config;
    private readonly ILogger<StatisticsReporter> _logger;
    public readonly ConcurrentDictionary<long, Stats> Stats = new();
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(1));

    public StatisticsReporter(ITelegramBotClient bot, IServiceScopeFactory scopeFactory, Config config, ILogger<StatisticsReporter> logger)
    {
        _bot = bot;
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    public async Task MainStatisticsLoop(CancellationToken ct)
    {
        _ = Init();
        while (await _timer.WaitForNextTickAsync(ct))
        {
            var utcNow = DateTimeOffset.UtcNow;
            bool reportingTime = utcNow.Hour == 7 && utcNow.Minute == 0;
            if (!reportingTime)
            {
                _ = WriteToDb();
                continue;
            }

            var report = Stats.ToArray();
            Stats.Clear();
            var free = new List<string>();
            var assigned = new Dictionary<long, List<string>>();

            foreach (var (chatId, stats) in report)
            {
                var list = free;
                if (_config.MultiAdminChatMap.TryGetValue(chatId, out var adminChat))
                {
                    if (!assigned.TryGetValue(adminChat, out list))
                    {
                        list = [];
                        assigned[adminChat] = list;
                    }
                }
                else
                {
                    list.Add($"Unmapped ID {chatId} {stats.ChatTitle}");
                }
                list.Add(ChatToStatsString(stats));
            }

            try
            {
                foreach (var (adminChat, list) in assigned)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    await _bot.SendMessage(
                        adminChat,
                        $"За последние 24 часа - статистика того что даже не прилетало в админку:\n{string.Join('\n', list)}",
                        cancellationToken: ct
                    );
                }
                foreach (var chunk in free.Chunk(10))
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    await _bot.SendMessage(
                        _config.AdminChatId,
                        $"В фри чатах за 24 часа:\n{string.Join('\n', chunk)}",
                        cancellationToken: ct
                    );
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to sent report to admin chat");
            }
        }
    }

    private async Task WriteToDb()
    {
        if (Stats.IsEmpty)
            return;

        var report = Stats.ToArray();
        var keys = report.Select(x => x.Key).ToHashSet();

        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stats = await db.Stats.ToListAsync();

        foreach (var chat in report)
        {
            var inDb = stats.SingleOrDefault(x => x.Id == chat.Key);
            if (inDb != null)
            {
                inDb.BlacklistBanned = chat.Value.BlacklistBanned;
                inDb.StoppedCaptcha = chat.Value.StoppedCaptcha;
                inDb.KnownBadMessage = chat.Value.KnownBadMessage;
            }
            else
            {
                db.Add(chat.Value);
            }
        }

        foreach (var stat in stats.Where(x => !keys.Contains(x.Id)))
            db.Remove(stat);

        await db.SaveChangesAsync();
    }

    private async Task Init()
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stats = await db.Stats.AsNoTracking().ToListAsync();
        foreach (var stat in stats)
            Stats[stat.Id] = stat;
    }

    private static string ChatToStatsString(Stats stats)
    {
        var sb = new StringBuilder();
        sb.Append("В ");
        sb.Append(stats.ChatTitle);
        var sum = stats.KnownBadMessage + stats.BlacklistBanned + stats.StoppedCaptcha + stats.Channels + stats.Autoban;
        sb.Append($": {sum} раза сработала защита автоматом{Environment.NewLine}");
        sb.Append(
            $"По блеклистам известных спамеров забанено: {stats.BlacklistBanned}, не прошло капчу: {stats.StoppedCaptcha}, автобан (например: кнопки на сообщении, ML супер уверен, сообщения маскирующиеся под русские - а там греческие буквы): {stats.Autoban}, каналов с малым количеством подписчиков: {stats.Channels}, за строгому соответствию блеклистам спам сообщений: {stats.KnownBadMessage}"
        );
        return sb.ToString();
    }
}

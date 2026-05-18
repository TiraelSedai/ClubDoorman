using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations.Schema;
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

internal class StatisticsReporter : IDisposable
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

            var report = BuildStatisticsReportMessages(
                Stats.ToArray(),
                _config.MultiAdminChatMap,
                _config.StatisticsFallbackAdminChats,
                _config.AdminChatId
            );
            Stats.Clear();

            try
            {
                foreach (var message in report)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    await _bot.SendMessage(message.AdminChatId, message.Text, cancellationToken: ct);
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
                inDb.Autoban = chat.Value.Autoban;
                inDb.Channels = chat.Value.Channels;
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

    internal static IReadOnlyList<StatisticsReportMessage> BuildStatisticsReportMessages(
        IEnumerable<KeyValuePair<long, Stats>> report,
        FrozenDictionary<long, long> multiAdminChatMap,
        FrozenSet<long> fallbackAdminChats,
        long defaultAdminChatId
    )
    {
        var free = new List<string>();
        var assigned = new Dictionary<long, List<string>>();
        foreach (var (chatId, stats) in report)
        {
            var list = free;
            if (multiAdminChatMap.TryGetValue(chatId, out var adminChat))
            {
                var targetAdminChat = fallbackAdminChats.Contains(adminChat) ? defaultAdminChatId : adminChat;

                if (!assigned.TryGetValue(targetAdminChat, out list))
                {
                    list = [];
                    assigned[targetAdminChat] = list;
                }
            }
            else
            {
                list.Add($"Unmapped ID {chatId} {stats.ChatTitle}");
            }
            list.Add(ChatToStatsString(stats));
        }

        var messages = new List<StatisticsReportMessage>();
        foreach (var (adminChat, list) in assigned)
        {
            messages.Add(
                new StatisticsReportMessage(
                    adminChat,
                    $"За последние 24 часа - статистика того что даже не прилетало в админку:\n{string.Join("\n=============================\n", list)}"
                )
            );
        }

        foreach (var chunk in free.Chunk(10))
        {
            messages.Add(
                new StatisticsReportMessage(
                    defaultAdminChatId,
                    $"В фри чатах за 24 часа:\n{string.Join("\n=============================\n", chunk)}"
                )
            );
        }

        return messages;
    }

    internal sealed record StatisticsReportMessage(long AdminChatId, string Text);

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
        var sum = stats.KnownBadMessage + stats.BlacklistBanned + stats.StoppedCaptcha + stats.Channels + stats.Autoban;
        var lines = new List<string> { $"В чате {stats.ChatTitle}: {sum} раз(а) сработала защита автоматом" };

        if (stats.StoppedCaptcha > 0)
            lines.Add($"{stats.StoppedCaptcha} не прошло капчу");
        if (stats.BlacklistBanned > 0)
            lines.Add($"{stats.BlacklistBanned} известных спамеров");
        if (stats.KnownBadMessage > 0)
            lines.Add($"{stats.KnownBadMessage} известных спам-сообщений");
        if (stats.Channels > 0)
            lines.Add($"{stats.Channels} каналов с малым количеством подписчиков");
        if (stats.Autoban > 0)
            lines.Add($"{stats.Autoban} забанено автоматом по сумме эвристик.");

        return string.Join(Environment.NewLine, lines);
    }

    public void Dispose()
    {
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }
}

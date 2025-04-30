using System.Collections.Concurrent;
using System.Text;
using Telegram.Bot;

namespace ClubDoorman;

internal sealed class Stats(string? Title)
{
    public string? ChatTitle = Title;
    public int StoppedCaptcha;
    public int BlacklistBanned;
    public int KnownBadMessage;
}

internal class StatisticsReporter
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<StatisticsReporter> _logger;
    public readonly ConcurrentDictionary<long, Stats> Stats = new();
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));

    public StatisticsReporter(ITelegramBotClient bot, ILogger<StatisticsReporter> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public async Task ReportStatistics(CancellationToken ct)
    {
        while (await _timer.WaitForNextTickAsync(ct))
        {
            if (DateTimeOffset.UtcNow.Hour != 12)
                continue;

            var report = Stats.ToArray();
            Stats.Clear();
            var free = new List<string>();
            var assigned = new Dictionary<long, List<string>>();

            foreach (var (chatId, stats) in report)
            {
                var list = free;
                if (Config.MultiAdminChatMap.TryGetValue(chatId, out var adminChat))
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
                    await _bot.SendMessage(adminChat, $"За последние 24 часа:\n{string.Join('\n', list)}", cancellationToken: ct);
                }
                foreach (var chunk in free.Chunk(10))
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    await _bot.SendMessage(
                        Config.AdminChatId,
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

    private static string ChatToStatsString(Stats stats)
    {
        var sb = new StringBuilder();
        sb.Append("В ");
        sb.Append(stats.ChatTitle);
        var sum = stats.KnownBadMessage + stats.BlacklistBanned + stats.StoppedCaptcha;
        sb.Append($": {sum} раза сработала защита автоматом{Environment.NewLine}");
        sb.Append(
            $"По блеклистам известных аккаунтов спамеров забанено: {stats.BlacklistBanned}, не прошло капчу: {stats.StoppedCaptcha}, за известные спам сообщения забанено: {stats.KnownBadMessage}"
        );
        return sb.ToString();
    }
}

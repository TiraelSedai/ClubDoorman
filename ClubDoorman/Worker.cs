using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman;

internal sealed class Worker(
    ITelegramBotClient botClient,
    CaptchaManager captchaManager,
    MessageProcessor messageProcessor,
    StatisticsReporter statisticsReporter,
    ILogger<Worker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = captchaManager.CaptchaLoop(stoppingToken);
        _ = statisticsReporter.MainStatisticsLoop(stoppingToken);
        const string offsetPath = "data/offset.txt";
        var offset = 0;
        if (File.Exists(offsetPath))
        {
            var lines = await File.ReadAllLinesAsync(offsetPath, stoppingToken);
            if (lines.Length > 0 && int.TryParse(lines[0], out offset))
                logger.LogDebug("offset read ok");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                offset = await UpdateLoop(offset, stoppingToken);
                if (offset % 100 == 0)
                    await File.WriteAllTextAsync(offsetPath, offset.ToString(), stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "ExecuteAsync");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }

    private async Task<int> UpdateLoop(int offset, CancellationToken stoppingToken)
    {
        var updates = await botClient.GetUpdates(
            offset,
            limit: 100,
            timeout: 100,
            allowedUpdates:
            [
                UpdateType.Message,
                UpdateType.EditedMessage,
                UpdateType.ChatMember,
                UpdateType.CallbackQuery,
                UpdateType.MessageReaction,
            ],
            cancellationToken: stoppingToken
        );
        if (updates.Length == 0)
            return offset;
        offset = updates.Max(x => x.Id) + 1;
        string? mediaGroupId = null;
        foreach (var update in updates)
        {
            var prevMediaGroup = mediaGroupId;
            mediaGroupId = update.Message?.MediaGroupId;
            if (prevMediaGroup != null && prevMediaGroup == mediaGroupId)
            {
                logger.LogDebug("2+ message from an album, it could not have any text/caption, skip");
                continue;
            }
            messageProcessor.HandleUpdate(update, stoppingToken).FireAndForget(logger, nameof(MessageProcessor.HandleUpdate));
        }
        return offset;
    }
}

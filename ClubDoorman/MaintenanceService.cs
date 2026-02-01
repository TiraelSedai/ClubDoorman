using Microsoft.EntityFrameworkCore;

namespace ClubDoorman;

internal sealed class MaintenanceService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<MaintenanceService> logger
)
{
    public async Task MaintenanceLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextMidnight = now.Date.AddDays(1);
            var delay = nextMidnight - now;
            
            await Task.Delay(delay, stoppingToken);

            try
            {
                await PerformMaintenance(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during maintenance");
            }
        }
    }

    private async Task PerformMaintenance(CancellationToken ct)
    {
        logger.LogInformation("Starting daily maintenance tasks");

        var removedCount = await RemoveHalfApprovedDuplicates(ct);
        if (removedCount > 0)
            logger.LogInformation("Removed {Count} users from HalfApprovedUsers that were already fully approved", removedCount);

        logger.LogInformation("Daily maintenance completed");
    }

    private async Task<int> RemoveHalfApprovedDuplicates(CancellationToken ct)
    {
        using var scope = serviceScopeFactory.CreateScope();
        await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var approvedUserIds = await db.ApprovedUsers
            .AsNoTracking()
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (approvedUserIds.Count == 0)
            return 0;

        var deleted = await db.HalfApprovedUsers
            .Where(h => approvedUserIds.Contains(h.Id))
            .ExecuteDeleteAsync(ct);

        return deleted;
    }
}

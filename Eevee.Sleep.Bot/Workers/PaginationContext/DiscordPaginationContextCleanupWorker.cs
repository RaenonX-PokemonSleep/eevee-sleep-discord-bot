using Eevee.Sleep.Bot.Models.Pagination;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Workers.PaginationContext;

public class DiscordPaginationContextCleanupWorker : BackgroundService {
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(GlobalConst.DiscordPaginationParams.Ttl / 2.0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            DiscordPaginationContext<object>.CleanupExpiredStates();

            try {
                await Task.Delay(_cleanupInterval, stoppingToken);
            } catch (TaskCanceledException) {
                break;
            }
        }
    }
}
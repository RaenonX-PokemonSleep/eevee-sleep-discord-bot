using Discord.WebSocket;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using Eevee.Sleep.Bot.Workers.Crawlers;

namespace Eevee.Sleep.Bot.Workers.Announcement;

public abstract class AnnouncementCrawlingWorker(
    IAnnouncementCrawler crawler,
    DiscordSocketClient client,
    ILogger<AnnouncementCrawlingWorker> logger
) : BackgroundService {
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
        logger.LogInformation("Starting in-game announcement update crawler.");
        cancellationToken.Register(
            () => logger.LogInformation("Stopping in-game announcement update crawler: Cancellation token received.")
        );

        while (!cancellationToken.IsCancellationRequested) {
            logger.LogInformation("Checking in-game announcement updates.");

            try {
                await crawler.ExecuteAsync();
            } catch (MaxAttemptExceededException e) {
                await client.SendMessageInAdminAlertChannel(
                    embed: DiscordMessageMakerForAnnouncement.MakeDocumentProcessingErrorMessage(e.InnerException)
                );
                await _cancellationTokenSource.CancelAsync();
                break;
            }

            await Task.Delay(_checkInterval, cancellationToken);
        }
    }
}
using Discord.WebSocket;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using Eevee.Sleep.Bot.Workers.Crawlers;

namespace Eevee.Sleep.Bot.Workers;

public class AnnouncementCrawlingWorker (
    IAnnoucementCrawler crawler,
    DiscordSocketClient client,
    ILogger<AnnouncementCrawlingWorker> logger
) : BackgroundService {
    private readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(60);
    private readonly CancellationTokenSource CancellationTokenSource = new();

    protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
        logger.LogInformation("Starting in-game announcement update crawler.");
        cancellationToken.Register(() => logger.LogInformation("Stopping in-game announcement update crawler: cancellation token received."));

        while (!cancellationToken.IsCancellationRequested) {
            logger.LogInformation("Checking in-game announcement updates.");

            try {
                await crawler.ExecuteAsync();
            } catch (MaxAttemptExceededException e) {
                await client.SendMessageInAdminAlertChannel(
                    embed: DiscordMessageMakerForInGameAnnouncement.MakeDocumentProcessingErrorMessage(e.InnerException)
                );
                CancellationTokenSource.Cancel();
                break;
            }

            await Task.Delay(CheckInterval, cancellationToken);
        }
    }
}
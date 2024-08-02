using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.InGameAnnouncement.Officialsite;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using Eevee.Sleep.Bot.Workers.Crawlers;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Workers;

public class OfficialsiteAnnouncementUpdateWatchingWorker(
    OfficialsiteAnnouncementCrawler crawler,
    DiscordSocketClient client,
    ILogger<OfficialsiteAnnouncementUpdateWatchingWorker> logger
) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
        // If UpdateWatchingWorker enters the waiting state before CrawlingWorker before initialization, 
        // it will be notified by the number of news, so check the news first and then enter the waiting state.
        logger.LogInformation("Starting initialization process of the Officialsite announcement update worker.");
        try {
            await crawler.ExecuteAsync();
        } catch (MaxAttemptExceededException e) {
            await client.SendMessageInAdminAlertChannel(
                embed: DiscordMessageMakerForInGameAnnouncement.MakeUpdateWachingWorkerInitializeFailedMessage(e.InnerException)
            );
        }

        var options = new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<OfficialsiteAnnouncementDetailModel>>()
            .Match(x =>
                x.OperationType == ChangeStreamOperationType.Update ||
                x.OperationType == ChangeStreamOperationType.Modify ||
                x.OperationType == ChangeStreamOperationType.Insert
            );

        using var cursor = await MongoConst.OfficialsiteAnnouncementDetailCollection
            .WatchAsync(pipeline, options, cancellationToken);

        await cursor.ForEachAsync(async change => {
            var detail = change.FullDocument;

            logger.LogInformation(
                "Received in-game announcement detail update in {Language} ({Title} / #{Id})",
                detail.Language,
                detail.Title,
                detail.AnnouncementId
            );

            await client.SendMessageInOfficialsiteAnnouncementNoticeChannelAsync(
                embed: DiscordMessageMakerForInGameAnnouncement.MakeOfficialsiteAnnouncementUpdateMessage(detail)
            );
        }, cancellationToken);
    }
}
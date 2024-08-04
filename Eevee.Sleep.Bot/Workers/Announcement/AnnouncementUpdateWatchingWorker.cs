using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using Eevee.Sleep.Bot.Workers.Crawlers;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Workers.Announcement;

public abstract class AnnouncementUpdateWatchingWorker<T>(
    IAnnoucementCrawler crawler,
    DiscordSocketClient client,
    ILogger<AnnouncementUpdateWatchingWorker<T>> logger
) : BackgroundService where T : AnnouncementMetaModel{
    protected abstract IMongoCollection<T> GetMongoCollection();

    protected abstract ulong? GetNotifyRoleId(AnnouncementLanguage language);

    protected abstract Embed MakeAnnouncementUpdateMessage(T detail);

    protected abstract Task SendMessageInAnnouncementNoticeChannelAsync(
        string? message,
        AnnouncementLanguage language,
        Embed embed
    );

    protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
        // If UpdateWatchingWorker enters the waiting state before CrawlingWorker before initialization, 
        // it will be notified by the number of news, so check the news first and then enter the waiting state.
        logger.LogInformation("Starting initialization process of the ingame announcement update worker.");
        try {
            await crawler.ExecuteAsync();
        } catch (MaxAttemptExceededException e) {
            await client.SendMessageInAdminAlertChannel(
                embed: DiscordMessageMakerForAnnouncement.MakeUpdateWachingWorkerInitializeFailedMessage(e.InnerException)
            );
        }

        var options = new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<T>>()
            .Match(x =>
                x.OperationType == ChangeStreamOperationType.Update ||
                x.OperationType == ChangeStreamOperationType.Modify ||
                x.OperationType == ChangeStreamOperationType.Insert
            );

        using var cursor = await GetMongoCollection()
            .WatchAsync(pipeline, options, cancellationToken);

        await cursor.ForEachAsync(async change => {
            var detail = change.FullDocument;

            logger.LogInformation(
                "Received in-game announcement detail update in {Language} ({Title} / #{Id})",
                detail.Language,
                detail.Title,
                detail.AnnouncementId
            );

            var notifyRole = GetNotifyRoleId(detail.Language);

            await SendMessageInAnnouncementNoticeChannelAsync(
                message: notifyRole is not null ? MentionUtils.MentionRole(notifyRole.Value) : null,
                language: detail.Language,
                embed: MakeAnnouncementUpdateMessage(detail)
            );
        }, cancellationToken);
    }
}
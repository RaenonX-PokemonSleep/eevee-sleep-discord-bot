using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Models.Announcement;
using Eevee.Sleep.Bot.Workers.Crawlers;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Workers.Announcement;

public abstract class AnnouncementUpdateWatchingWorker<T>(
    IAnnouncementCrawler crawler,
    AnnouncementHistoryController<T> historyController,
    DiscordSocketClient client,
    ILogger<AnnouncementUpdateWatchingWorker<T>> logger
) : BackgroundService where T : AnnouncementMetaModel {
    private readonly AnnouncementDiffAlertSender<T> _diffAlertSender = new(historyController, client, logger);

    protected abstract IMongoCollection<T> GetMongoCollection();

    protected abstract ulong? GetNotifyRoleId(AnnouncementLanguage language);

    protected abstract Embed MakeAnnouncementUpdateMessage(T detail, bool isNew);

    protected abstract bool HasSameContent(T first, T second);

    protected abstract string GetContent(T detail);

    protected abstract string GetSourceName();

    protected abstract string GetDisplayUrl(T detail);

    protected abstract Task SendMessageInAnnouncementNoticeChannelAsync(
        string? message,
        AnnouncementLanguage language,
        Embed embed
    );

    protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
        // The crawling worker owns writes. Ignore bootstrap changes by opening the change stream afterward.
        logger.LogInformation("Waiting for the initial announcement crawl to complete.");
        await crawler.InitialCrawlCompleted.WaitAsync(cancellationToken);

        var options = new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<T>>()
            .Match(x =>
                x.OperationType == ChangeStreamOperationType.Update ||
                x.OperationType == ChangeStreamOperationType.Modify ||
                x.OperationType == ChangeStreamOperationType.Insert ||
                x.OperationType == ChangeStreamOperationType.Replace
            );

        while (!cancellationToken.IsCancellationRequested) {
            try {
                using var cursor = await GetMongoCollection().WatchAsync(pipeline, options, cancellationToken);

                await cursor.ForEachAsync(
                    async change => {
                        var detail = change.FullDocument;

                        logger.LogInformation(
                            "Received in-game announcement detail update in {Language} ({Title} / #{Id})",
                            detail.Language,
                            detail.Title,
                            detail.AnnouncementId
                        );

                        var notifyRole = GetNotifyRoleId(detail.Language);

                        var isNew = change.OperationType == ChangeStreamOperationType.Insert;
                        var publicNotificationTask = SendMessageInAnnouncementNoticeChannelAsync(
                            notifyRole is not null ? MentionUtils.MentionRole(notifyRole.Value) : null,
                            detail.Language,
                            MakeAnnouncementUpdateMessage(detail, isNew)
                        );

                        if (isNew) {
                            await publicNotificationTask;
                            return;
                        }

                        await Task.WhenAll(
                            publicNotificationTask,
                            _diffAlertSender.SendAsync(
                                detail,
                                HasSameContent,
                                GetContent,
                                GetSourceName(),
                                GetDisplayUrl(detail)
                            )
                        );
                    },
                    cancellationToken
                );
            } catch (Exception e) when (e is not OperationCanceledException) {
                logger.LogError(e, "An error occurred while watching announcement updates. Restarting the watcher.");
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
    }
}
using AngleSharp.Common;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement.OfficialSite;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;
using Eevee.Sleep.Bot.Modules.ExternalServices;

namespace Eevee.Sleep.Bot.Workers.Crawlers;

public class OfficialSiteAnnouncementCrawler(
    ILogger<OfficialSiteAnnouncementCrawler> logger,
    OfficialSiteNewsClient newsClient,
    OfficialSiteAnnouncementCrawlStateController crawlStateController,
    AnnouncementDetailController<OfficialSiteAnnouncementDetailModel> detailController,
    AnnouncementHistoryController<OfficialSiteAnnouncementDetailModel> historyController
) : IAnnouncementCrawler {
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private readonly TaskCompletionSource _initialCrawlCompleted = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    public Task InitialCrawlCompleted => _initialCrawlCompleted.Task;

    public async Task ExecuteAsync(CancellationToken cancellationToken, int retryCount = 0) {
        await Semaphore.WaitAsync(cancellationToken);

        try {
            while (true) {
                try {
                    await CrawlAsync(cancellationToken);
                    _initialCrawlCompleted.TrySetResult();
                    return;
                } catch (DocumentProcessingException e) {
                    retryCount++;
                    logger.LogError("{Message} Retries: {RetryCount}", e.Message, retryCount);

                    var status = e.Context.GetValueOrDefault("status");
                    var isAccessBlocked = status is "Unauthorized" or "Forbidden" or "TooManyRequests";
                    if (isAccessBlocked || retryCount >= IAnnouncementCrawler.MaxRetryCount) {
                        throw new MaxAttemptExceededException(
                            isAccessBlocked
                                ? "Official website announcement requests were denied or rate limited."
                                : "Failed to get official website announcements. Retry count exceeded.",
                            e
                        );
                    }

                    await Task.Delay(RetryInterval, cancellationToken);
                }
            }
        } finally {
            Semaphore.Release();
        }
    }

    private async Task CrawlAsync(CancellationToken cancellationToken) {
        var entries = await newsClient.FetchEntriesAsync(cancellationToken);
        var modifiedTimes = await crawlStateController.FindModifiedTimesAsync(
            entries.Select(entry => entry.Url), cancellationToken
        );
        var changed = entries.Where(entry => entry.NeedsFetch(modifiedTimes)).ToArray();
        logger.LogInformation("Fetching {Count} new or changed official website articles.", changed.Length);

        foreach (var entry in changed) {
            var article = await newsClient.FetchArticleAsync(entry, cancellationToken);
            var (index, detail) = article.ToModels(entry);
            await OfficialSiteAnnouncementIndexController.BulkUpsert([index]);
            await SaveDetailsAndHistories([detail]);

            // Checkpoint only after persistence succeeds, so failed/interrupted crawls can resume per article.
            await crawlStateController.Upsert(entry, cancellationToken);
        }
    }

    private async Task SaveDetailsAndHistories(List<OfficialSiteAnnouncementDetailModel> details) {
        var existedDetails = detailController.FindAllByIds(details.Select(x => x.AnnouncementId));
        var existedDetailsById = existedDetails.ToDictionary(x => (x.AnnouncementId, x.Language));

        var shouldSave = details.Where(
            detail => {
                var current = existedDetailsById.GetOrDefault((detail.AnnouncementId, detail.Language), null);
                return current is null ||
                       current.ContentHash != detail.ContentHash ||
                       current.Title != detail.Title ||
                       current.Url != detail.Url ||
                       current.OriginalUpdated != detail.OriginalUpdated;
            }
        ).ToArray();
        var shouldRecordHistory = shouldSave.Where(
            detail =>
                !existedDetailsById.TryGetValue((detail.AnnouncementId, detail.Language), out var current) ||
                current.ContentHash != detail.ContentHash
        ).ToArray();

        await detailController.BulkUpsert(shouldSave);
        await historyController.BulkInsert(shouldRecordHistory);
    }
}

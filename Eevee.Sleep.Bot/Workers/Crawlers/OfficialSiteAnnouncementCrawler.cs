using AngleSharp.Common;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement.OfficialSite;
using Eevee.Sleep.Bot.Enums;
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
    private static readonly TimeSpan WatermarkOverlap = TimeSpan.FromMinutes(5);
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
                    var isRateBlocked = status is "Forbidden" or "TooManyRequests";
                    if (isRateBlocked || retryCount >= IAnnouncementCrawler.MaxRetryCount) {
                        throw new MaxAttemptExceededException(
                            isRateBlocked
                                ? "Official website announcement requests were rate blocked."
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
        foreach (var language in Enum.GetValues<AnnouncementLanguage>()) {
            var lastModifiedUtc = crawlStateController.FindLastModifiedUtc(language);
            var modifiedAfterUtc = lastModifiedUtc - WatermarkOverlap;
            var responses = await newsClient.FetchAllAsync(language, modifiedAfterUtc, cancellationToken);

            if (responses.Count == 0) {
                continue;
            }

            var models = responses.Select(x => x.ToModels(language)).ToList();
            await OfficialSiteAnnouncementIndexController.BulkUpsert([..models.Select(x => x.Index)]);
            await SaveDetailsAndHistories(models.Select(x => x.Detail).ToList());

            await crawlStateController.Upsert(
                language,
                responses.Max(x => x.GetModifiedUtc())
            );
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

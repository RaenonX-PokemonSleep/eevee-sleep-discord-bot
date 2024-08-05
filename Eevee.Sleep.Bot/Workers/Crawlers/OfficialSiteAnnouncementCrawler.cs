using AngleSharp.Common;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement.OfficialSite;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;
using Eevee.Sleep.Bot.Workers.Scrapers.Announcement.OfficialSite;

namespace Eevee.Sleep.Bot.Workers.Crawlers;

public class OfficialSiteAnnouncementCrawler(
    ILogger<OfficialSiteAnnouncementCrawler> logger,
    AnnouncementDetailController<OfficialSiteAnnouncementDetailModel> detailController,
    AnnouncementHistoryController<OfficialSiteAnnouncementDetailModel> historyController
) : IAnnouncementCrawler {
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(10);

    // Used to run only one process when called by multiple workers at the same time.
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    public async Task ExecuteAsync(int retryCount = 0) {
        if (retryCount == 0) {
            await Semaphore.WaitAsync();
        }

        try {
            var indexes = await GetIndexes();
            await OfficialSiteAnnouncementIndexController.BulkUpsert([..indexes]);

            var details = await GetDetails(indexes);
            await SaveDetailsAndHistories(details.ToList());

            retryCount = 0;
        } catch (DocumentProcessingException e) {
            retryCount++;
            logger.LogError("{Message} Retries: {RetryCount}", e.Message, retryCount);

            if (retryCount >= IAnnouncementCrawler.MaxRetryCount) {
                logger.LogError("Failed to get indexes. Retry count exceeded.");
                throw new MaxAttemptExceededException("Failed to get indexes. Retry count exceeded.", e);
            }

            // Keep running until the attempt limit is exceeded
            await Task.Delay(RetryInterval);
            await ExecuteAsync(retryCount);
        } finally {
            if (Semaphore.CurrentCount == 0) {
                Semaphore.Release();
            }
        }
    }

    private static async Task<IList<OfficialSiteAnnouncementIndexModel>> GetIndexes() {
        Dictionary<string, AnnouncementLanguage> baseUrls = new() {
            { "https://www.pokemonsleep.net/news", AnnouncementLanguage.JP },
            { "https://www.pokemonsleep.net/en/news", AnnouncementLanguage.EN },
            { "https://www.pokemonsleep.net/zh/news", AnnouncementLanguage.ZH },
        };

        var tasks = baseUrls.Select(dict => IndexScraper.GetAllPagesAsync(dict.Key, dict.Value)).ToArray();
        var results = await Task.WhenAll(tasks);

        return results.SelectMany(x => x).ToList();
    }

    private static async Task<IEnumerable<OfficialSiteAnnouncementDetailModel>> GetDetails(
        IEnumerable<OfficialSiteAnnouncementIndexModel> indexes
    ) {
        var detailTasks = indexes
            .AsParallel()
            .WithDegreeOfParallelism(1)
            .Select(DetailScraper.GetAsync);

        return await Task.WhenAll(detailTasks);
    }

    private async Task SaveDetailsAndHistories(List<OfficialSiteAnnouncementDetailModel> details) {
        var existedDetails = detailController.FindAllByIds(details.Select(x => x.AnnouncementId));
        var existedDetailsById = existedDetails.ToDictionary(x => x.AnnouncementId);

        var shouldSaveDetail = (
            from detail in details
            let detailModel = existedDetailsById.GetOrDefault(detail.AnnouncementId, null)
            where detailModel?.ContentHash != detail.ContentHash
            select detail
        ).ToList();

        await detailController.BulkUpsert([..shouldSaveDetail]);
        await historyController.BulkInsert([..shouldSaveDetail]);
    }
}
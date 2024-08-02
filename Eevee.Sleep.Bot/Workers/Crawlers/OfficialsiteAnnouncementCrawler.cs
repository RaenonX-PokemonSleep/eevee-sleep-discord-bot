using AngleSharp.Common;
using Eevee.Sleep.Bot.Controllers.Mongo.InGameAnnouncement;
using Eevee.Sleep.Bot.Controllers.Mongo.InGameAnnouncement.Officialsite;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Models.InGameAnnouncement.Officialsite;
using Eevee.Sleep.Bot.Workers.Scrapers.InGameAnnouncement.Officialsite;

namespace Eevee.Sleep.Bot.Workers.Crawlers;

public class OfficialsiteAnnouncementCrawler(
    ILogger<OfficialsiteAnnouncementCrawler> logger,
    AnnouncementDetailController<OfficialsiteAnnouncementDetailModel> officialsiteAnnouncememntDetailController,
    AnnouncementHistoryController<OfficialsiteAnnouncementDetailModel> officialsiteAnnouncememntHistoryController
) : IAnnoucementCrawler {
    private const int MAX_RETRY_COUNT = 3;
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(10);
    // Used to run only one process when called by multiple workers at the same time.
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static async Task<IEnumerable<OfficialsiteAnnouncementIndexModel>> GetIndexes() {
        Dictionary<string, InGameAnnoucementLanguage> BaseUrls = new(){
            { "https://www.pokemonsleep.net/news", InGameAnnoucementLanguage.JP },
            { "https://www.pokemonsleep.net/en/news", InGameAnnoucementLanguage.EN },
            { "https://www.pokemonsleep.net/zh/news", InGameAnnoucementLanguage.ZH },
        };

        var tasks = BaseUrls.Select(dict => IndexScraper.GetAllPagesAsync(dict.Key, dict.Value)).ToArray();
        var results = await Task.WhenAll(tasks);

        return results.SelectMany(x => x);
    }

    private static async Task<IEnumerable<OfficialsiteAnnouncementDetailModel>> GetDetails(IEnumerable<OfficialsiteAnnouncementIndexModel> indexes) {
        var detailTasks = indexes
            .AsParallel()
            .WithDegreeOfParallelism(5)
            .Select(DetailScraper.GetAsync);
    
        return await Task.WhenAll(detailTasks);
    }

    private async Task SaveDetailsAndHistories(List<OfficialsiteAnnouncementDetailModel> details) {
        var existedDetails = officialsiteAnnouncememntDetailController.FindAllByIds(details.Select(x => x.AnnouncementId));
        var existedDetailsById = existedDetails.ToDictionary(x => x.AnnouncementId);
    
        var shouldSaveDetail = new List<OfficialsiteAnnouncementDetailModel>();
    
        foreach (var detail in details) {
            var detailModel = existedDetailsById.GetOrDefault(detail.AnnouncementId, null);
            if (detailModel?.ContentHash != detail.ContentHash) {
                shouldSaveDetail.Add(detail);
            }
        }
        await officialsiteAnnouncememntDetailController.BulkUpsert([..shouldSaveDetail]);
        await officialsiteAnnouncememntHistoryController.BulkInsert([..shouldSaveDetail]);
    }

    public async Task ExecuteAsync(int retryCount = 0) {
        if (retryCount == 0) {
            await Semaphore.WaitAsync();
        } 

        try {
            var indexes = await GetIndexes();
            await OfficialsiteAnnouncememntIndexController.BulkUpsert([..indexes]);
            
            var details = await GetDetails(indexes);
            await SaveDetailsAndHistories(details.ToList());

            retryCount = 0;
        } catch (DocumentProcessingException e) {
            retryCount++;
            logger.LogError("{Message} Retries: {RetryCount}", e.Message, retryCount);

            if (retryCount >= MAX_RETRY_COUNT) {
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
}
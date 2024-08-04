using Eevee.Sleep.Bot.Controllers.Mongo.Announcement;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement.InGame;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Models.Announcement.InGame;
using Eevee.Sleep.Bot.Workers.Scrapers;

namespace Eevee.Sleep.Bot.Workers.Crawlers;

public class InGameAnnouncementCrawler(
    ILogger<InGameAnnouncementCrawler> logger,
    AnnouncementDetailController<InGameAnnouncementDetailModel> InGameAnnouncementDetailController,
    AnnouncementHistoryController<InGameAnnouncementDetailModel> InGameAnnouncementHistoryController
) : IAnnoucementCrawler {
    private const int MAX_RETRY_COUNT = 3;

    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(10);

    // Used to run only one process when called by multiple workers at the same time.
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static readonly Dictionary<string, AnnouncementLanguage> Urls = new(){
        { "https://view.sleep.pokemon.co.jp/news/news_list/data/36922246/1/list_0_0.json", AnnouncementLanguage.JP },
        { "https://view.sleep.pokemon.co.jp/news/news_list/data/36922246/1/list_1_0.json", AnnouncementLanguage.JP },
        { "https://view.sleep.pokemon.co.jp/news/news_list/data/36922246/1/list_2_0.json", AnnouncementLanguage.JP },
        { "https://view.sleep.pokemon.co.jp/news/news_list/data/36922246/2/list_0_0.json", AnnouncementLanguage.EN },
        { "https://view.sleep.pokemon.co.jp/news/news_list/data/36922246/2/list_1_0.json", AnnouncementLanguage.EN },
        { "https://view.sleep.pokemon.co.jp/news/news_list/data/36922246/2/list_2_0.json", AnnouncementLanguage.EN },
        { "https://view.sleep.pokemon.co.jp/news/news_list/data/36922246/8/list_0_0.json", AnnouncementLanguage.ZH },
        { "https://view.sleep.pokemon.co.jp/news/news_list/data/36922246/8/list_1_0.json", AnnouncementLanguage.ZH },
        { "https://view.sleep.pokemon.co.jp/news/news_list/data/36922246/8/list_2_0.json", AnnouncementLanguage.ZH }
    };

    public async Task ExecuteAsync(int retryCount = 0) {
        if (retryCount == 0) {
            await Semaphore.WaitAsync();
        }

        try {
            var indexTasks = Urls.Select(async url => (await JsonDocumentFetcher<IEnumerable<InGameAnnouncementIndexResponse>>.FetchAsync(url.Key))
                .Select(x => x.ToModel(url.Key, url.Value))
            );
            var indexes = (await Task.WhenAll(indexTasks)).SelectMany(x => x);
            var updatedIndexes = await InGameAnnouncementIndexController.BulkUpsert([..indexes]);

            var detailTasks = updatedIndexes
                .AsParallel()
                .WithDegreeOfParallelism(5)
                .Select(async index => (await JsonDocumentFetcher<InGameAnnouncementDetailResponse>.FetchAsync(index.Url))
                    .ToModel(index.Url, index.Language)
                );
            var details = await Task.WhenAll(detailTasks);
            await InGameAnnouncementDetailController.BulkUpsert([..details]);
            await InGameAnnouncementHistoryController.BulkInsert([..details]);

            retryCount = 0;
        } catch (DocumentProcessingException e) {
            retryCount++;
            logger.LogError("{Message} Retries: {RetryCount}", e.Message, retryCount);

            if (retryCount >= MAX_RETRY_COUNT) {
                logger.LogError("Failed to get in-game news. Retry count exceeded.");
                throw new MaxAttemptExceededException("Failed to get in-game news. Retry count exceeded.", e);
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
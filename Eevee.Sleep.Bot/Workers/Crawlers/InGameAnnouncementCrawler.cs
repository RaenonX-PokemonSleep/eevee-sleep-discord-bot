using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement.InGame;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Models.Announcement.InGame;
using Eevee.Sleep.Bot.Modules.ExternalServices;
using Eevee.Sleep.Bot.Workers.Scrapers;

namespace Eevee.Sleep.Bot.Workers.Crawlers;

public class InGameAnnouncementCrawler(
    ILogger<InGameAnnouncementCrawler> logger,
    DiscordSocketClient client,
    AnnouncementDetailController<InGameAnnouncementDetailModel> detailController,
    AnnouncementHistoryController<InGameAnnouncementDetailModel> historyController
) : IAnnouncementCrawler {
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);

    // Used to run only one process when called by multiple workers at the same time.
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    public async Task ExecuteAsync(int retryCount = 0) {
        if (retryCount == 0) {
            await Semaphore.WaitAsync();
        }

        try {
            var urls = await GetAnnouncementUrls();
            var indexTasks = urls.Select(
                async url =>
                    (await JsonDocumentFetcher<IEnumerable<InGameAnnouncementIndexResponse>>.FetchAsync(url.Key))
                    .Select(x => x.ToModel(url.Key, url.Value))
            );
            var indexes = (await Task.WhenAll(indexTasks)).SelectMany(x => x);
            var updatedIndexes = await InGameAnnouncementIndexController.BulkUpsert([..indexes]);

            var detailTasks = updatedIndexes
                .AsParallel()
                .WithDegreeOfParallelism(5)
                .Select(
                    async index =>
                        (await JsonDocumentFetcher<InGameAnnouncementDetailResponse>.FetchAsync(index.Url))
                        .ToModel(index.Url, index.Language)
                );
            var details = await Task.WhenAll(detailTasks);
            await detailController.BulkUpsert([..details]);
            await historyController.BulkInsert([..details]);

            retryCount = 0;
        } catch (OfficialServerInMaintenanceException) {
            logger.LogInformation("Official server in maintenance during in-game announcement check.");
        } catch (DocumentProcessingException e) {
            retryCount++;
            logger.LogError("{Message} Retries: {RetryCount}", e.Message, retryCount);

            if (e is not FetchVersionNumberFailedException && retryCount >= IAnnouncementCrawler.MaxRetryCount) {
                // Only stop re-trying if the failure is not originated from version number fetching
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

    private static Dictionary<string, AnnouncementLanguage> GetAnnouncementUrlsOfLanguage(
        AnnouncementLanguage language,
        uint version
    ) {
        return new Dictionary<string, AnnouncementLanguage> {
            {
                $"https://view.sleep.pokemon.co.jp/news/news_list/data/android/{version}/{Convert.ToInt32(language)}/list_0_0.json",
                language
            }, {
                $"https://view.sleep.pokemon.co.jp/news/news_list/data/android/{version}/{Convert.ToInt32(language)}/list_1_0.json",
                language
            }, {
                $"https://view.sleep.pokemon.co.jp/news/news_list/data/android/{version}/{Convert.ToInt32(language)}/list_2_0.json",
                language
            },
        };
    }

    private async Task<Dictionary<string, AnnouncementLanguage>> GetAnnouncementUrls() {
        var currentVersion = await ChesterMicroservice.FetchCurrentVersion(client);

        logger.LogInformation("Current game data versions: {currentVersion}", currentVersion);

        return new[] {
                GetAnnouncementUrlsOfLanguage(AnnouncementLanguage.JP, currentVersion.InV),
                GetAnnouncementUrlsOfLanguage(AnnouncementLanguage.EN, currentVersion.InV),
                GetAnnouncementUrlsOfLanguage(AnnouncementLanguage.ZH, currentVersion.InV),
            }
            .SelectMany(dict => dict)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
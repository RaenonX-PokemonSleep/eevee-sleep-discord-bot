using AngleSharp.Common;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo.InGameAnnouncement;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.InGameAnnouncement;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using Eevee.Sleep.Bot.Workers.Scrapers.InGameAnnouncement;

namespace Eevee.Sleep.Bot.Workers;

public class InGameAnnouncementCrawlingWorker(
    DiscordSocketClient client,
    ILogger<InGameAnnouncementCrawlingWorker> logger
) : BackgroundService {
    private readonly DiscordSocketClient _client = client;
    private readonly ILogger<InGameAnnouncementCrawlingWorker> _logger = logger;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private const int MAX_RETRY_COUNT = 3;
    private int retryCount = 0;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(60);

    private static async Task<IEnumerable<InGameAnnouncementIndexModel>> GetIndexes() {
        Dictionary<string, InGameAnnoucementLanguage> BaseUrls = new(){
            { "https://www.pokemonsleep.net/news", InGameAnnoucementLanguage.JP },
            { "https://www.pokemonsleep.net/en/news", InGameAnnoucementLanguage.EN },
            { "https://www.pokemonsleep.net/zh/news", InGameAnnoucementLanguage.ZH },
        };

        var tasks = BaseUrls.Select(dict => IndexScraper.GetAllPagesAsync(dict.Key, dict.Value)).ToArray();
        var results = await Task.WhenAll(tasks);

        return results.SelectMany(x => x);
    }

    private static async Task<IEnumerable<InGameAnnouncementDetailModel>> GetDetails(IEnumerable<InGameAnnouncementIndexModel> indexes) {
        var detailTasks = indexes
            .AsParallel()
            .WithDegreeOfParallelism(5)
            .Select(DetailScraper.GetAsync);
    
        return await Task.WhenAll(detailTasks);
    }

    private static async Task SaveDetailsAndHistories(IEnumerable<InGameAnnouncementDetailModel> details) {
        var existedDetails = InGameAnnouncementDetailController.FindAllByIds(details.Select(x => x.AnnouncementId));
        var existedDetailsById = existedDetails.ToDictionary(x => x.AnnouncementId);
    
        var shouldSaveDetail = new List<InGameAnnouncementDetailModel>();
    
        foreach (var detail in details) {
            var detailModel = existedDetailsById.GetOrDefault(detail.AnnouncementId, null);
            if (detailModel?.ContentHash != detail.ContentHash) {
                shouldSaveDetail.Add(detail);
            }
        }
        await InGameAnnouncementDetailController.BulkUpsert([..shouldSaveDetail]);
        await InGameAnnouncememntHistoryController.BulkUpsert([..shouldSaveDetail]);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("Starting in-game announcement update watcher.");
        cancellationToken.Register(() => _logger.LogInformation("Stopping in-game announcement update watcher: cancellation token received."));

        while (!cancellationToken.IsCancellationRequested) {
            _logger.LogInformation("Checking in-game announcement updates.");

            try {
                var indexes = await GetIndexes();
                await InGameAnnouncememntIndexController.BulkUpsert([..indexes]);
                
                var details = await GetDetails(indexes);
                await SaveDetailsAndHistories(details);

                retryCount = 0;
            } catch (DocumentProcessingException e) {
                retryCount++;
                _logger.LogError("{Message} Retries: {RetryCount}", e.Message, retryCount);

                if (retryCount >= MAX_RETRY_COUNT) {
                    _logger.LogError("Failed to get indexes. Retry count exceeded.");
                    await _client.SendMessageInAdminAlertChannel(
                        embed: DiscordMessageMakerForInGameAnnouncement.MakeDocumentProcessingErrorMessage(e)
                    );
                    _cancellationTokenSource.Cancel();
                    break;
                }
            }

            await Task.Delay(CheckInterval, cancellationToken);
        }
    }
}
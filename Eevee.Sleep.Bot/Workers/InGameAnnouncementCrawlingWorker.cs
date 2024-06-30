using AngleSharp.Common;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Workers.Scrapers.InGameAnnouncement;

namespace Eevee.Sleep.Bot.Workers;
public class InGameAnnouncementCrawlingWorker(
        DiscordSocketClient client,
        ILogger<InGameAnnouncementCrawlingWorker> logger
    ) : BackgroundService {
    private readonly DiscordSocketClient _client = client;
    private readonly ILogger<InGameAnnouncementCrawlingWorker> _logger = logger;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("InGameAnnouncementUpdateCheckWorker is starting.");
        cancellationToken.Register(() => _logger.LogInformation("InGameAnnouncementUpdateCheckWorker background task is stopping."));

        while (!cancellationToken.IsCancellationRequested) {
            _logger.LogInformation("InGameAnnouncementUpdateCheckWorker task doing background work.");

            try {
                var indexes = await GetIndexes();
                await InGameAnnouncememntIndexController.BulkUpsert([.. indexes]);
                
                var details = await GetDetails(indexes);
                await SaveDetailsAndHistories(details);
            } catch (ContentStructureChangedException e) {
                _logger.LogError("Failed to get indexes. Web page structure may have changed.");
                await _client.SendMessageInAdminAlertChannel(
                    embed: DiscordMessageMaker.MakeContentStructureChangedMessage(e)
                );
                _cancellationTokenSource.Cancel();
                break;
            }

            await Task.Delay(CheckInterval, cancellationToken);
        }
    }

    private static async Task<IEnumerable<InGameAnnouncementIndexModel>> GetIndexes() {
        Dictionary<string, InGameAnnoucementLanguages> urls = new(){
            { "https://www.pokemonsleep.net/news/", InGameAnnoucementLanguages.JP },
            { "https://www.pokemonsleep.net/news/page/2", InGameAnnoucementLanguages.JP },
            { "https://www.pokemonsleep.net/en/news/", InGameAnnoucementLanguages.EN },
            { "https://www.pokemonsleep.net/zh/news/", InGameAnnoucementLanguages.ZH },
        };

        var tasks = urls.Select(dict => IndexScraper.GetAsync(dict.Key, dict.Value)).ToArray();
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
        var detailModels = InGameAnnouncementDetailController.FindAllByIds(details.Select(x => x.AnnouncementId));
        var detailModelsById = detailModels.ToDictionary(x => x.AnnouncementId);
    
        List<InGameAnnouncementDetailModel> shouldSaveDetail = [];
    
        foreach (var detail in details) {
            var detailModel = detailModelsById.GetOrDefault(detail.AnnouncementId, null);
            if (detailModel?.ContentHash != detail.ContentHash) {
                shouldSaveDetail.Add(detail!);
            }
        }
        await InGameAnnouncementDetailController.BulkUpsert([.. shouldSaveDetail]);
        await InGameAnnouncememntHistoryController.BulkUpsert([.. shouldSaveDetail]);
    }
}
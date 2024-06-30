using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Workers;

public class InGameAnnouncementUpdateCheckWorker(
    DiscordSocketClient client,
    ILogger<InGameAnnouncementUpdateCheckWorker> logger
) : BackgroundService {
    private readonly DiscordSocketClient _client = client;
    private readonly ILogger<InGameAnnouncementUpdateCheckWorker> _logger = logger;

     protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
        var options = new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<InGameAnnouncementDetailModel>>()
            .Match(x =>
                x.OperationType == ChangeStreamOperationType.Update ||
                x.OperationType == ChangeStreamOperationType.Modify || 
                x.OperationType == ChangeStreamOperationType.Insert
            );

        using var cursor = await MongoConst.InGameAnnouncementDetailCollection
            .WatchAsync(pipeline, options, cancellationToken);

        await cursor.ForEachAsync(async change => {
            var detail = change.FullDocument;

            _logger.LogInformation(
                "Received Detail Update {title}, {id}",
                detail.Title,
                detail.Url
            );

            await _client.SendMessageInInGameAnnouncementNoticeChannelAsync(
                language: detail.Language,
                embed: DiscordMessageMaker.MakeInGameAnnouncementUpdateMessage(detail)
            );
        }, cancellationToken);
    }
}
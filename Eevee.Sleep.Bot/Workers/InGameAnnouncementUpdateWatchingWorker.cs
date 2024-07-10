using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.InGameAnnouncement;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Workers;

public class InGameAnnouncementUpdateWatchingWorker(
    DiscordSocketClient client,
    ILogger<InGameAnnouncementUpdateWatchingWorker> logger
) : BackgroundService {
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

            logger.LogInformation(
                "Received in-game announcement detail update in {Language} ({Title} / #{Id})",
                detail.Language,
                detail.Title,
                detail.AnnouncementId
            );

            var notifyRole = ConfigHelper.GetInGameAnnouncementNotificationRoleId(detail.Language);

            await client.SendMessageInInGameAnnouncementNoticeChannelAsync(
                message: notifyRole is not null ? MentionUtils.MentionRole(notifyRole.Value) : null,
                language: detail.Language,
                embed: DiscordMessageMakerForInGameAnnouncement.MakeInGameAnnouncementUpdateMessage(detail)
            );
        }, cancellationToken);
    }
}
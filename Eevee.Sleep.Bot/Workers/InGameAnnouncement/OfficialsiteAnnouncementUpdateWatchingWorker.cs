using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.InGameAnnouncement.Officialsite;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using Eevee.Sleep.Bot.Workers.Crawlers;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Workers.InGameAnnouncement;

public class OfficialsiteAnnouncementUpdateWatchingWorker: AnnouncementUpdateWatchingWorker<OfficialsiteAnnouncementDetailModel> {
    private readonly DiscordSocketClient _client;

    public OfficialsiteAnnouncementUpdateWatchingWorker(
        OfficialsiteAnnouncementCrawler crawler,
        DiscordSocketClient client,
        ILogger<OfficialsiteAnnouncementUpdateWatchingWorker> logger
    ) : base(crawler, client, logger) {
        _client = client;
    }

    protected override IMongoCollection<OfficialsiteAnnouncementDetailModel> GetMongoCollection() {
        return MongoConst.OfficialsiteAnnouncementDetailCollection;
    }

    protected override ulong? GetNotifyRoleId(InGameAnnouncementLanguage language) {
        return null;
    }

    protected override Embed MakeAnnouncementUpdateMessage(OfficialsiteAnnouncementDetailModel detail) {
        return DiscordMessageMakerForInGameAnnouncement.MakeOfficialsiteAnnouncementUpdateMessage(detail);
    }

    protected override Task SendMessageInAnnouncementNoticeChannelAsync(
        string? message,
        InGameAnnouncementLanguage language,
        Embed embed
    ) {
        return _client.SendMessageInOfficialsiteAnnouncementNoticeChannelAsync(
            message: message,
            embed: embed
        );
    }
}
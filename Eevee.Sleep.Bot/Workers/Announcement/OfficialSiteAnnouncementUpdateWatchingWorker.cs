using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using Eevee.Sleep.Bot.Workers.Crawlers;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Workers.Announcement;

public class OfficialSiteAnnouncementUpdateWatchingWorker: AnnouncementUpdateWatchingWorker<OfficialSiteAnnouncementDetailModel> {
    private readonly DiscordSocketClient _client;

    public OfficialSiteAnnouncementUpdateWatchingWorker(
        OfficialSiteAnnouncementCrawler crawler,
        DiscordSocketClient client,
        ILogger<OfficialSiteAnnouncementUpdateWatchingWorker> logger
    ) : base(crawler, client, logger) {
        _client = client;
    }

    protected override IMongoCollection<OfficialSiteAnnouncementDetailModel> GetMongoCollection() {
        return MongoConst.OfficialSiteAnnouncementDetailCollection;
    }

    protected override ulong? GetNotifyRoleId(AnnouncementLanguage language) {
        return null;
    }

    protected override Embed MakeAnnouncementUpdateMessage(OfficialSiteAnnouncementDetailModel detail) {
        return DiscordMessageMakerForAnnouncement.MakeOfficialSiteAnnouncementUpdateMessage(detail);
    }

    protected override Task SendMessageInAnnouncementNoticeChannelAsync(
        string? message,
        AnnouncementLanguage language,
        Embed embed
    ) {
        return _client.SendMessageInOfficialSiteAnnouncementNoticeChannelAsync(
            message: message,
            embed: embed
        );
    }
}
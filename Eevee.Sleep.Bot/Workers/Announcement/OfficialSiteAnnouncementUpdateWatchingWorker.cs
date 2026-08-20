using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using Eevee.Sleep.Bot.Workers.Crawlers;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Workers.Announcement;

public class OfficialSiteAnnouncementUpdateWatchingWorker(
    OfficialSiteAnnouncementCrawler crawler,
    AnnouncementHistoryController<OfficialSiteAnnouncementDetailModel> historyController,
    DiscordSocketClient client,
    ILogger<OfficialSiteAnnouncementUpdateWatchingWorker> logger
) : AnnouncementUpdateWatchingWorker<OfficialSiteAnnouncementDetailModel>(crawler, historyController, client, logger) {
    private readonly DiscordSocketClient _client = client;

    protected override IMongoCollection<OfficialSiteAnnouncementDetailModel> GetMongoCollection() {
        return MongoConst.OfficialSiteAnnouncementDetailCollection;
    }

    protected override ulong? GetNotifyRoleId(AnnouncementLanguage language) {
        return null;
    }

    protected override Embed MakeAnnouncementUpdateMessage(OfficialSiteAnnouncementDetailModel detail, bool isNew) {
        return DiscordMessageMakerForAnnouncement.MakeOfficialSiteAnnouncementUpdateMessage(detail, isNew);
    }

    protected override bool HasSameContent(
        OfficialSiteAnnouncementDetailModel first,
        OfficialSiteAnnouncementDetailModel second
    ) {
        return first.ContentHash == second.ContentHash;
    }

    protected override string GetContent(OfficialSiteAnnouncementDetailModel detail) {
        return detail.Content;
    }

    protected override string GetSourceName() {
        return "Official Website";
    }

    protected override string GetDisplayUrl(OfficialSiteAnnouncementDetailModel detail) {
        return detail.Url;
    }

    protected override Task SendMessageInAnnouncementNoticeChannelAsync(
        string? message,
        AnnouncementLanguage language,
        Embed embed
    ) {
        return _client.SendMessageInOfficialSiteAnnouncementNoticeChannelAsync(language, message, embed);
    }
}

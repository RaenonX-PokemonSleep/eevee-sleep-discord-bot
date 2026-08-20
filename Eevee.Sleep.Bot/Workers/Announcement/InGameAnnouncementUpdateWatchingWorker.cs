using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement.InGame;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using Eevee.Sleep.Bot.Workers.Crawlers;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Workers.Announcement;

public class InGameAnnouncementUpdateWatchingWorker(
    InGameAnnouncementCrawler crawler,
    AnnouncementHistoryController<InGameAnnouncementDetailModel> historyController,
    DiscordSocketClient client,
    ILogger<InGameAnnouncementUpdateWatchingWorker> logger
) : AnnouncementUpdateWatchingWorker<InGameAnnouncementDetailModel>(crawler, historyController, client, logger) {
    private readonly DiscordSocketClient _client = client;

    protected override IMongoCollection<InGameAnnouncementDetailModel> GetMongoCollection() {
        return MongoConst.InGameAnnouncementDetailCollection;
    }

    protected override ulong? GetNotifyRoleId(AnnouncementLanguage language) {
        return ConfigHelper.GetInGameAnnouncementNotificationRoleId(language);
    }

    protected override Embed MakeAnnouncementUpdateMessage(InGameAnnouncementDetailModel detail, bool isNew) {
        return DiscordMessageMakerForAnnouncement.MakeInGameAnnouncementUpdateMessage(detail, isNew);
    }

    protected override bool HasSameContent(
        InGameAnnouncementDetailModel first,
        InGameAnnouncementDetailModel second
    ) {
        return first.Text == second.Text;
    }

    protected override string GetContent(InGameAnnouncementDetailModel detail) {
        return detail.Text;
    }

    protected override string GetSourceName() {
        return "In-game";
    }

    protected override string GetDisplayUrl(InGameAnnouncementDetailModel detail) {
        return ConfigHelper.GetGameAnnouncementProxyUrl(detail.AnnouncementId);
    }

    protected override Task SendMessageInAnnouncementNoticeChannelAsync(
        string? message,
        AnnouncementLanguage language,
        Embed embed
    ) {
        return _client.SendMessageInInGameAnnouncementNoticeChannelAsync(
            language,
            message,
            embed
        );
    }
}

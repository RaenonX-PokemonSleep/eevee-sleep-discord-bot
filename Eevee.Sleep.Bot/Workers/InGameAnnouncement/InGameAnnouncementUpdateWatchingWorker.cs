using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.InGameAnnouncement.InGame;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using Eevee.Sleep.Bot.Workers.Crawlers;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Workers.InGameAnnouncement;

public class InGameAnnouncementUpdateWatchingWorker: AnnouncementUpdateWatchingWorker<InGameAnnouncementDetailModel> {
    private readonly DiscordSocketClient _client;

    public InGameAnnouncementUpdateWatchingWorker(
        InGameAnnouncementCrawler crawler,
        DiscordSocketClient client,
        ILogger<InGameAnnouncementUpdateWatchingWorker> logger
    ) : base(crawler, client, logger) {
        _client = client;
    }

    protected override IMongoCollection<InGameAnnouncementDetailModel> GetMongoCollection() {
        return MongoConst.InGameAnnouncementDetailCollection;
    }

    protected override ulong? GetNotifyRoleId(InGameAnnouncementLanguage language) {
        return ConfigHelper.GetInGameAnnouncementNotificationRoleId(language);
    }

    protected override Embed MakeAnnouncementUpdateMessage(InGameAnnouncementDetailModel detail) {
        return DiscordMessageMakerForInGameAnnouncement.MakeInGameAnnouncementUpdateMessage(detail);
    }

    protected override Task SendMessageInAnnouncementNoticeChannelAsync(
        string? message,
        InGameAnnouncementLanguage language,
        Embed embed
    ) {
        return _client.SendMessageInInGameAnnouncementNoticeChannelAsync(
            language: language,
            message: message,
            embed: embed
        );
    }
}
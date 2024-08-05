using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Handlers;
using Eevee.Sleep.Bot.Models.Announcement.InGame;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;
using Eevee.Sleep.Bot.Workers;
using Eevee.Sleep.Bot.Workers.Announcement;
using Eevee.Sleep.Bot.Workers.Crawlers;

var socketConfig = new DiscordSocketConfig {
    GatewayIntents =
        GatewayIntents.All &
        ~GatewayIntents.GuildScheduledEvents &
        ~GatewayIntents.GuildInvites &
        ~GatewayIntents.GuildPresences,
    AlwaysDownloadUsers = true,
};

var builder = WebApplication.CreateBuilder(args)
    .BuildCommon();

builder.Services.AddCorsFromConfig();
builder.Services.AddSingleton(socketConfig)
    .AddSingleton<DiscordSocketClient>()
    .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
    .AddSingleton<InteractionHandler>()
    .AddSingleton(
        new AnnouncementDetailController<OfficialSiteAnnouncementDetailModel>(
            MongoConst.OfficialSiteAnnouncementDetailCollection
        )
    )
    .AddSingleton(
        new AnnouncementHistoryController<OfficialSiteAnnouncementDetailModel>(
            MongoConst.OfficialSiteAnnouncementHistoryCollection
        )
    )
    .AddSingleton(
        new AnnouncementDetailController<InGameAnnouncementDetailModel>(
            MongoConst.InGameAnnouncementDetailCollection
        )
    )
    .AddSingleton(
        new AnnouncementHistoryController<InGameAnnouncementDetailModel>(
            MongoConst.InGameAnnouncementHistoryCollection
        )
    )
    .AddSingleton<OfficialSiteAnnouncementCrawler>()
    .AddSingleton<InGameAnnouncementCrawler>()
    .AddHostedService<DiscordClientWorker>()
    .AddHostedService<OfficialSiteAnnouncementUpdateWatchingWorker>()
    .AddHostedService<InGameAnnouncementUpdateWatchingWorker>()
    .AddHostedService<OfficialSiteAnnouncementCrawlingWorker>()
    .AddHostedService<InGameAnnouncementCrawlingWorker>()
    .AddControllers();

var app = builder
    .Build()
    .InitLogging();

app.MapControllers();
await app.BootAsync();
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Controllers.Mongo.InGameAnnouncement;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Handlers;
using Eevee.Sleep.Bot.Models.InGameAnnouncement.InGame;
using Eevee.Sleep.Bot.Models.InGameAnnouncement.Officialsite;
using Eevee.Sleep.Bot.Workers;
using Eevee.Sleep.Bot.Workers.InGameAnnouncement;
using Eevee.Sleep.Bot.Workers.Crawlers;

var socketConfig = new DiscordSocketConfig {
    GatewayIntents =
        GatewayIntents.All &
        ~GatewayIntents.GuildScheduledEvents &
        ~GatewayIntents.GuildInvites &
        ~GatewayIntents.GuildPresences,
    AlwaysDownloadUsers = true
};

var builder = WebApplication.CreateBuilder(args)
    .BuildCommon();

builder.Services.AddCorsFromConfig();
builder.Services.AddSingleton(socketConfig)
    .AddSingleton<DiscordSocketClient>()
    .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
    .AddSingleton<InteractionHandler>()
    .AddSingleton(x => new AnnouncementDetailController<OfficialsiteAnnouncementDetailModel>(
        MongoConst.OfficialsiteAnnouncementDetailCollection
    ))
    .AddSingleton(x => new AnnouncementHistoryController<OfficialsiteAnnouncementDetailModel>(
        MongoConst.OfficialsiteAnnouncementHistoryCollection
    ))
    .AddSingleton(x => new AnnouncementDetailController<InGameAnnouncementDetailModel>(
        MongoConst.InGameAnnouncementDetailCollection
    ))
    .AddSingleton(x => new AnnouncementHistoryController<InGameAnnouncementDetailModel>(
        MongoConst.InGameAnnouncementHistoryCollection
    ))
    .AddSingleton<OfficialsiteAnnouncementCrawler>()
    .AddSingleton<InGameAnnouncementCrawler>()
    .AddHostedService<DiscordClientWorker>()
    .AddHostedService<OfficialsiteAnnouncementUpdateWatchingWorker>()
    .AddHostedService<InGameAnnouncementUpdateWatchingWorker>()
    .AddHostedService<OfficialsiteAnnouncementCrawlingWorker>()
    .AddHostedService<InGameAnnouncementCrawlingWorker>()
    .AddControllers();

var app = builder
    .Build()
    .InitLogging();

app.MapControllers();
await app.BootAsync();
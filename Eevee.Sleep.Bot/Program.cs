using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Handlers;
using Eevee.Sleep.Bot.Workers;
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
    .AddSingleton<OfficialSiteAnnouncementCrawler>()
    .AddSingleton<InGameAnnouncementCrawler>()
    .AddHostedService<DiscordClientWorker>()
    .AddHostedService<InGameAnnouncementUpdateWatchingWorker>()
    .AddHostedService(x => new AnnouncementCrawlingWorker(
        x.GetRequiredService<InGameAnnouncementCrawler>(),
        x.GetRequiredService<DiscordSocketClient>(),
        x.GetRequiredService<ILogger<AnnouncementCrawlingWorker>>()
    ))
    .AddHostedService(x => new AnnouncementCrawlingWorker(
        x.GetRequiredService<OfficialSiteAnnouncementCrawler>(),
        x.GetRequiredService<DiscordSocketClient>(),
        x.GetRequiredService<ILogger<AnnouncementCrawlingWorker>>()
    ))
    .AddControllers();

var app = builder
    .Build()
    .InitLogging();

app.MapControllers();
await app.BootAsync();
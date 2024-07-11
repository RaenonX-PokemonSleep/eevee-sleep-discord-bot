using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Handlers;
using Eevee.Sleep.Bot.Workers;

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
    .AddSingleton<InGameAnnouncementCrawler>()
    .AddHostedService<DiscordClientWorker>()
    .AddHostedService<InGameAnnouncementUpdateWatchingWorker>()
    .AddHostedService<InGameAnnouncementCrawlingWorker>()
    .AddControllers();

var app = builder
    .Build()
    .InitLogging();

app.MapControllers();
await app.BootAsync();
using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Handlers;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Workers;

public class DiscordClientWorker : BackgroundService {
    private readonly IServiceProvider _services;

    private readonly DiscordSocketClient _client;

    public DiscordClientWorker(IServiceProvider services, DiscordSocketClient client) {
        _services = services;
        _client = client;
    }

    private async Task SendTestMessage() {
        var messages = await Task.WhenAll(
            (await _client.GetAdminAlertChannelAsync()).SendMessageAsync("`SYSTEM` Admin alert sending test")
        );

        await Task.Delay(TimeSpan.FromSeconds(30));

        await Task.WhenAll(messages.Select(x => x.DeleteAsync()));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
        _client.Log += OnLogHandler.OnLogAsync;

        await _services.GetRequiredService<InteractionHandler>().InitializeAsync();

        await _client.LoginAsync(TokenType.Bot, ConfigHelper.GetDiscordToken());
        await _client.StartAsync();

        await SendTestMessage();

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
}
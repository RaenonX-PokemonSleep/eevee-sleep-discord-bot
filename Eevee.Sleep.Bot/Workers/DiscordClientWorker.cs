using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Handlers;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Workers;

public class DiscordClientWorker(IServiceProvider services, DiscordSocketClient client) : BackgroundService {
    private async Task SendTestMessage() {
        var message = await client.SendMessageInAdminAlertChannel("`SYSTEM` Admin alert sending test");

        await Task.Delay(TimeSpan.FromSeconds(30));

        await message.DeleteAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
        client.Log += OnLogHandler.OnLogAsync;

        await services.GetRequiredService<InteractionHandler>().InitializeAsync();

        await client.LoginAsync(TokenType.Bot, ConfigHelper.GetDiscordToken());
        await client.StartAsync();

        await SendTestMessage();

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
}
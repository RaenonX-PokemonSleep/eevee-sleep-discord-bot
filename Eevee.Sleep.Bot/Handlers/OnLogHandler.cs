using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Handlers;

public static class OnLogHandler {
    private static readonly ILogger Logger = LogHelper.CreateLogger(typeof(OnLogHandler));

    public static async Task OnLogAsync(DiscordSocketClient client, LogMessage message) {
        if (
            message.Exception is not null &&
            message.Exception is not GatewayReconnectException &
            message.Exception.Message != "WebSocket connection was closed"
        ) {
            await client.SendMessageInAdminAlertChannel(embed: DiscordMessageMaker.MakeErrorFromLog(message));
        }

        Logger.Log(
            message.Severity.ToLogLevel(),
            message.Exception,
            "{Source}: {Message}",
            message.Source,
            message.Message
        );
    }
}
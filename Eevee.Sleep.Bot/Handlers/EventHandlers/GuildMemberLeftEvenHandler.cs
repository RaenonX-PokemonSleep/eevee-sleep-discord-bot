using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Handlers.EventHandlers;

public static class GuildMemberLeftEventHandler {
    private static readonly ILogger Logger = LogHelper.CreateLogger(typeof(GuildMemberLeftEventHandler));

    public static Task OnEvent(SocketGuild guild, SocketUser user) {
        Logger.LogInformation(
            "User {UserId} (@{Username}) left the server, removing associated activation if any",
            user.Id,
            user.Username
        );
        return ActivationController.RemoveDiscordActivationData(user.Id.ToString());
    }
}
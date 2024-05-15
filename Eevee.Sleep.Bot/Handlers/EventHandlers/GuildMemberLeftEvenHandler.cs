using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Handlers.EventHandlers;

public static class GuildMemberLeftEventHandler {
    private static readonly ILogger Logger = LogHelper.CreateLogger(typeof(GuildMemberLeftEventHandler));

    public static async Task OnEvent(IDiscordClient client, SocketUser user) {
        Logger.LogInformation(
            "User {UserId} (@{Username}) left the server, removing associated activation if any",
            user.Id,
            user.Username
        );
        var subscriptionDuration = await ActivationController.RemoveDiscordActivationAndGetSubscriptionDuration(user.Id.ToString());

        if (subscriptionDuration is not null) {
            await client.SendMessageInAdminAlertChannel(
                embed: await DiscordMessageMaker.MakeUserUnsubscribed(user, subscriptionDuration)
            );
        }
    }
}
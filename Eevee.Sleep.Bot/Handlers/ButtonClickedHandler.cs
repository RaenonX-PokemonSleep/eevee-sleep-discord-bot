using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Models;

namespace Eevee.Sleep.Bot.Handlers;

public static class ButtonClickedHandler {
    public static async Task DisplayRoleButtonClicked(
        ButtonInteractionInfo info,
        SocketGuildUser user
    ) {
        await user.RemoveRolesAsync(
            roleIds: DiscordTrackedRoleController
                .FindAllTrackedRoleIdsByRoleIds(user.Roles.Select(r => r.Id).ToArray())
                .Select(r => r.Id)
        );
        await user.AddRoleAsync(info.CustomId);
    }

    public static Task AddRoleButtonClicked(
        ButtonInteractionInfo info,
        SocketGuildUser user
    ) {
        return user.AddRoleAsync(info.CustomId);
    }

    public static Task RemoveRoleButtonClicked(
        ButtonInteractionInfo info,
        SocketGuildUser user
    ) {
        return user.RemoveRoleAsync(info.CustomId);
    }
}
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Utils;
using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Modules.SlashCommands;

[Group("role", "Commands for managing the roles of a user.")]
public class RoleManagementSlashModule : InteractionModuleBase<SocketInteractionContext> {
    private async Task SendEphemeralMessageToBeDeletedAsync(string text, MessageComponent components) {
        await Context.Interaction.RespondAsync(
            text: text,
            components: components,
            ephemeral: true
        );

        // Ephemeral messages cannot be deleted by the button handler, so the message containing the button should be deleted after 30 seconds.
        await Task.Delay(TimeSpan.FromSeconds(30));
        await Context.Interaction.DeleteOriginalResponseAsync();
    }

    [SlashCommand("display", "Select the role to display.")]
    [UsedImplicitly]
    public Task DisplayRoleAsync() {
        string[] messages = [
            "All tracked roles will be removed from after selecting the role to display.",
            "Role ownership won't get affected by the removal, only the role assignment on Discord is affected.",
            "",
            "Select your role to display."
        ];
        return SendEphemeralMessageToBeDeletedAsync(
            string.Join("\n", messages),
            components: DiscordMessageMaker.MakeRoleSelectButton(
                roles: DiscordTrackedRoleController.FindAllTrackedRoleIdsByRoleIds(
                    DiscordRoleRecordController.FindRoleIdsByUserId(Context.User.Id)
                ),
                buttonId: ButtonId.RoleChanger
            )
        );
    }

    [SlashCommand("add", "Adds the selected role to the user on Discord.")]
    [UsedImplicitly]
    public Task AddRoleAsync() {
        var user =
            Context.User as SocketGuildUser ??
            throw new InvalidOperationException("User is not SocketGuildUser.");

        return SendEphemeralMessageToBeDeletedAsync(
            "You already have all the roles.",
            components: DiscordMessageMaker.MakeRoleSelectButton(
                roles: DiscordTrackedRoleController.FindAllTrackedRoleIdsByRoleIds(
                    // Find the role ids that the user does not have
                    DiscordRoleRecordController
                        .FindRoleIdsByUserId(Context.User.Id)
                        .Except(user.Roles.Select(x => x.Id))
                        .ToArray()
                ),
                buttonId: ButtonId.RoleAdder
            )
        );
    }

    [SlashCommand("remove", "Removes the selected role from a user on Discord.")]
    [UsedImplicitly]
    public Task RemoveRoleAsync() {
        var user =
            Context.User as SocketGuildUser ??
            throw new InvalidOperationException("User is not SocketGuildUser.");

        return SendEphemeralMessageToBeDeletedAsync(
            "You don't have any roles to remove.",
            components: DiscordMessageMaker.MakeRoleSelectButton(
                // Find the role ids that the user has
                roles: DiscordTrackedRoleController.FindAllTrackedRoleIdsByRoleIds(
                    user.Roles.Select(x => x.Id).ToArray()
                ),
                buttonId: ButtonId.RoleRemover
            )
        );
    }

    [SlashCommand("delete-record", "Deletes the role record of a specific user from the database.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [UsedImplicitly]
    public async Task DeleteRoleRecordAsync(IUser user, IRole role) {
        await DiscordRoleRecordController.RemoveRoles(user.Id, [role.Id]);

        await Context.Interaction.RespondAsync(
            embed: DiscordMessageMaker.MakeDeleteRoleResult(user, role.Id)
        );
    }

    [SlashCommand("track", "Tracks the specified role.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [UsedImplicitly]
    public async Task TrackRoleAsync(IRole role) {
        await DiscordTrackedRoleController.SaveTrackedRole(role);

        var roleOwnedUsers = Context.Guild.Users
            .Where(x => x.Roles.Contains(role))
            .Select(x => x.Id)
            .ToArray();

        await DiscordRoleRecordController.BulkAddRoles(roleOwnedUsers, [role.Id]);

        await Context.Interaction.RespondAsync(
            embed: DiscordMessageMaker.MakeTrackRoleResult(
                roleId: role.Id,
                roleOwnedUserCount: roleOwnedUsers.Length,
                message: "Role tracked.",
                color: Colors.Success
            )
        );
    }

    [SlashCommand("untrack", "Untracks the specified role.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [UsedImplicitly]
    public async Task UntrackRoleAsync(IRole role) {
        await DiscordTrackedRoleController.RemoveTrackedRole(role.Id);

        var roleOwnedUsers = Context.Guild.Users
            .Where(x => x.Roles.Contains(role))
            .Select(x => x.Id)
            .ToArray();
        await DiscordRoleRecordController.BulkRemoveRoles(roleOwnedUsers, [role.Id]);

        await Context.Interaction.RespondAsync(
            embed: DiscordMessageMaker.MakeTrackRoleResult(
                roleId: role.Id,
                roleOwnedUserCount: roleOwnedUsers.Length,
                message: "Role untracked.",
                color: Colors.Danger
            )
        );
    }
}
using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Enums;
using Discord.WebSocket;

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
                roles: DiscordTrackedRoleContoller.FindAllTrackedRoleIdsByRoleIds(
                    DiscordRoleRecordContoller.FindRoleIdsByUserId(Context.User.Id)
                ),
                buttonId: ButtonId.RoleChanger
            )
        );
    }

    [SlashCommand("add", "Add the role ownership of a user.")]
    [UsedImplicitly]
    public Task AddRoleAsync() {
        var user = Context.User is SocketGuildUser matchedUser ?
            matchedUser : 
            throw new InvalidOperationException("User is not SocketGuildUser.");

        return SendEphemeralMessageToBeDeletedAsync(
            "You already have all the roles.",
            components: DiscordMessageMaker.MakeRoleSelectButton(
                roles: DiscordTrackedRoleContoller.FindAllTrackedRoleIdsByRoleIds(
                    // Find the role ids that the user does not have
                    DiscordRoleRecordContoller
                        .FindRoleIdsByUserId(Context.User.Id)
                        .Except(user.Roles.Select(x => x.Id))
                        .ToArray()
                ),
                buttonId: ButtonId.RoleAdder
            )
        );
    }

    [SlashCommand("remove", "Remove the role ownership of a user.")]
    [UsedImplicitly]
    public Task RemoveRoleAsync() {
        var user = Context.User is SocketGuildUser matchedUser ?
            matchedUser : 
            throw new InvalidOperationException("User is not SocketGuildUser.");

        return SendEphemeralMessageToBeDeletedAsync(
            "You don't have any roles to remove.",
            components: DiscordMessageMaker.MakeRoleSelectButton(
                // Find the role ids that the user has
                roles: DiscordTrackedRoleContoller.FindAllTrackedRoleIdsByRoleIds(
                    user.Roles.Select(x => x.Id).ToArray()
                ),
                buttonId: ButtonId.RoleRemover
            )
        );
    }

    [SlashCommand("delete-record", "Delete a role owned by a specific user from the database.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [UsedImplicitly]
    public async Task DeleteRoleRecordAsync(IUser user, IRole role) {
        await DiscordRoleRecordContoller.RemoveRoles(user.Id, [role.Id]);

        await Context.Interaction.RespondAsync(
            embed: DiscordMessageMaker.MakeDeleteRoleResult(user, role.Id)
        );
    }

    [SlashCommand("track", "Track the specific role.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [UsedImplicitly]
    public async Task TrackRoleAsync(IRole role) {
        await DiscordTrackedRoleContoller.SaveTrackedRole(role);

        var roleOwnedUsers = Context.Guild.Users
            .Where(x => x.Roles.Contains(role))
            .Select(x => x.Id)
            .ToArray();

        await DiscordRoleRecordContoller.BulkAddRoles(roleOwnedUsers, [role.Id]);

        await Context.Interaction.RespondAsync(
            embed: DiscordMessageMaker.MakeTrackRoleResult(
                roleId: role.Id,
                trackedRoles: DiscordTrackedRoleContoller.FindAllTrackedRoles().Select(x => x.Id).ToArray(),
                roleOwnedUsercount: roleOwnedUsers.Length,
                message: "Role tracked.",
                color: Colors.Success
            )
        );
    }

    [SlashCommand("untrack", "Untrack the specified role.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [UsedImplicitly]
    public async Task UntrackRoleAsync(IRole role) {
        await DiscordTrackedRoleContoller.RemoveTrackedRole(role.Id);

        var roleOwnedUsers = Context.Guild.Users
            .Where(x => x.Roles.Contains(role))
            .Select(x => x.Id)
            .ToArray();
        await DiscordRoleRecordContoller.BulkRemoveRoles(roleOwnedUsers, [role.Id]);

        await Context.Interaction.RespondAsync(
            embed: DiscordMessageMaker.MakeTrackRoleResult(
                roleId: role.Id,
                trackedRoles: DiscordTrackedRoleContoller.FindAllTrackedRoles().Select(x => x.Id).ToArray(),
                roleOwnedUsercount: roleOwnedUsers.Length,
                message: "Role untracked.",
                color: Colors.Danger
            )
        );
    }
}
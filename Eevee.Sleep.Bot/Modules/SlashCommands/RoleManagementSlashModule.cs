using Discord;
using Discord.Interactions;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Utils;
using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Modules.SlashCommands;

[Group("role", "Commands for managing the roles of a user.")]
public class RoleManagementSlashModule : InteractionModuleBase<SocketInteractionContext> {
    private async Task SendEphemeralMessageToBeDeletedAsync(string text) {
        await Context.Interaction.RespondAsync(
            text: text,
            ephemeral: true
        );

        // Ephemeral messages cannot be deleted by the button handler, so the message containing the button should be deleted after 30 seconds.
        await Task.Delay(TimeSpan.FromSeconds(30));
        await Context.Interaction.DeleteOriginalResponseAsync();
    }

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
        var roles = DiscordTrackedRoleController.FindAllTrackedRoleIdsByRoleIds(
            DiscordRoleRecordController.FindRoleIdsByUserId(Context.User.Id)
        );

        if (roles.Length == 0) {
            return SendEphemeralMessageToBeDeletedAsync("No roles available for selection.");
        }

        string[] messages = [
            "Select the role to display.",
            "",
            "All tracked roles will be removed from after selecting the role to display.",
            "Role ownership won't get affected by the removal, only the role assignment on Discord is affected.",
            "",
            ..DiscordMessageMaker.MakeRoleSelectCorrespondenceList(roles)
        ];

        return SendEphemeralMessageToBeDeletedAsync(
            messages.MergeLines(),
            components: DiscordMessageMaker.MakeRoleSelectButton(
                roles: roles,
                buttonId: ButtonId.RoleChanger
            )
        );
    }

    [SlashCommand("add", "Adds the selected role to the user on Discord.")]
    [UsedImplicitly]
    public Task AddRoleAsync() {
        var user = Context.User.AsGuildUser();

        var roles = DiscordTrackedRoleController.FindAllTrackedRoleIdsByRoleIds(
            // Find the role ids that the user does not have
            DiscordRoleRecordController
                .FindRoleIdsByUserId(user.Id)
                .Except(user.Roles.Select(x => x.Id))
                .ToArray()
        );

        if (roles.Length == 0) {
            return SendEphemeralMessageToBeDeletedAsync("No roles available for addition.");
        }

        string[] messages = [
            "Select a role to obtain the ownership on Discord.",
            "This does not guarantee that the selected role will show. To ensure the selected role shows up, use `/role display` instead.",
            "",
            ..DiscordMessageMaker.MakeRoleSelectCorrespondenceList(roles)
        ];

        return SendEphemeralMessageToBeDeletedAsync(
            messages.MergeLines(),
            components: DiscordMessageMaker.MakeRoleSelectButton(
                roles: roles,
                buttonId: ButtonId.RoleAdder
            )
        );
    }

    [SlashCommand("remove", "Removes the selected role from a user on Discord.")]
    [UsedImplicitly]
    public Task RemoveRoleAsync() {
        var user = Context.User.AsGuildUser();

        var roles = DiscordTrackedRoleController.FindAllTrackedRoleIdsByRoleIds(
            user.Roles.Select(x => x.Id).ToArray()
        );

        if (roles.Length == 0) {
            return SendEphemeralMessageToBeDeletedAsync("No roles available for removal.");
        }

        string[] messages = [
            "Select a role to remove the ownership on Discord.",
            "This does not remove the actual ownership of the role. You can get them back using either `/role add` or `/role display` at any time.",
            "",
            ..DiscordMessageMaker.MakeRoleSelectCorrespondenceList(roles)
        ];

        return SendEphemeralMessageToBeDeletedAsync(
            messages.MergeLines(),
            components: DiscordMessageMaker.MakeRoleSelectButton(
                roles: roles,
                buttonId: ButtonId.RoleRemover
            )
        );
    }

    [SlashCommand("add-all", "Add all owned tracked roles.")]
    [UsedImplicitly]
    public async Task AddAllRoleAsync() {
        var user = Context.User.AsGuildUser();

        var previousRoleIds = DiscordTrackedRoleController
            .FindAllTrackedRoleIdsByRoleIds(user.Roles.Select(x => x.Id).ToArray())
            .Select(x => x.RoleId)
            .ToArray();

        await user.AddRolesAsync(DiscordRoleRecordController.FindRoleIdsByUserId(user.Id));

        await Context.Interaction.RespondAsync(
            embed: DiscordMessageMaker.MakeChangeRoleResult(
                user: user,
                previousRoleIds: previousRoleIds,
                currentRoleIds: DiscordRoleRecordController.FindRoleIdsByUserId(user.Id),
                color: Colors.Success
            ),
            ephemeral: true
        );
    }

    [SlashCommand("remove-all", "Remove all tracked roles.")]
    [UsedImplicitly]
    public async Task RemoveAllRoleAsync() {
        var user = Context.User.AsGuildUser();

        var previousRoleIds = DiscordTrackedRoleController
            .FindAllTrackedRoleIdsByRoleIds(user.Roles.Select(x => x.Id).ToArray())
            .Select(x => x.RoleId)
            .ToArray();

        await user.RemoveRolesAsync(DiscordRoleRecordController.FindRoleIdsByUserId(user.Id));

        await Context.Interaction.RespondAsync(
            embed: DiscordMessageMaker.MakeChangeRoleResult(
                user: user,
                previousRoleIds: previousRoleIds,
                currentRoleIds: [],
                color: Colors.Danger
            ),
            ephemeral: true
        );
    }

    [SlashCommand("show", "Shows all the owned tracked roles.")]
    [UsedImplicitly]
    public Task ShowRoleAsync() {
        var user = Context.User.AsGuildUser();

        return Context.Interaction.RespondAsync(
            embed: DiscordMessageMaker.MakeShowRoleResult(
                user: user,
                roleIds: DiscordRoleRecordController.FindRoleIdsByUserId(user.Id)
            ),
            ephemeral: true
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

    [SlashCommand("untrack", "Untrack the specified role.")]
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

    [SlashCommand("show-tracked", "Show all tracked roles.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [UsedImplicitly]
    public async Task ShowTrackedRolesAsync() {
        var trackedRoles = DiscordTrackedRoleController.FindAllTrackedRoles()
            .Select(x => $"- {MentionUtils.MentionRole(x.RoleId)}")
            .ToArray();

        string[] messages = [
            $"Currently tracked roles ({trackedRoles.Length}):",
            ..trackedRoles
        ];

        await Context.Interaction.RespondAsync(messages.MergeLines());
    }
}
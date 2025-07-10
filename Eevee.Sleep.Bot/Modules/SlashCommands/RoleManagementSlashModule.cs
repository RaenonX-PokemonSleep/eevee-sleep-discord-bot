using Discord;
using Discord.Interactions;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Models.Pagination;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Modules.SlashCommands;

[Group("role", "Commands for managing the roles of a user.")]
[CommandContextType(InteractionContextType.Guild)]
public class RoleManagementSlashModule : InteractionModuleBase<SocketInteractionContext> {
    private async Task SendEphemeralMessageToBeDeletedAsync(string text) {
        await Context.Interaction.RespondAsync(
            text,
            ephemeral: true
        );

        // Ephemeral messages cannot be deleted by the button handler, so the message containing the button should be deleted after 30 seconds.
        await Task.Delay(TimeSpan.FromSeconds(30));
        await Context.Interaction.DeleteOriginalResponseAsync();
    }

    private async Task SendEphemeralMessageToBeDeletedAsync(string text, MessageComponent components) {
        await Context.Interaction.RespondAsync(
            text,
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

        DiscordPaginationContext<TrackedRoleModel>.SaveState(
            Context.User.Id.ToString(),
            new PaginationState<TrackedRoleModel> {
                CurrentPage = 1,
                TotalPages = (int)Math.Ceiling(
                    (double)roles.Length / GlobalConst.DiscordPaginationParams.ItemsPerPage
                ),
                Collection = roles,
                ActionButtonId = ButtonId.RoleChanger,
            },
            ttl: TimeSpan.FromSeconds(GlobalConst.DiscordPaginationParams.Ttl)
        );

        string[] messages = [
            "Select the role to display.",
            "",
            "All tracked roles will be removed from after selecting the role to display.",
            "Role ownership won't get affected by the removal, only the role assignment on Discord is affected.",
            "",
            ..DiscordMessageMakerForRoleChange.MakeRoleSelectCorrespondenceList(roles),
        ];

        return SendEphemeralMessageToBeDeletedAsync(
            messages.MergeLines(),
            DiscordMessageMakerForRoleChange.MakeRoleSelectButton(
                roles,
                ButtonId.RoleChanger
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

        DiscordPaginationContext<TrackedRoleModel>.SaveState(
            Context.User.Id.ToString(),
            new PaginationState<TrackedRoleModel> {
                CurrentPage = 1,
                TotalPages = (int)Math.Ceiling(
                    (double)roles.Length / GlobalConst.DiscordPaginationParams.ItemsPerPage
                ),
                Collection = roles,
                ActionButtonId = ButtonId.RoleAdder,
            },
            ttl: TimeSpan.FromSeconds(GlobalConst.DiscordPaginationParams.Ttl)
        );

        string[] messages = [
            "Select a role to obtain the ownership on Discord.",
            "This does not guarantee that the selected role will show. To ensure the selected role shows up, use `/role display` instead.",
            "",
            ..DiscordMessageMakerForRoleChange.MakeRoleSelectCorrespondenceList(roles),
        ];

        return SendEphemeralMessageToBeDeletedAsync(
            messages.MergeLines(),
            DiscordMessageMakerForRoleChange.MakeRoleSelectButton(
                roles,
                ButtonId.RoleAdder
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

        DiscordPaginationContext<TrackedRoleModel>.SaveState(
            Context.User.Id.ToString(),
            new PaginationState<TrackedRoleModel> {
                CurrentPage = 1,
                TotalPages = (int)Math.Ceiling(
                    (double)roles.Length / GlobalConst.DiscordPaginationParams.ItemsPerPage
                ),
                Collection = roles,
                ActionButtonId = ButtonId.RoleRemover,
            },
            ttl: TimeSpan.FromSeconds(GlobalConst.DiscordPaginationParams.Ttl)
        );

        string[] messages = [
            "Select a role to remove the ownership on Discord.",
            "This does not remove the actual ownership of the role. You can get them back using either `/role add` or `/role display` at any time.",
            "",
            ..DiscordMessageMakerForRoleChange.MakeRoleSelectCorrespondenceList(roles),
        ];

        return SendEphemeralMessageToBeDeletedAsync(
            messages.MergeLines(),
            DiscordMessageMakerForRoleChange.MakeRoleSelectButton(
                roles,
                ButtonId.RoleRemover
            )
        );
    }

    [SlashCommand("add-all", "Add all owned tracked roles.")]
    [UsedImplicitly]
    public async Task AddAllRoleAsync() {
        await Context.Interaction.RespondAsync(text: "Adding all owned tracked roles...", ephemeral: true);

        var user = Context.User.AsGuildUser();

        var previousRoleIds = DiscordTrackedRoleController
            .FindAllTrackedRoleIdsByRoleIds(user.Roles.Select(x => x.Id).ToArray())
            .Select(x => x.RoleId)
            .ToArray();

        await user.AddRolesAsync(DiscordRoleRecordController.FindRoleIdsByUserId(user.Id));

        var response = await Context.Interaction.GetOriginalResponseAsync();
        if (response is null) {
            throw new NullReferenceException("Original response not found");
        }

        await response.ModifyAsync(x => {
            x.Content = null;
            x.Embed = DiscordMessageMakerForRoleChange.MakeChangeRoleResult(
                user,
                previousRoleIds,
                DiscordRoleRecordController.FindRoleIdsByUserId(user.Id),
                Colors.Success
            );
        });
    }

    [SlashCommand("remove-all", "Remove all tracked roles.")]
    [UsedImplicitly]
    public async Task RemoveAllRoleAsync() {
        await Context.Interaction.RespondAsync(text: "Removing all owned tracked roles...", ephemeral: true);

        var user = Context.User.AsGuildUser();

        var previousRoleIds = DiscordTrackedRoleController
            .FindAllTrackedRoleIdsByRoleIds(user.Roles.Select(x => x.Id).ToArray())
            .Select(x => x.RoleId)
            .ToArray();

        await user.RemoveRolesAsync(DiscordRoleRecordController.FindRoleIdsByUserId(user.Id));

        var response = await Context.Interaction.GetOriginalResponseAsync();
        if (response is null) {
            throw new NullReferenceException("Original response not found");
        }

        await response.ModifyAsync(x => {
            x.Content = null;
            x.Embed = DiscordMessageMakerForRoleChange.MakeChangeRoleResult(
                user,
                previousRoleIds,
                DiscordRoleRecordController.FindRoleIdsByUserId(user.Id),
                Colors.Success
            );
        });
    }

    [SlashCommand("show", "Shows all the owned tracked roles.")]
    [UsedImplicitly]
    public Task ShowRoleAsync() {
        var user = Context.User.AsGuildUser();

        return Context.Interaction.RespondAsync(
            embed: DiscordMessageMakerForRoleChange.MakeShowRoleResult(
                user,
                DiscordRoleRecordController.FindRoleIdsByUserId(user.Id)
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
            embed: DiscordMessageMakerForRoleChange.MakeDeleteRoleResult(user, role.Id)
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

        var trackedRolesMentions = DiscordTrackedRoleController
            .FindAllTrackedRoles()
            .Select(x => x.RoleId)
            .ToArray()
            .MentionAllRoles();

        await Context.Interaction.RespondAsync(
            text: $"Currently tracked roles:\n{trackedRolesMentions}",
            embed: DiscordMessageMakerForRoleChange.MakeTrackRoleResult(
                role.Id,
                roleOwnedUsers.Length,
                "Role tracked.",
                Colors.Success
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

        var trackedRolesMentions = DiscordTrackedRoleController
            .FindAllTrackedRoles()
            .Select(x => x.RoleId)
            .ToArray()
            .MentionAllRoles();

        await Context.Interaction.RespondAsync(
            text: $"Currently tracked roles:\n{trackedRolesMentions}",
            embed: DiscordMessageMakerForRoleChange.MakeTrackRoleResult(
                role.Id,
                roleOwnedUsers.Length,
                "Role untracked.",
                Colors.Danger
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
            ..trackedRoles,
        ];

        await Context.Interaction.RespondAsync(messages.MergeLines());
    }

    [SlashCommand("reorder", "Reorder the target role to be at the position right below the base role.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [UsedImplicitly]
    public async Task ReorderRoleAsync(
        [Summary("target", "Role to change the order.")] IRole target,
        [Summary("above", "Target role will be placed right above this role.")] IRole above
    ) {
        // Position `0` is the very bottom in Discord list
        await Context.Guild.ReorderRolesAsync([new ReorderRoleProperties(target.Id, above.Position)]);

        await Context.Interaction.RespondAsync(
            $"{MentionUtils.MentionRole(target.Id)} is now moved to right above {MentionUtils.MentionRole(above.Id)}!",
            ephemeral: true
        );
    }
}
using Discord;
using Discord.Interactions;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Modules.SlashCommands;

[Group("role-restrict", "Commands for controlling role restrictions.")]
[CommandContextType(InteractionContextType.Guild)]
[RequireUserPermission(GuildPermission.Administrator)]
public class RoleRestrictionSlashModule : InteractionModuleBase<SocketInteractionContext> {

    [SlashCommand("add", "Add or update the role restriction conditions.")]
    [UsedImplicitly]
    public async Task AddRestrictedRoleAsync(
        [Summary("role", "Target role to setup restriction(s).")] IRole target,
        [Summary("min-account-age-days", "Required account age in days.")] uint minAccountAgeDays
    ) {
        await DiscordRestrictedRoleController.SaveRestrictedRole(target, minAccountAgeDays);

        await Context.Interaction.RespondAsync(
            embed: DiscordMessageMakerForRoleRestriction.MakeRestrictedRoleAddedMessage(target.Id, minAccountAgeDays)
        );
    }

    [SlashCommand("remove", "Remove the role restriction.")]
    [UsedImplicitly]
    public async Task RemoveRestrictedRoleAsync(
        [Summary("role", "Target role to remove the restriction.")] IRole target
    ) {
        await DiscordRestrictedRoleController.RemoveRestrictedRole(target.Id);

        await Context.Interaction.RespondAsync(
            embed: DiscordMessageMakerForRoleRestriction.MakeRestrictedRoleRemovedMessage(target.Id)
        );
    }

    [SlashCommand("bypass", "Add the user to the white list of the restricted role.")]
    [UsedImplicitly]
    public async Task AddWhiteListUserToRestrictedRoleAsync(
        [Summary("role", "The role to restrict.")] IRole target,
        [Summary("user", "The user to add to the white list.")] IUser user
    ) {
        await DiscordRestrictedRoleController.AddWhiteListUserToRestrictedRole(target.Id, user.Id);

        await Context.Interaction.RespondAsync(
            embed: DiscordMessageMakerForRoleRestriction.MakeRestrictedRoleAddedWhiteListMessage(target.Id, user)
        );
    }
}
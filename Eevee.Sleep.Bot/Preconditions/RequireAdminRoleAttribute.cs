using Discord;
using Discord.Interactions;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Preconditions;

public sealed class RequireAdminRoleAttribute : PreconditionAttribute {
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    ) {
        if (context.User is not IGuildUser guildUser) {
            return Task.FromResult(
                PreconditionResult.FromError("This command can only be used in a server.")
            );
        }

        var adminRoleId = ConfigHelper.GetDiscordAdminRoleId();
        return Task.FromResult(
            guildUser.RoleIds.Contains(adminRoleId)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("You do not have the required admin role.")
        );
    }
}

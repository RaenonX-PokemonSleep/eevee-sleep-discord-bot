using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Handlers.EventHandlers;

public static class GuildMemberUpdatedEventHandler {
    private static readonly ILogger Logger = LogHelper.CreateLogger(typeof(GuildMemberUpdatedEventHandler));

    private static async Task HandleRolesAdded(string[] roleIdsAdded, IUser user) {
        Logger.LogInformation(
            "Handing subscriber role addition of {RoleIds} for user {UserId} (@{UserName})",
            roleIdsAdded,
            user.Id,
            user.Username
        );

        var activationLink = await HttpRequestHelper.GenerateDiscordActivationLink(
            user.Id.ToString(),
            roleIdsAdded
        );
        if (string.IsNullOrEmpty(activationLink)) {
            Logger.LogWarning(
                "Activation link failed to generate for user {UserId} (@{UserName})",
                user.Id,
                user.Username
            );
            return;
        }

        await user.SendMessageAsync(
            activationLink,
            embeds: DiscordMessageMaker.MakeActivationNote()
        );

        Logger.LogInformation(
            "Activation link generated for user {UserId} (@{UserName}) - {Link}",
            user.Id,
            user.Username,
            string.IsNullOrEmpty(activationLink) ? "(empty - check for error)" : activationLink
        );
    }

    private static Task HandleRolesRemoved(IUser user) {
        Logger.LogInformation(
            "User {UserId} (@{Username}) activation expired (role dropped), removing associated activation",
            user.Id,
            user.Username
        );
        return ActivationController.RemoveDiscordActivationData(user.Id.ToString());
    }

    public static Task OnEvent(Cacheable<SocketGuildUser, ulong> original, SocketGuildUser updated) {
        if (!original.HasValue) {
            Logger.LogWarning("User data of {UserId} not cached", original.Id);
            return Task.CompletedTask;
        }

        var taggedRoleIds = ActivationPresetController.GetTaggedRoleIds();

        var rolesAdded = updated.Roles
            .ExceptBy(original.Value.Roles.Select(x => x.Id), role => role.Id)
            .Select(x => x.Id.ToString())
            .ToArray();
        var rolesRemoved = original.Value.Roles
            .ExceptBy(updated.Roles.Select(x => x.Id), role => role.Id)
            .Select(x => x.Id.ToString())
            .ToList();

        if (rolesAdded.Any(taggedRoleIds.Contains)) {
            return HandleRolesAdded(rolesAdded, updated);
        }

        if (rolesRemoved.Any(taggedRoleIds.Contains)) {
            return HandleRolesRemoved(updated);
        }

        return Task.CompletedTask;
    }
}
using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Handlers.EventHandlers;

public static class GuildMemberUpdatedEventHandler {
    private static readonly ILogger Logger = LogHelper.CreateLogger(typeof(GuildMemberUpdatedEventHandler));

    private static async Task HandleRolesAdded(string[] roleIdsAdded, IUser updated) {
        Logger.LogInformation(
            "Handing subscriber role addition of {RoleIds} for user {UserId} (@{UserName})",
            roleIdsAdded,
            updated.Id,
            updated.Username
        );

        var activationLink = await HttpRequestHelper.GenerateDiscordActivationLink(
            updated.Id.ToString(),
            roleIdsAdded
        );
        if (string.IsNullOrEmpty(activationLink)) {
            Logger.LogWarning(
                "Activation link failed to generate for user {UserId} (@{UserName})",
                updated.Id,
                updated.Username
            );
            return;
        }

        await updated.SendMessageAsync(
            activationLink,
            embeds: DiscordMessageMaker.MakeActivationNote()
        );

        Logger.LogInformation(
            "Activation link generated for user {UserId} (@{UserName}) - {Link}",
            updated.Id,
            updated.Username,
            string.IsNullOrEmpty(activationLink) ? "(empty - check for error)" : activationLink
        );
    }

    private static Task HandleRolesRemoved() {
        return Task.CompletedTask;
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
            return HandleRolesRemoved();
        }

        return Task.CompletedTask;
    }
}
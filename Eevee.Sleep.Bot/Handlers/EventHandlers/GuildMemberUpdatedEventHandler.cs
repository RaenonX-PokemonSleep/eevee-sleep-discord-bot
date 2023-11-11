using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Handlers.EventHandlers;

public static class GuildMemberUpdatedEventHandler {
    private static readonly ILogger Logger = LogHelper.CreateLogger(typeof(GuildMemberUpdatedEventHandler));

    private static async Task HandleRolesAdded(
        IDiscordClient client,
        IHostEnvironment environment,
        string[] roleIds,
        IUser user
    ) {
        Logger.LogInformation(
            "Handing subscriber role addition of {RoleIds} for user {UserId} (@{UserName})",
            roleIds,
            user.Id,
            user.Username
        );

        var activationLink = await HttpRequestHelper.GenerateDiscordActivationLink(
            user.Id.ToString(),
            roleIds
        );
        if (string.IsNullOrEmpty(activationLink)) {
            await client.SendMessageInAdminAlertChannel(
                $"Activation link failed to generate for user <@{user.Id}> (@{user.Username})",
                embed: DiscordMessageMaker.MakeUserSubscribed(user, roleIds, Colors.Warning)
            );
            Logger.LogWarning(
                "Activation link failed to generate for user {UserId} (@{UserName})",
                user.Id,
                user.Username
            );
            return;
        }

        Logger.LogInformation(
            "Activation link generated for user {UserId} (@{UserName}) - {Link}",
            user.Id,
            user.Username,
            string.IsNullOrEmpty(activationLink) ? "(empty - check for error)" : activationLink
        );
        // Prevent accidentally sending messages to the users
        if (environment.IsProduction()) {
            await user.SendMessageAsync(
                activationLink,
                embeds: DiscordMessageMaker.MakeActivationNote()
            );
        }
        await client.SendMessageInAdminAlertChannel(
            embed: DiscordMessageMaker.MakeUserSubscribed(user, roleIds)
        );
    }

    private static async Task HandleRolesRemoved(
        IDiscordClient client,
        string[] roleIds,
        IUser user
    ) {
        await client.SendMessageInAdminAlertChannel(
            embed: DiscordMessageMaker.MakeUserUnsubscribed(user, roleIds)
        );
        Logger.LogInformation(
            "User {UserId} (@{Username}) activation expired ({RoleCount} roles dropped: {RoleIds}), removing associated activation",
            user.Id,
            user.Username,
            roleIds.Length,
            string.Join(" / ", roleIds)
        );

        await ActivationController.RemoveDiscordActivationData(user.Id.ToString());
    }

    public static async Task OnEvent(
        IDiscordClient client,
        IHostEnvironment environment,
        Cacheable<SocketGuildUser, ulong> original,
        SocketGuildUser updated
    ) {
        if (!original.HasValue) {
            Logger.LogWarning("User data of {UserId} not cached", original.Id);
            await client.SendMessageInAdminAlertChannel(
                embed: DiscordMessageMaker.MakeUserDataNotCached(original.Id, updated)
            );
            return;
        }

        var taggedRoleIds = ActivationPresetController.GetTaggedRoleIds();

        var rolesAdded = updated.Roles
            .ExceptBy(original.Value.Roles.Select(x => x.Id), role => role.Id)
            .Select(x => x.Id.ToString())
            .ToArray();
        var rolesRemoved = original.Value.Roles
            .ExceptBy(updated.Roles.Select(x => x.Id), role => role.Id)
            .Select(x => x.Id.ToString())
            .ToArray();

        if (rolesAdded.Any(taggedRoleIds.Contains)) {
            await HandleRolesAdded(client, environment, rolesAdded, updated);
            return;
        }

        if (rolesRemoved.Any(taggedRoleIds.Contains)) {
            await HandleRolesRemoved(client, rolesRemoved, updated);
        }
    }
}
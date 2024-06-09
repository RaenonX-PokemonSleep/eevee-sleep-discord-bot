using Discord;
using Discord.Net;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Handlers.EventHandlers;

public static class GuildMemberUpdatedEventHandler {
    private static readonly ILogger Logger = LogHelper.CreateLogger(typeof(GuildMemberUpdatedEventHandler));

    private static async Task HandleTaggedRolesAdded(
        IDiscordClient client,
        IHostEnvironment environment,
        HashSet<ActivationPresetRole> rolesAdded,
        IUser user
    ) {
        Logger.LogInformation(
            "Handing subscriber role addition of {RoleIds} for user {UserId} (@{UserName})",
            string.Join(" / ", rolesAdded.Select(x => x.RoleId)),
            user.Id,
            user.Username
        );

        var activeRoles = rolesAdded
            .Where(x => !x.Suspended)
            .ToHashSet();

        if (activeRoles.Count <= 0) {
            await client.SendMessageInAdminAlertChannel(
                $"All roles to add to {MentionUtils.MentionUser(user.Id)} (@{user.Username}) are suspended",
                embed: await DiscordMessageMaker.MakeUserSubscribed(user, rolesAdded, Colors.Info)
            );
            Logger.LogInformation(
                "Skipped generating activation link due to role suspension for user {UserId} (@{UserName})",
                user.Id,
                user.Username
            );
            return;
        }

        var activationLink = await HttpRequestHelper.GenerateDiscordActivationLink(
            user.Id.ToString(),
            activeRoles.Select(x => x.RoleId)
        );
        if (string.IsNullOrEmpty(activationLink)) {
            await client.SendMessageInAdminAlertChannel(
                $"Activation link failed to generate for user {MentionUtils.MentionUser(user.Id)} (@{user.Username})",
                embed: await DiscordMessageMaker.MakeUserSubscribed(user, rolesAdded, Colors.Warning)
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
            try {
                await user.SendMessageAsync(
                    activationLink,
                    embeds: DiscordMessageMaker.MakeActivationNote()
                );
            } catch (HttpException e) {
                await client.SendMessageInAdminAlertChannel(
                    $"Error occurred during activation link delivery to {MentionUtils.MentionUser(user.Id)}\n" +
                    $"> {activationLink}",
                    embed: DiscordMessageMaker.MakeDiscordHttpException(e)
                );
            }
        }

        await client.SendMessageInAdminAlertChannel(
            embed: await DiscordMessageMaker.MakeUserSubscribed(user, rolesAdded)
        );
    }

    private static async Task HandleRolesAdded(ulong userId, ulong[] addedRoles) {
        await DiscordRoleRecordController.AddRoles(
            userId: userId,
            roles: DiscordTrackedRoleController
                .FindAllTrackedRoleIdsByRoleIds(addedRoles)
                .Select(x => x.RoleId)
                .ToArray()
        );
    }

    private static async Task HandleRolesRemoved(
        IDiscordClient client,
        IReadOnlyCollection<ulong> roleIds,
        IUser user
    ) {
        var subscriptionDuration = await ActivationController.RemoveDiscordActivationAndGetSubscriptionDuration(user.Id.ToString());
        
        await client.SendMessageInAdminAlertChannel(
            embed: await DiscordMessageMaker.MakeUserUnsubscribed(user, subscriptionDuration, roleIds)
        );
        Logger.LogInformation(
            "User {UserId} (@{Username}) activation expired ({RoleCount} roles dropped: {RoleIds}), removing associated activation",
            user.Id,
            user.Username,
            roleIds.Count,
            string.Join(" / ", roleIds)
        );
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

        var taggedRoles = ActivationPresetController
            .GetTaggedRoles();

        var rolesAdded = updated.Roles
            .ExceptBy(original.Value.Roles.Select(x => x.Id), role => role.Id)
            .Select(x => x.Id)
            .ToArray();
        var rolesRemoved = original.Value.Roles
            .ExceptBy(updated.Roles.Select(x => x.Id), role => role.Id)
            .Select(x => x.Id)
            .ToArray();

        var taggedRolesAdded = taggedRoles
            .Where(x => rolesAdded.Contains(x.RoleId))
            .ToHashSet();

        if (rolesAdded.Length > 0) {
            await HandleRolesAdded(original.Id, rolesAdded);
        }

        if (taggedRolesAdded.Count > 0) {
            await HandleTaggedRolesAdded(client, environment, taggedRolesAdded, updated);
            return;
        }

        if (rolesRemoved.Any(taggedRoles.Select(x => x.RoleId).Contains)) {
            await HandleRolesRemoved(client, rolesRemoved, updated);
        }
    }
}
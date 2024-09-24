using Discord;
using Discord.Net;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

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
            rolesAdded.Select(x => x.RoleId).MergeToSameLine(),
            user.Id,
            user.Username
        );

        var activeRoles = rolesAdded
            .Where(x => !x.Suspended)
            .ToHashSet();

        if (activeRoles.Count <= 0) {
            await client.SendMessageInAdminAlertChannel(
                $"All roles to add to {MentionUtils.MentionUser(user.Id)} (@{user.Username}) are suspended",
                await DiscordMessageMakerForActivation.MakeUserSubscribed(user, rolesAdded, Colors.Info)
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
                await DiscordMessageMakerForActivation.MakeUserSubscribed(user, rolesAdded, Colors.Warning)
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
                    embeds: DiscordMessageMakerForActivation.MakeActivationNote()
                );
            } catch (HttpException e) {
                await client.SendMessageInAdminAlertChannel(
                    $"Error occurred during activation link delivery to {MentionUtils.MentionUser(user.Id)}\n" +
                    $"> {activationLink}",
                    DiscordMessageMakerForError.MakeDiscordHttpException(e)
                );
            }
        }

        await client.SendMessageInAdminAlertChannel(
            embed: await DiscordMessageMakerForActivation.MakeUserSubscribed(user, rolesAdded)
        );
    }

    private static async Task RemoveRestrictedRoles(
        IDiscordClient client,
        ulong userId,
        ulong[] roleIds
    ) {
        var restrictedRoles = DiscordRestrictedRoleController
            .FindAllRestrictedRoleByRoleIds(roleIds);

        var guild = await client.GetCurrentWorkingGuild();
        var user = await client.GetGuildUserAsync(userId);

        foreach (var restrictedRole in restrictedRoles) {
            if (restrictedRole.WhitelistedUserIds.Contains(userId)) {
                continue;
            }

            if (!restrictedRole.MinAccountAgeDays.HasValue) {
                continue;
            }

            var accountCreationDate = user.CreatedAt.UtcDateTime;
            var accountAge = DateTime.UtcNow - accountCreationDate;
            if (accountAge.Days >= restrictedRole.MinAccountAgeDays) {
                continue;
            }

            await user.RemoveRoleAsync(restrictedRole.RoleId);

            var embed = DiscordMessageMakerForRoleRestriction.MakeRoleRestrictedNote(
                roleName: guild.GetRole(restrictedRole.RoleId)?.Name ?? "Unknown",
                user: user,
                minAccountAgeDays: restrictedRole.MinAccountAgeDays.Value,
                guildName: guild.Name
            );
            await user.SendMessageAsync(embed: embed);
            await client.SendMessageInRoleRestrictedChannel(embed: embed);
        }
    }

    private static async Task HandleRolesAdded(
        IDiscordClient client,
        ulong userId,
        ulong[] addedRoles
    ) {
        await DiscordRoleRecordController.AddRoles(
            userId,
            DiscordTrackedRoleController
                .FindAllTrackedRoleIdsByRoleIds(addedRoles)
                .Select(x => x.RoleId)
                .ToArray()
        );

        await RemoveRestrictedRoles(client, userId, addedRoles);
    }

    private static async Task HandleRolesRemoved(
        IDiscordClient client,
        IReadOnlyCollection<ulong> roleIds,
        IUser user
    ) {
        var subscriptionDuration = await ActivationController.RemoveDiscordActivationAndGetSubscriptionDuration(
            user.Id.ToString()
        );

        await client.SendMessageInAdminAlertChannel(
            embed: await DiscordMessageMakerForActivation.MakeUserUnsubscribed(user, subscriptionDuration, roleIds)
        );
        Logger.LogInformation(
            "User {UserId} (@{Username}) activation expired ({RoleCount} roles dropped: {RoleIds}), removing associated activation",
            user.Id,
            user.Username,
            roleIds.Count,
            roleIds.MergeToSameLine()
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
                embed: DiscordMessageMakerForActivation.MakeUserDataNotCached(original.Id, updated)
            );
            return;
        }

        var taggedRoles = ActivationPresetController
            .GetTaggedRolesAll();

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
            await HandleRolesAdded(client, original.Id, rolesAdded);
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
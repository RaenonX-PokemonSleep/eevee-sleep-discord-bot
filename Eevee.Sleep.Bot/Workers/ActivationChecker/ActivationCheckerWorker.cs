using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Models.ActivationChecker;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

namespace Eevee.Sleep.Bot.Workers.ActivationChecker;

public class ActivationCheckerWorker(
    DiscordSocketClient client,
    ILogger<ActivationCheckerWorker> logger
) : BackgroundService {
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    private static void TryCollectPresetLinkedRoleToAdd(
        ActivationCheckerExternalActivation externalActivation,
        Dictionary<string, ActivationPresetModel> presetDict,
        HashSet<ulong> trackedRolesOfUser,
        List<ActivationPresetRole> rolesToApply,
        ILogger<ActivationCheckerWorker> logger
    ) {
        var linkedPresetUuid = externalActivation.ActivationProperties.LinkedPresetUuid;
        if (linkedPresetUuid is null) {
            return;
        }

        presetDict.TryGetValue(linkedPresetUuid, out var preset);
        if (preset?.LinkedDiscordRoleIdString is null) {
            return;
        }

        var linkedDiscordRoleIdParsed = ulong.TryParse(
            preset.LinkedDiscordRoleIdString,
            out var linkedDiscordRoleId
        );
        if (!linkedDiscordRoleIdParsed) {
            logger.LogWarning(
                "Failed to parse Discord role ID to `ulong`! ({RoleIdString})",
                preset.LinkedDiscordRoleIdString
            );
            return;
        }

        if (!trackedRolesOfUser.Add(linkedDiscordRoleId)) {
            return;
        }

        rolesToApply.Add(
            new ActivationPresetRole {
                RoleId = linkedDiscordRoleId,
                Suspended = false,
            }
        );
    }

    private static void TryCollectPlatformRoleToAdd(
        ActivationCheckerExternalActivation externalActivation,
        HashSet<ulong> trackedRolesOfUser,
        List<ActivationPresetRole> rolesToApply,
        Dictionary<string, HashSet<ulong>> activeUsersByPlatform
    ) {
        var source = externalActivation.ActivationProperties.Source;
        if (!GlobalConst.TryGetPlatformSubscriberRole(source, out var platformRoleId, out var _)) {
            return;
        }

        if (source is null) {
            return;
        }

        if (!activeUsersByPlatform.TryGetValue(source, out var activeUsers)) {
            activeUsers = [];
            activeUsersByPlatform[source] = activeUsers;
        }

        activeUsers.Add(externalActivation.DiscordUser.Id);

        if (!trackedRolesOfUser.Add(platformRoleId)) {
            return;
        }

        rolesToApply.Add(
            new ActivationPresetRole {
                RoleId = platformRoleId,
                Suspended = false,
            }
        );
    }

    private async Task ApplyAddedRolesAndAlert(
        ActivationCheckerExternalActivation externalActivation,
        List<ActivationPresetRole> rolesToApply
    ) {
        if (rolesToApply.Count == 0) {
            return;
        }

        var roleIdsToApply = rolesToApply
            .Select(x => x.RoleId)
            .Distinct()
            .ToArray();

        logger.LogInformation(
            "Granting roles {RoleIds} to user {UserId} (@{Username}) from external activation checks",
            string.Join(", ", roleIdsToApply),
            externalActivation.DiscordUser.Id,
            externalActivation.DiscordUser.Username
        );

        await Task.WhenAll(
            externalActivation.DiscordUser.AddRolesAsync(roleIdsToApply),
            DiscordRoleRecordController.AddRoles(externalActivation.DiscordUser.Id, roleIdsToApply)
        );

        await client.SendMessageInAdminAlertChannel(
            embed: await DiscordMessageMakerForActivation.MakeUserSubscribed(
                externalActivation.DiscordUser,
                [..rolesToApply],
                Color.Teal,
                true
            )
        );
    }

    private async Task RemoveStalePlatformRoles(
        SocketGuild guild,
        Dictionary<string, HashSet<ulong>> activeUsersByPlatform
    ) {
        foreach (var source in GlobalConst.GetSupportedPlatformSubscriberSources()) {
            if (!GlobalConst.TryGetPlatformSubscriberRole(source, out var roleId, out var sourceName)) {
                continue;
            }

            activeUsersByPlatform.TryGetValue(source, out var activeUsers);
            activeUsers ??= [];

            var usersWithRole = guild.Users
                .Where(user => user.Roles.Any(role => role.Id == roleId))
                .ToList();

            foreach (var user in usersWithRole.Where(user => !activeUsers.Contains(user.Id))) {
                logger.LogInformation(
                    "Removing {Source} subscriber role {RoleId} from user {UserId} (@{Username}) - no active subscription",
                    sourceName,
                    roleId,
                    user.Id,
                    user.Username
                );

                await Task.WhenAll(
                    user.RemoveRoleAsync(roleId),
                    DiscordRoleRecordController.RemoveRoles(user.Id, [roleId])
                );

                await client.SendMessageInAdminAlertChannel(
                    embed: await DiscordMessageMakerForActivation.MakeUserUnsubscribed(
                        user,
                        TimeSpan.Zero,
                        [roleId],
                        true
                    )
                );
            }
        }
    }

    private List<ActivationCheckerExternalActivation> GetActivationCheckerExternalActivationCollection(
        ActivationPropertiesModel[] externalSubscribers
    ) {
        var guild = client.GetCurrentWorkingGuild();

        var externalActivations = new List<ActivationCheckerExternalActivation>();

        foreach (var externalSubscriber in externalSubscribers) {
            var discordUserIdParsed = ulong.TryParse(
                externalSubscriber.Contact.Discord,
                out var discordUserId
            );
            if (!discordUserIdParsed) {
                continue;
            }

            var subscriberOnDiscord = guild.GetUser(discordUserId);
            if (subscriberOnDiscord is null) {
                continue;
            }

            externalActivations.Add(
                new ActivationCheckerExternalActivation {
                    ActivationProperties = externalSubscriber,
                    DiscordUser = subscriberOnDiscord,
                }
            );
        }

        return externalActivations;
    }

    private async Task CheckExternalActivations() {
        var externalSubscribers = await ActivationController.GetExternalSubscribersWithDiscordContact();
        var externalActivations = GetActivationCheckerExternalActivationCollection(externalSubscribers);
        var guild = client.GetCurrentWorkingGuild();
        var activeUsersByPlatform = new Dictionary<string, HashSet<ulong>>();

        var targetPresetUuids = new List<string>();
        foreach (var externalSubscriber in externalSubscribers) {
            if (externalSubscriber.LinkedPresetUuid is not null) {
                targetPresetUuids.Add(externalSubscriber.LinkedPresetUuid);
            }
        }

        var presetDict = ActivationPresetController.GetPresetDictByUuid(targetPresetUuids);

        var roleRecordDict = await DiscordRoleRecordController.GetRoleRecordLookup(
            externalActivations.Select(x => x.DiscordUser.Id)
        );

        var trackedRolesByUser = externalActivations
            .GroupBy(x => x.DiscordUser.Id)
            .ToDictionary(
                group => group.Key,
                group =>
                    roleRecordDict.TryGetValue(group.Key, out var roleRecord) ?
                        roleRecord.Roles.ToHashSet() :
                        group.First().DiscordUser.Roles.Select(x => x.Id).ToHashSet()
            );

        foreach (var externalActivation in externalActivations) {
            var rolesToApply = new List<ActivationPresetRole>();

            var userId = externalActivation.DiscordUser.Id;
            if (!trackedRolesByUser.TryGetValue(userId, out var trackedRolesOfUser)) {
                trackedRolesOfUser = [];
                trackedRolesByUser[userId] = trackedRolesOfUser;
            }

            TryCollectPresetLinkedRoleToAdd(
                externalActivation,
                presetDict,
                trackedRolesOfUser,
                rolesToApply,
                logger
            );

            TryCollectPlatformRoleToAdd(
                externalActivation,
                trackedRolesOfUser,
                rolesToApply,
                activeUsersByPlatform
            );

            await ApplyAddedRolesAndAlert(
                externalActivation,
                rolesToApply
            );
        }

        await RemoveStalePlatformRoles(
            guild,
            activeUsersByPlatform
        );
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
        logger.LogInformation("Starting Activation Checker.");
        cancellationToken.Register(
            () => logger.LogInformation("Stopping Activation Checker: Cancellation token received.")
        );

        while (!cancellationToken.IsCancellationRequested) {
            if (client.ConnectionState != ConnectionState.Connected) {
                logger.LogWarning(
                    "Skipped checking activation as Discord client is not ready yet ({ConnectionState})",
                    client.ConnectionState
                );
            } else {
                logger.LogInformation("Checking Discord roles on subscribers");

                try {
                    await CheckExternalActivations();
                } catch {
                    await _cancellationTokenSource.CancelAsync();
                    throw;
                }
            }

            await Task.Delay(_checkInterval, cancellationToken);
        }
    }
}
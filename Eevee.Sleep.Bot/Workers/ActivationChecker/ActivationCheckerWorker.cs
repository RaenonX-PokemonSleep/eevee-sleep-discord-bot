using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Models.ActivationChecker;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

namespace Eevee.Sleep.Bot.Workers.ActivationChecker;

public class ActivationCheckerWorker(
    DiscordSocketClient client,
    ILogger<ActivationCheckerWorker> logger
) : BackgroundService {
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

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

        foreach (var externalActivation in externalActivations) {
            if (externalActivation.ActivationProperties.LinkedPresetUuid is null) {
                continue;
            }

            presetDict.TryGetValue(externalActivation.ActivationProperties.LinkedPresetUuid, out var preset);
            if (preset?.LinkedDiscordRoleIdString is null) {
                continue;
            }

            roleRecordDict.TryGetValue(externalActivation.DiscordUser.Id, out var roleRecordOfUser);
            if (roleRecordOfUser is null) {
                continue;
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
                continue;
            }

            var userHasRoleRecord = roleRecordOfUser.Roles.Any(roleId => roleId == linkedDiscordRoleId);
            if (userHasRoleRecord) {
                continue;
            }

            logger.LogInformation(
                "User {UserId} (@{Username}) subscribed externally, granting corresponding subscriber role",
                externalActivation.DiscordUser.Id,
                externalActivation.DiscordUser.Username
            );

            await Task.WhenAll(
                [
                    externalActivation.DiscordUser.AddRoleAsync(linkedDiscordRoleId),
                    DiscordRoleRecordController.AddRoles(
                        externalActivation.DiscordUser.Id,
                        [linkedDiscordRoleId]
                    ),
                ]
            );
            await client.SendMessageInAdminAlertChannel(
                embed: await DiscordMessageMakerForActivation.MakeUserSubscribed(
                    externalActivation.DiscordUser,
                    [new ActivationPresetRole { RoleId = linkedDiscordRoleId, Suspended = false }],
                    Color.Teal,
                    true
                )
            );
        }
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
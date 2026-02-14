using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Workers.ActivationChecker.Removal;

public abstract class ActivationRemovalWatcher<T>(
    DiscordSocketClient client,
    ILogger<ActivationRemovalWatcher<T>> logger
) : BackgroundService where T : ActivationKeyModel {
    protected abstract IMongoCollection<T> GetMongoCollection();

    protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
        logger.LogInformation("Starting Activation Removal Watcher.");
        cancellationToken.Register(
            () => logger.LogInformation("Stopping Activation Removal Watcher: Cancellation token received.")
        );

        var options = new ChangeStreamOptions {
            FullDocumentBeforeChange = ChangeStreamFullDocumentBeforeChangeOption.Required,
        };
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<T>>()
            .Match(x => x.OperationType == ChangeStreamOperationType.Delete);

        using var cursor = await GetMongoCollection().WatchAsync(pipeline, options, cancellationToken);

        await cursor.ForEachAsync(
            async change => {
                var activation = change.FullDocumentBeforeChange;
                if (activation is null) {
                    logger.LogWarning("Detected activation removal but the original data is unavailable!");
                    return;
                }

                var discordUserIdParsed = ulong.TryParse(activation.Contact.Discord, out var discordUserId);
                if (!discordUserIdParsed) {
                    return;
                }

                logger.LogInformation(
                    "Found activation removal of key {Key} associated to Discord user <@{DiscordUserId}>",
                    activation.Key,
                    discordUserId
                );

                var guild = client.GetCurrentWorkingGuild();
                var subscriberOnDiscord = guild.GetUser(discordUserId);

                if (subscriberOnDiscord is null) {
                    logger.LogWarning(
                        "User {DiscordUserId} not found in guild for role removal",
                        discordUserId
                    );
                    return;
                }

                var rolesToRemove = new HashSet<ulong>();

                // Check for source-based roles (Stripe/GitHub)
                if (GlobalConst.TryGetPlatformSubscriberRole(
                    activation.Source,
                    out var platformRoleId,
                    out var sourceName
                )) {
                    rolesToRemove.Add(platformRoleId);
                    logger.LogInformation(
                        "Removing platform subscriber role {RoleId} from <@{UserId}> for source {Source}",
                        platformRoleId,
                        discordUserId,
                        sourceName
                    );
                }

                // Check for preset-based roles
                if (activation.LinkedPresetUuid is not null) {
                    var preset = ActivationPresetController.GetPresetByUuid(activation.LinkedPresetUuid);
                    if (preset is null) {
                        logger.LogWarning(
                            "Found un-existed linked preset of UUID {PresetUuid} on the activation source from {ActivationSource}",
                            activation.LinkedPresetUuid,
                            activation.Source
                        );
                    } else if (preset.LinkedDiscordRoleIdString is not null) {
                        var linkedDiscordRoleId = ulong.Parse(preset.LinkedDiscordRoleIdString);
                        rolesToRemove.Add(linkedDiscordRoleId);
                        logger.LogInformation(
                            "Removing preset-based role {RoleId} from <@{UserId}>",
                            linkedDiscordRoleId,
                            discordUserId
                        );
                    } else {
                        logger.LogInformation(
                            "Preset {PresetUuid} doesn't have linked Discord role to remove",
                            preset.Uuid
                        );
                    }
                }

                if (rolesToRemove.Count == 0) {
                    logger.LogInformation("No roles to remove for user <@{UserId}>", discordUserId);
                    return;
                }

                var rolesToRemoveList = rolesToRemove.ToList();

                await Task.WhenAll(
                    subscriberOnDiscord.RemoveRolesAsync(rolesToRemoveList),
                    DiscordRoleRecordController.RemoveRoles(discordUserId, [..rolesToRemoveList])
                );

                logger.LogInformation(
                    "Removed roles {RoleIds} from <@{UserId}> including role record due to unsubscription",
                    string.Join(", ", rolesToRemoveList),
                    discordUserId
                );
                await client.SendMessageInAdminAlertChannel(
                    embed: await DiscordMessageMakerForActivation.MakeUserUnsubscribed(
                        subscriberOnDiscord,
                        DateTime.UtcNow - activation.GeneratedAt,
                        [..rolesToRemoveList],
                        true
                    )
                );
            },
            cancellationToken
        );
    }
}
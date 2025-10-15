using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
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

                if (activation.LinkedPresetUuid is null) {
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

                var preset = ActivationPresetController.GetPresetByUuid(activation.LinkedPresetUuid);
                if (preset is null) {
                    logger.LogWarning(
                        "Found un-existed linked preset of UUID {PresetUuid} on the activation source from {ActivationSource}",
                        activation.LinkedPresetUuid,
                        activation.Source
                    );
                    return;
                }

                if (preset.LinkedDiscordRoleIdString is null) {
                    logger.LogInformation(
                        "Preset {PresetUuid} doesn't have linked Discord role to remove",
                        preset.Uuid
                    );
                    return;
                }

                var guild = client.GetCurrentWorkingGuild();

                var subscriberOnDiscord = guild.GetUser(discordUserId);
                var linkedDiscordRoleId = ulong.Parse(preset.LinkedDiscordRoleIdString);

                await Task.WhenAll(
                    subscriberOnDiscord.RemoveRoleAsync(linkedDiscordRoleId),
                    DiscordRoleRecordController.RemoveRoles(discordUserId, [linkedDiscordRoleId])
                );

                logger.LogInformation(
                    "Removed Discord subscriber role of @{RoleId} from <{UserId}> including role record due to unsubscription",
                    linkedDiscordRoleId,
                    discordUserId
                );
                await client.SendMessageInAdminAlertChannel(
                    embed: await DiscordMessageMakerForActivation.MakeUserUnsubscribed(
                        subscriberOnDiscord,
                        DateTime.UtcNow - activation.GeneratedAt,
                        [linkedDiscordRoleId],
                        true
                    )
                );
            },
            cancellationToken
        );
    }
}
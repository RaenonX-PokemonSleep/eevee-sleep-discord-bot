using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Handlers.EventHandlers;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Workers;

public class DiscordMessageSelfDestructWorker(DiscordSocketClient client) : BackgroundService {
    private static readonly ILogger Logger = LogHelper.CreateLogger(typeof(DiscordMessageSelfDestructWorker));

    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                await Task.Delay(Interval, stoppingToken);
            } catch (TaskCanceledException) {
                break;
            }

            try {
                await ProcessExpiredMessages();
            } catch (Exception ex) {
                Logger.LogError(ex, "Error processing self-destruct messages");
            }
        }
    }

    private async Task ProcessExpiredMessages() {
        var expired = await SelfDestructController.FindExpiredMessages();

        if (expired.Length == 0) {
            return;
        }

        Logger.LogInformation("Processing {Count} expired self-destruct messages", expired.Length);

        foreach (var model in expired) {
            await ProcessSingleExpiredMessage(model);
        }
    }

    private async Task ProcessSingleExpiredMessage(SelfDestructMessageModel model) {
        try {
            await DeleteDiscordMessage(model.ChannelId, model.MessageId);
        } catch (Exception ex) {
            Logger.LogWarning(
                ex,
                "Failed to delete Discord message {MessageId} in channel {ChannelId}",
                model.MessageId, model.ChannelId
            );
        }

        ReactionRoleEventHandler.InvalidateCache(model.MessageId);
        await ReactionRoleController.DeleteByMessageId(model.MessageId);
        await SelfDestructController.DeleteByMessageId(model.MessageId);

        Logger.LogInformation("Cleaned up self-destruct message {MessageId}", model.MessageId);
    }

    private async Task DeleteDiscordMessage(ulong channelId, ulong messageId) {
        if (client.GetChannel(channelId) is not ISocketMessageChannel channel) {
            Logger.LogWarning("Channel {ChannelId} not found for self-destruct", channelId);
            return;
        }

        var message = await channel.GetMessageAsync(messageId);
        if (message is not null) {
            await message.DeleteAsync();
        }
    }
}

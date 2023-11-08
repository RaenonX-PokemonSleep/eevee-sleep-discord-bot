using Discord.Interactions;
using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Modules.SlashCommands;

[Group("bot", "Commands for bot management or bot utilities.")]
public class BotSlashModule : InteractionModuleBase<SocketInteractionContext> {
    [SlashCommand("ping", "Pings the bot and returns its latency.")]
    [UsedImplicitly]
    public Task PingAsync() => RespondAsync(text: $"Bot Latency: {Context.Client.Latency} ms", ephemeral: true);
}
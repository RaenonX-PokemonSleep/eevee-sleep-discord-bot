using Discord.WebSocket;

namespace Eevee.Sleep.Bot.Models.ActivationChecker;

public record ActivationCheckerExternalActivation {
    public required ActivationPropertiesModel ActivationProperties { get; init; }

    public required SocketGuildUser DiscordUser { get; init; }
}
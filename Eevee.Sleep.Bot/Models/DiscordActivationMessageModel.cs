using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Models;

public record DiscordActivationMessageModel {
    [UsedImplicitly]
    public required string UserId { get; init; }

    [UsedImplicitly]
    public required string Link { get; init; }
}
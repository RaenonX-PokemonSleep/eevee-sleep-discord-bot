using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Models;

public record ActivationContactModel {
    [UsedImplicitly]
    public required string? Discord { get; init; }
};
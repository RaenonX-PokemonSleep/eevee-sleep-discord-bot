namespace Eevee.Sleep.Bot.Models;

public record ActivationPresetRole {
    public required ulong RoleId { get; init; }

    public required bool Suspended { get; init; }
}
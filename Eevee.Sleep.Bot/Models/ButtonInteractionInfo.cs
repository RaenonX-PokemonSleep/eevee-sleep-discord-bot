using Eevee.Sleep.Bot.Enums;

namespace Eevee.Sleep.Bot.Models;

public record ButtonInteractionInfo {
    public required ButtonId ButtonId { get; init; }
    public required ulong CustomId { get; init; }
}
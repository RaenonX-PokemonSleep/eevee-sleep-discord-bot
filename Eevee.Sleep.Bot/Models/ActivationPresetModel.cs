using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models;

// To ignore `_id`
[BsonIgnoreExtraElements]
public record ActivationPresetModel {
    [UsedImplicitly]
    public required string Uuid { get; init; }

    [UsedImplicitly]
    public required string Source { get; init; }

    [UsedImplicitly]
    public required string Tag { get; init; }

    [UsedImplicitly]
    public required string Name { get; init; }

    [UsedImplicitly]
    public required Dictionary<string, bool> Activation { get; init; }

    [UsedImplicitly]
    public required bool Suspended { get; init; }

    [UsedImplicitly]
    public required string? LinkedDiscordRoleIdString { get; init; }
}
using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models; 

// To ignore `_id`
[BsonIgnoreExtraElements]
public record ActivationPropertiesModel {
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [UsedImplicitly]
    public required DateTime Expiry { get; init; }
    
    [UsedImplicitly]
    public required string? Source { get; init; }
    
    [UsedImplicitly]
    public required Dictionary<string, string> Contact { get; init; }

    [UsedImplicitly]
    public required bool IsSpecial { get; init; }

    [UsedImplicitly]
    public required bool? IsCmsMod { get; init; }

    [UsedImplicitly]
    public required string Note { get; init; }

    [UsedImplicitly]
    public required Dictionary<string, bool> Activation { get; init; }
}
using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models;

[BsonIgnoreExtraElements]
public record SelfDestructMessageModel {
    [UsedImplicitly]
    public required ulong MessageId { get; init; }

    [UsedImplicitly]
    public required ulong ChannelId { get; init; }

    [UsedImplicitly]
    public required long DestructAtEpochSec { get; init; }
}

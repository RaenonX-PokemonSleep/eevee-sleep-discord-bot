using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models;

// To ignore `_id`
[BsonIgnoreExtraElements]
public record ActivationDataModel : ActivationKeyModel {
    [UsedImplicitly]
    public ObjectId? UserId { get; init; }

    [UsedImplicitly]
    public ActivationRevocationModel? Revocation { get; init; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [UsedImplicitly]
    public DateTime? ConsumedAt { get; init; }
}
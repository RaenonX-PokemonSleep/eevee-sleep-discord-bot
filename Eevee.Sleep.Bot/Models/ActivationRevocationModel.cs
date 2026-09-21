using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models;

public record ActivationRevocationModel {
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public required DateTime At { get; init; }

    public required string Reason { get; init; }
}

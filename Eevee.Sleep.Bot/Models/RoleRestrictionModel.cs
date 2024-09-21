using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models;

[BsonIgnoreExtraElements]
public record RoleRestrictionModel {
    [UsedImplicitly]
    public required ulong RoleId { get; init; }

    [UsedImplicitly]
    public uint? MinAccountAgeDays { get; init; }

    [UsedImplicitly]
    public required List<ulong> WhitelistedUserIds { get; init; } = [];
}
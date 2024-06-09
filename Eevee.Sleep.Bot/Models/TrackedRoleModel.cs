using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models;

[BsonIgnoreExtraElements]
public record TrackedRoleModel {
    [UsedImplicitly]
    public required ulong RoleId { get; init; }
}
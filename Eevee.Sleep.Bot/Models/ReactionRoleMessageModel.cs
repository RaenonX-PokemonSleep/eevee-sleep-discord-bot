using Eevee.Sleep.Bot.Models.CustomSerializers;
using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models;

[BsonIgnoreExtraElements]
public record ReactionRoleMessageModel {
    [UsedImplicitly]
    public required ulong MessageId { get; init; }

    [UsedImplicitly]
    public required ulong ChannelId { get; init; }

    [UsedImplicitly]
    [BsonSerializer(typeof(EmoteToRoleMapSerializer))]
    public required Dictionary<string, ulong> EmoteToRoleMap { get; init; }

    [UsedImplicitly]
    public ulong[]? WhitelistedRoleIds { get; init; }
}

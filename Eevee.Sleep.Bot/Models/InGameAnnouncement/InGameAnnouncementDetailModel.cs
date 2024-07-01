using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models.InGameAnnouncement;

[BsonIgnoreExtraElements]
public record InGameAnnouncementDetailModel: InGameAnnouncementMetaModel {
    [UsedImplicitly]
    public required string Content { get; init; }

    [UsedImplicitly]
    public required string ContentHash { get; init; }

    [UsedImplicitly]
    public required string Updated { get; init; }

    [UsedImplicitly]
    public required DateTime RecordCreated { get; init; }
}
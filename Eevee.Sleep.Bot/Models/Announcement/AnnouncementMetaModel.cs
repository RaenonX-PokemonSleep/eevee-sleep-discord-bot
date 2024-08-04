using Eevee.Sleep.Bot.Enums;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models.Announcement;

[BsonIgnoreExtraElements]
public record AnnouncementMetaModel {
    [UsedImplicitly]
    public required string AnnouncementId { get; init; }

    [UsedImplicitly]
    public required string Title { get; init; }

    [UsedImplicitly]
    [BsonRepresentation(BsonType.String)]
    public required AnnouncementLanguage Language { get; init; }

    [UsedImplicitly]
    public required string Url { get; init; }

    [UsedImplicitly]
    public required DateTime RecordCreatedUtc { get; init; }

    [UsedImplicitly]
    public required DateTime RecordUpdatedUtc { get; init; }
}
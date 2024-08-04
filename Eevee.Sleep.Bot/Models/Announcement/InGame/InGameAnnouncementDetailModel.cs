using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models.Announcement.InGame;

[BsonIgnoreExtraElements]
public record InGameAnnouncementDetailModel : AnnouncementMetaModel {
    public required string Text { get; init; }

    [UsedImplicitly]
    public required DateTime OriginalUpdatedUtc { get; init; }

    [UsedImplicitly]
    public required DateTime OriginalCreatedUtc { get; init; }
}
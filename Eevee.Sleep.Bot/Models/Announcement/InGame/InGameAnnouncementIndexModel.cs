using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models.Announcement.InGame;

[BsonIgnoreExtraElements]
public record InGameAnnouncementIndexModel : AnnouncementMetaModel {
    public required string Hash { get; init; }
    
    [UsedImplicitly]
    public required DateTime OriginalUpdatedUtc { get; init; }

    [UsedImplicitly]
    public required DateTime OriginalCreatedUtc { get; init; }
}
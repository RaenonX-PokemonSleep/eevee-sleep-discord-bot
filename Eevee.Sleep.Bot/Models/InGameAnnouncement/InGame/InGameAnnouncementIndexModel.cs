using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models.InGameAnnouncement.InGame;

[BsonIgnoreExtraElements]
public record InGameAnnouncementIndexModel : InGameAnnouncementMetaModel {
    public required string Hash { get; init; }
}
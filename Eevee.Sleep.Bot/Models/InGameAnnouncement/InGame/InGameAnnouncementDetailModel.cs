using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models.InGameAnnouncement.InGame;

[BsonIgnoreExtraElements]
public record InGameAnnouncementDetailModel : InGameAnnouncementMetaModel {
    public required string Text { get; init; }

    [UsedImplicitly]
    public required DateTime RecordCreatedUtc { get; init; }
}
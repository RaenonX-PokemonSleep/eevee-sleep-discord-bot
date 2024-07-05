using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models.InGameAnnouncement;

[BsonIgnoreExtraElements]
public record InGameAnnouncementIndexModel: InGameAnnouncementMetaModel;
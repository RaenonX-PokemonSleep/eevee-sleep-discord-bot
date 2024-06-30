using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models;

[BsonIgnoreExtraElements]
public record InGameAnnouncementIndexModel: InGameAnnouncementMetaModel {
}
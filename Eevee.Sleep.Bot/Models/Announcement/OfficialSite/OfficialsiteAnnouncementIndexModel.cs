using Eevee.Sleep.Bot.Models.Announcement.ApiResponses;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models.Announcement.Officialsite;

[BsonIgnoreExtraElements]
public record OfficialsiteAnnouncementIndexModel: AnnouncementMetaModel {
    public AnnouncementIndexResponse ToApiResponse() => new() {
        Title = Title,
        OfficialLink = Url,
        LastUpdated = new() {
            Official = RecordUpdatedUtc, // Since there is no updatedDate, use the server-side update date and time instead
            Server = RecordUpdatedUtc
        }
    };
}
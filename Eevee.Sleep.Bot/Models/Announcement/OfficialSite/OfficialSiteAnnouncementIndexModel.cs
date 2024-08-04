using Eevee.Sleep.Bot.Models.Announcement.ApiResponses;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models.Announcement.OfficialSite;

[BsonIgnoreExtraElements]
public record OfficialSiteAnnouncementIndexModel : AnnouncementMetaModel {
    public AnnouncementIndexResponse ToApiResponse() {
        return new AnnouncementIndexResponse {
            Title = Title,
            OfficialLink = Url,
            LastUpdated = new AnnouncementLastUpdatedResponse {
                // Since there is no updatedDate, use the server-side update date and time instead
                Official = RecordUpdatedUtc,
                Server = RecordUpdatedUtc,
            },
        };
    }
}
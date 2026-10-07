using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models.Announcement.OfficialSite;

public record OfficialSiteAnnouncementCrawlStateModel {
    [BsonId]
    public required string Url { get; init; }

    public required DateTime LastModifiedUtc { get; init; }
}

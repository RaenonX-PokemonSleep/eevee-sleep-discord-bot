using Eevee.Sleep.Bot.Models.Announcement.ApiResponses;
using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models.Announcement.Officialsite;

[BsonIgnoreExtraElements]
public record OfficialsiteAnnouncementDetailModel : AnnouncementMetaModel {
    [UsedImplicitly]
    public required string Content { get; init; }

    [UsedImplicitly]
    public required string ContentHash { get; init; }

    [UsedImplicitly]
    public required DateOnly OriginalUpdated { get; init; }

    public AnnouncementDetailResponse ToApiResponse() => new() {
        Title = Title,
        OfficialLink = Url,
        LastUpdated = new() {
            Official = OriginalUpdated.ToDateTime(new TimeOnly(0), DateTimeKind.Utc),
            Server = RecordUpdatedUtc
        },
        Content = Content
    };
}
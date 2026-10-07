using Eevee.Sleep.Bot.Extensions;

namespace Eevee.Sleep.Bot.Models.Announcement.OfficialSite;

public record OfficialSiteNewsArticle {
    public required string Title { get; init; }

    public required string Content { get; init; }

    public required DateOnly Date { get; init; }

    public (OfficialSiteAnnouncementIndexModel Index, OfficialSiteAnnouncementDetailModel Detail) ToModels(
        OfficialSiteNewsEntry entry
    ) {
        var now = DateTime.UtcNow;

        return (
            new OfficialSiteAnnouncementIndexModel {
                AnnouncementId = entry.Slug,
                Title = Title,
                Language = entry.Language,
                Url = entry.Url,
                Hash = $"{Title}{entry.Url}".ToSha256Hash(),
                RecordCreatedUtc = now,
                RecordUpdatedUtc = now,
            },
            new OfficialSiteAnnouncementDetailModel {
                AnnouncementId = entry.Slug,
                Title = Title,
                Language = entry.Language,
                Url = entry.Url,
                Content = Content,
                ContentHash = Content.ToSha256Hash(),
                OriginalUpdated = Date,
                RecordCreatedUtc = now,
                RecordUpdatedUtc = now,
            }
        );
    }
}

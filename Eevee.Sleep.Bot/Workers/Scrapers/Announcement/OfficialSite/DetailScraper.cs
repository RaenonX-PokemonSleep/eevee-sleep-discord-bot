using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;

namespace Eevee.Sleep.Bot.Workers.Scrapers.Announcement.OfficialSite;

public static class DetailScraper {
    public static async Task<OfficialSiteAnnouncementDetailModel> GetAsync(OfficialSiteAnnouncementIndexModel index) {
        var document = await DocumentLoader.FetchDocumentAsync(index.Url);
        var date = document.QuerySelector("p.header_4__date > time")?.TextContent.Trim();
        var content = document.QuerySelector("div.article_2__content")?.InnerHtml.Trim();

        if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(content)) {
            throw new ContentStructureChangedException(
                message: "Date or content is empty. Failed to get detail.",
                context: new Dictionary<string, string?> {
                    { "title", index.Title },
                    { "url", index.Url },
                    { "language", index.Language.ToString() },
                    { "date", date },
                    { "content", content },
                    { "id", index.AnnouncementId },
                    { "statusCode", document.StatusCode.ToString()}
                }
            );
        }

        await Task.Delay(500);
        return new OfficialSiteAnnouncementDetailModel() {
            AnnouncementId = index.AnnouncementId,
            Title = index.Title,
            Language = index.Language,
            Url = index.Url,
            Content = content,
            ContentHash = content.ToSha256Hash(),
            OriginalUpdated = DateOnly.Parse(date),
            RecordCreatedUtc = DateTime.UtcNow,
            RecordUpdatedUtc = DateTime.UtcNow
        };
    }
}
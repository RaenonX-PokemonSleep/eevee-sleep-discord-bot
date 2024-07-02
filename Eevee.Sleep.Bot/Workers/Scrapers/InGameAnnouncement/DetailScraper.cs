using AngleSharp;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.InGameAnnouncement;

namespace Eevee.Sleep.Bot.Workers.Scrapers.InGameAnnouncement;

public static class DetailScraper {
    public static async Task<InGameAnnouncementDetailModel> GetAsync(InGameAnnouncementIndexModel index) {
        var config = Configuration.Default.WithDefaultLoader();

        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(index.Url);

        var date = document.QuerySelector("p.header_4__date > time")?.TextContent;
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
        return new InGameAnnouncementDetailModel() {
            AnnouncementId = index.AnnouncementId,
            Title = index.Title,
            Language = index.Language,
            Url = index.Url,
            Content = content,
            ContentHash = content.ToSha256Hash(),
            Updated = date,
            RecordCreatedUtc = DateTime.UtcNow
        };
    }
}
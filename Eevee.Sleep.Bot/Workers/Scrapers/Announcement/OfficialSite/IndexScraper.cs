using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;
using MongoDB.Driver.Linq;

namespace Eevee.Sleep.Bot.Workers.Scrapers.Announcement.OfficialSite;

public static class IndexScraper {
    public static async Task<List<OfficialSiteAnnouncementIndexModel>> GetAsync(string url, AnnouncementLanguage language) {
        var document = await DocumentLoader.FetchDocumentAsync(url);
        var contents = document.QuerySelectorAll("ul.items > li > a.banner_2");

        List<OfficialSiteAnnouncementIndexModel> IndexModels = [];
        foreach (var content in contents) {
            if (content is null) {
                throw new ContentStructureChangedException(
                    message: "Fetch news list failed.",
                    context: new Dictionary<string, string?> {
                        { "url", url },
                        { "language", language.ToString() },
                    }
                );
            };
            
            var title = content.QuerySelector("p.banner_2__title")?.TextContent?.Trim();
            // url format: https://www.pokemonsleep.net/news/313438343932333036303239323135373435/
            var id = content.GetAttribute("href")?.Split("/").SkipLast(1).Last();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(id)) {
                throw new ContentStructureChangedException(
                    message: "Title or id is empty. Failed to get index.",
                    context: new Dictionary<string, string?> {
                        { "title", title },
                        { "url", url },
                        { "language", language.ToString() },
                        { "id", id },
                        { "statusCode", document.StatusCode.ToString()}
                    }
                );
            };

            IndexModels.Add(new OfficialSiteAnnouncementIndexModel {
                Title = title,
                Language = language,
                AnnouncementId = id,
                Url = $"https://www.pokemonsleep.net/news/{id}/",
                RecordCreatedUtc = DateTime.UtcNow,
                RecordUpdatedUtc = DateTime.UtcNow
            });
        }

        await Task.Delay(500);
        return IndexModels;
    }

    public static async Task<IEnumerable<OfficialSiteAnnouncementIndexModel>> GetAllPagesAsync(string baseUrl, AnnouncementLanguage language) {
        var document = await DocumentLoader.FetchDocumentAsync(baseUrl);

        // format: "1/23" to "23"
        var pageCount = (document.QuerySelector("div.pagination_1 > div > p")?.TextContent?.Trim()?.Split("/").Last())
            ?? throw new ContentStructureChangedException(
                message: "Page count is empty. Failed to get index.",
                context: new Dictionary<string, string?> {
                    { "url", baseUrl },
                    { "language", language.ToString() },
                }
            );

        var tasks = Enumerable
            .Range(1, pageCount.ToInt(defaultValue: 1))
            .AsParallel()
            .WithDegreeOfParallelism(5)
            .Select(n => GetAsync($"{baseUrl}/page/{n}", language));

        return (await Task.WhenAll(tasks)).SelectMany(page => page);
    }
}
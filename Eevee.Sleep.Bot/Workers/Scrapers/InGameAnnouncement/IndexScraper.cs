using AngleSharp;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Models;
using MongoDB.Driver.Linq;

namespace Eevee.Sleep.Bot.Workers.Scrapers.InGameAnnouncement;

public static class IndexScraper {
    public static async Task<List<InGameAnnouncementIndexModel>> GetAsync(string url, InGameAnnoucementLanguage language) {
        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(url);

        var contents = document.QuerySelectorAll("ul.items > li > a.banner_2");

        List<InGameAnnouncementIndexModel> IndexModels = [];
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
                        { "id", id }
                    }
                );
            };

            IndexModels.Add(new InGameAnnouncementIndexModel {
                Title = title,
                Language = language,
                AnnouncementId = id,
                Url = $"https://www.pokemonsleep.net/news/{id}/",
            });
        }

        return IndexModels;
    }
}
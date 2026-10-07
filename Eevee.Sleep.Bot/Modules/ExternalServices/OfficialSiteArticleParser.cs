using System.Globalization;
using AngleSharp.Html.Parser;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;

namespace Eevee.Sleep.Bot.Modules.ExternalServices;

public static class OfficialSiteArticleParser {
    public static OfficialSiteNewsArticle Parse(string html, OfficialSiteNewsEntry entry) {
        using var document = new HtmlParser().ParseDocument(html);
        var title = document.QuerySelector("#post .header_4__h1")?.TextContent.Trim();
        var content = document.QuerySelector("#post .article_2__content")?.InnerHtml.Trim();
        var time = document.QuerySelector("#post .header_4__date time");
        var (locale, dateFormat) = entry.Language switch {
            AnnouncementLanguage.JP => ("ja", "yyyy/M/d"),
            AnnouncementLanguage.EN => ("en_US", "M/d/yyyy"),
            AnnouncementLanguage.ZH => ("zh_TW", "yyyy/M/d"),
            _ => throw new ArgumentOutOfRangeException(nameof(entry)),
        };

        // Read the server's publication date, before JavaScript localizes it to the browser's timezone.
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content) ||
            time?.GetAttribute("data-locale") != locale ||
            !DateOnly.TryParseExact(
                time.TextContent.Trim(), dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date
            )) {
            throw new ContentStructureChangedException(
                "Official website article is missing its title, content, or publication date.",
                new Dictionary<string, string?> {
                    { "url", entry.Url },
                    { "language", entry.Language.ToString() },
                }
            );
        }

        return new OfficialSiteNewsArticle { Title = title, Content = content, Date = date };
    }
}

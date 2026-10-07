using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;

namespace Eevee.Sleep.Bot.Modules.ExternalServices;

public static class OfficialSiteSitemapParser {
    private static readonly XNamespace Namespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    public static IReadOnlyList<string> ParseIndex(string xml, string sourceUrl) {
        var root = ParseRoot(xml, "sitemapindex", sourceUrl);
        var urls = root.Elements(Namespace + "sitemap")
            .Select(element => ParseUrl(element, sourceUrl))
            .Where(uri => uri.AbsolutePath.StartsWith("/wp-sitemap-posts-news-", StringComparison.Ordinal) &&
                          uri.AbsolutePath.EndsWith(".xml", StringComparison.Ordinal))
            .Select(uri => uri.AbsoluteUri)
            .Distinct()
            .ToArray();

        return urls.Length > 0 ? urls : throw InvalidSitemap(sourceUrl, "No news sitemaps found.");
    }

    public static IReadOnlyList<OfficialSiteNewsEntry> ParseEntries(string xml, string sourceUrl) {
        var root = ParseRoot(xml, "urlset", sourceUrl);
        var entries = new List<OfficialSiteNewsEntry>();

        foreach (var element in root.Elements(Namespace + "url")) {
            var uri = ParseUrl(element, sourceUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            AnnouncementLanguage? language = segments switch {
                ["news", _] => AnnouncementLanguage.JP,
                ["en", "news", _] => AnnouncementLanguage.EN,
                ["zh", "news", _] => AnnouncementLanguage.ZH,
                _ => null,
            };

            if (language is null) {
                continue;
            }

            if (!DateTimeOffset.TryParse(
                    element.Element(Namespace + "lastmod")?.Value,
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var modified
                )) {
                throw InvalidSitemap(sourceUrl, $"Missing or invalid lastmod for {uri}.");
            }

            entries.Add(new OfficialSiteNewsEntry {
                Url = uri.AbsoluteUri,
                Language = language.Value,
                LastModifiedUtc = modified.UtcDateTime,
            });
        }

        return entries;
    }

    private static XElement ParseRoot(string xml, string rootName, string sourceUrl) {
        try {
            using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            });
            var root = XDocument.Load(reader).Root;
            return root?.Name == Namespace + rootName
                ? root
                : throw InvalidSitemap(sourceUrl, $"Expected {rootName}.");
        } catch (XmlException e) {
            throw InvalidSitemap(sourceUrl, e.Message);
        }
    }

    private static Uri ParseUrl(XElement element, string sourceUrl) {
        if (!Uri.TryCreate(element.Element(Namespace + "loc")?.Value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || uri.Host != "www.pokemonsleep.net" ||
            !uri.IsDefaultPort || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0) {
            throw InvalidSitemap(sourceUrl, "Missing or unexpected URL.");
        }

        return uri;
    }

    private static ContentStructureChangedException InvalidSitemap(string url, string reason) {
        return new ContentStructureChangedException(
            "Official website news sitemap has an unexpected structure.",
            new Dictionary<string, string?> { { "url", url }, { "reason", reason } }
        );
    }
}

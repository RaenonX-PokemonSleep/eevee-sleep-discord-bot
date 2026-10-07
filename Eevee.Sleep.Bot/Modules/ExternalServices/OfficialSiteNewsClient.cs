using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;

namespace Eevee.Sleep.Bot.Modules.ExternalServices;

public class OfficialSiteNewsClient(HttpClient client) {
    private const string SitemapIndexUrl = "https://www.pokemonsleep.net/wp-sitemap.xml";

    private static readonly TimeSpan RequestInterval = TimeSpan.FromSeconds(2);
    private static readonly SemaphoreSlim RequestSemaphore = new(1, 1);

    private static DateTimeOffset _nextRequestUtc = DateTimeOffset.MinValue;

    public async Task<IReadOnlyList<OfficialSiteNewsEntry>> FetchEntriesAsync(
        CancellationToken cancellationToken = default
    ) {
        var index = await FetchDocumentAsync(SitemapIndexUrl, cancellationToken);
        var sitemapUrls = OfficialSiteSitemapParser.ParseIndex(index, SitemapIndexUrl);
        var entries = new List<OfficialSiteNewsEntry>();

        foreach (var url in sitemapUrls) {
            var xml = await FetchDocumentAsync(url, cancellationToken);
            entries.AddRange(OfficialSiteSitemapParser.ParseEntries(xml, url));
        }

        if (entries.Count == 0) {
            throw new ContentStructureChangedException(
                "Official website news sitemaps contained no supported articles.",
                new Dictionary<string, string?> { { "url", SitemapIndexUrl } }
            );
        }

        return entries.GroupBy(entry => entry.Url)
            .Select(group => group.MaxBy(entry => entry.LastModifiedUtc)!)
            .OrderBy(entry => entry.LastModifiedUtc)
            .ThenBy(entry => entry.Url, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<OfficialSiteNewsArticle> FetchArticleAsync(
        OfficialSiteNewsEntry entry,
        CancellationToken cancellationToken = default
    ) {
        var html = await FetchDocumentAsync(entry.Url, cancellationToken);
        return OfficialSiteArticleParser.Parse(html, entry);
    }

    private async Task<string> FetchDocumentAsync(string url, CancellationToken cancellationToken) {
        // Share pacing across client instances, languages, polls, and retries. Never burst concurrent requests.
        await RequestSemaphore.WaitAsync(cancellationToken);

        try {
            var delay = _nextRequestUtc - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero) {
                await Task.Delay(delay, cancellationToken);
            }

            using var response = await client.GetAsync(url, cancellationToken);
            var retryAfter = response.Headers.RetryAfter;
            _nextRequestUtc = retryAfter?.Date ?? DateTimeOffset.UtcNow + (retryAfter?.Delta ?? TimeSpan.Zero);

            if (!response.IsSuccessStatusCode) {
                throw new FetchDocumentFailedException(
                    "Failed to fetch official website announcements.",
                    new Dictionary<string, string?> {
                        { "url", url },
                        { "status", response.StatusCode.ToString() },
                        { "cloudFrontId", GetHeader(response, "X-Amz-Cf-Id") },
                        { "retryAfter", GetHeader(response, "Retry-After") },
                    }
                );
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception e) when (e is HttpRequestException or OperationCanceledException) {
            throw new FetchDocumentFailedException(
                "Failed to fetch official website announcements.",
                new Dictionary<string, string?> { { "url", url }, { "exception", e.Message } }
            );
        } finally {
            var nextRequestUtc = DateTimeOffset.UtcNow + RequestInterval;
            if (_nextRequestUtc < nextRequestUtc) {
                _nextRequestUtc = nextRequestUtc;
            }

            RequestSemaphore.Release();
        }
    }

    private static string? GetHeader(HttpResponseMessage response, string name) {
        return response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    }
}

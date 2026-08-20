using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;

namespace Eevee.Sleep.Bot.Modules.ExternalServices;

public class OfficialSiteNewsClient(HttpClient client) {
    private const int PageSize = 100;

    private static readonly IReadOnlyDictionary<AnnouncementLanguage, string> EndpointByLanguage =
        new Dictionary<AnnouncementLanguage, string> {
            { AnnouncementLanguage.JP, "https://www.pokemonsleep.net/wp-json/wp/v2/news" },
            { AnnouncementLanguage.EN, "https://www.pokemonsleep.net/en/wp-json/wp/v2/news" },
            { AnnouncementLanguage.ZH, "https://www.pokemonsleep.net/zh/wp-json/wp/v2/news" },
        };

    public async Task<IReadOnlyList<OfficialSiteNewsResponse>> FetchAllAsync(
        AnnouncementLanguage language,
        DateTime? modifiedAfterUtc = null,
        CancellationToken cancellationToken = default
    ) {
        var items = new List<OfficialSiteNewsResponse>();
        var page = 1;
        string? currentUrl = null;

        try {
            int totalPages;
            do {
                currentUrl = BuildUrl(language, page, modifiedAfterUtc);
                using var response = await client.GetAsync(currentUrl, cancellationToken);

                if (!response.IsSuccessStatusCode) {
                    throw new FetchDocumentFailedException(
                        "Failed to fetch official website announcements.",
                        new Dictionary<string, string?> {
                            { "url", currentUrl },
                            { "language", language.ToString() },
                            { "status", response.StatusCode.ToString() },
                            { "cloudFrontId", GetHeader(response, "X-Amz-Cf-Id") },
                            { "retryAfter", GetHeader(response, "Retry-After") },
                        }
                    );
                }

                totalPages = ParseTotalPages(response, language);
                items.AddRange(
                    await response.Content.ReadFromJsonAsync<OfficialSiteNewsResponse[]>(
                        cancellationToken: cancellationToken
                    ) ?? []
                );
                page++;
            } while (page <= totalPages);
        } catch (DocumentProcessingException) {
            throw;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception e) when (e is HttpRequestException or JsonException or OperationCanceledException) {
            throw new FetchDocumentFailedException(
                "Failed to fetch official website announcements.",
                new Dictionary<string, string?> {
                    { "url", currentUrl },
                    { "language", language.ToString() },
                    { "exception", e.Message },
                }
            );
        }

        return items
            .GroupBy(x => x.Slug)
            .Select(
                duplicates => duplicates
                    .OrderByDescending(x => x.GetModifiedUtc())
                    .ThenByDescending(x => x.Id)
                    .First()
            )
            .OrderBy(x => x.GetModifiedUtc())
            .ToList();
    }

    private static string BuildUrl(AnnouncementLanguage language, int page, DateTime? modifiedAfterUtc) {
        var modifiedAfter = modifiedAfterUtc is null
            ? string.Empty
            : $"&modified_after={Uri.EscapeDataString(modifiedAfterUtc.Value.ToString("O", CultureInfo.InvariantCulture))}";

        return $"{EndpointByLanguage[language]}?per_page={PageSize}&page={page}" +
               "&orderby=modified&order=asc" +
               "&_fields=id,slug,link,date,modified_gmt,title,content" +
               modifiedAfter;
    }

    private static int ParseTotalPages(HttpResponseMessage response, AnnouncementLanguage language) {
        var value = GetHeader(response, "X-WP-TotalPages");
        if (int.TryParse(value, out var totalPages)) {
            return totalPages;
        }

        throw new FetchDocumentFailedException(
            "Official website announcement response omitted its page count.",
            new Dictionary<string, string?> {
                { "language", language.ToString() },
                { "totalPages", value },
            }
        );
    }

    private static string? GetHeader(HttpResponseMessage response, string name) {
        return response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    }
}

using System.Text.Json;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Modules;

namespace Eevee.Sleep.Bot.Workers.Scrapers;

internal static class JsonDocumentFetcher<T> {
    public static async Task<T> FetchAsync(string url) {
        try {
            var response = await HttpModule.Client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var model = JsonSerializer.Deserialize<T>(content) ?? throw new FetchDocumentFailedException(
                "Failed to fetch document.",
                new Dictionary<string, string?> {
                    { "url", url },
                    { "status", response.StatusCode.ToString() },
                }
            );

            return model;
        } catch (Exception e) {
            throw new FetchDocumentFailedException(
                "Failed to fetch document.",
                new Dictionary<string, string?> {
                    { "url", url },
                    { "exception", e.Message },
                }
            );
        }
    }
}
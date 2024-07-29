using System.Text.Json;
using Eevee.Sleep.Bot.Exceptions;

namespace Eevee.Sleep.Bot.Workers.Scrapers;

static class JsonDocumentFetcher<T> {
    private static readonly HttpClient client = new() {
        Timeout = TimeSpan.FromSeconds(120)
    };
    
    public static async Task<T> FetchAsync(string url) {
        try {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var model = JsonSerializer.Deserialize<T>(content) ?? throw new FetchDocumentFailedException(
                    message: "Failed to fetch document.",
                    context: new Dictionary<string, string?> {
                        { "url", url },
                        { "status", response.StatusCode.ToString()}
                    });

            return model;
        } catch (Exception e) {
            throw new FetchDocumentFailedException(
                message: "Failed to fetch document.",
                context: new Dictionary<string, string?> {
                    { "url", url },
                    { "exception", e.Message }
                }
            );
        }
    }
}
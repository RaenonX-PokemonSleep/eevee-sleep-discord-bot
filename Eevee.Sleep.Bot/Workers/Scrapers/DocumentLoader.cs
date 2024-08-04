using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Io;
using Eevee.Sleep.Bot.Exceptions;

namespace Eevee.Sleep.Bot.Workers.Scrapers;

public static class DocumentLoader {
    private const int TimeoutSeconds = 120;

    public static async Task<IDocument> FetchDocumentAsync(string url) {
        var requester = new DefaultHttpRequester {
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds),
        };
        var config = Configuration.Default.With(requester).WithDefaultLoader();
        var context = BrowsingContext.New(config);

        try {
            // Connections establishment errors, etc., are to be detected here, so await.
            // If not await, exception will occur on the caller side.
            return await context.OpenAsync(url);
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
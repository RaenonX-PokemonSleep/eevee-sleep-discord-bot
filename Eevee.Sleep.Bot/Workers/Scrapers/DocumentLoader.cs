using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Io;
using Eevee.Sleep.Bot.Exceptions;

namespace Eevee.Sleep.Bot.Workers.Scrapers;

public static class DocumentLoader {
    private const int TIMEOUT_SECONDS = 120;

    public static Task<IDocument> FetchDocumentAsync(string url) {
        var requester = new DefaultHttpRequester {
            Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS)
        };
        var config = Configuration.Default.With(requester).WithDefaultLoader();
        var context = BrowsingContext.New(config);
        
        try {
            return context.OpenAsync(url);
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
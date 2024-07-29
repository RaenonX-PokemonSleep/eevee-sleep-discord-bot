namespace Eevee.Sleep.Bot.Workers.Crawlers;

public interface IAnnoucementCrawler {
    public Task ExecuteAsync(int retryCount = 0);
}
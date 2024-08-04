namespace Eevee.Sleep.Bot.Workers.Crawlers;

public interface IAnnouncementCrawler {
    protected const int MaxRetryCount = 3;

    public Task ExecuteAsync(int retryCount = 0);
}
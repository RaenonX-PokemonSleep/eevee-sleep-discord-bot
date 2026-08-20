namespace Eevee.Sleep.Bot.Workers.Crawlers;

public interface IAnnouncementCrawler {
    protected const int MaxRetryCount = 3;

    public Task InitialCrawlCompleted { get; }

    public Task ExecuteAsync(CancellationToken cancellationToken, int retryCount = 0);
}

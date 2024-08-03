using Discord.WebSocket;
using Eevee.Sleep.Bot.Workers.Crawlers;

namespace Eevee.Sleep.Bot.Workers.InGameAnnouncement;

public class InGameAnnouncementCrawlingWorker (
    InGameAnnouncementCrawler crawler,
    DiscordSocketClient client,
    ILogger<InGameAnnouncementCrawlingWorker> logger
) : AnnouncementCrawlingWorker(crawler, client, logger);
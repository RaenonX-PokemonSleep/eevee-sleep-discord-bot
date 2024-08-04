using Discord.WebSocket;
using Eevee.Sleep.Bot.Workers.Crawlers;

namespace Eevee.Sleep.Bot.Workers.Announcement;

public class OfficialsiteAnnouncementCrawlingWorker (
    OfficialsiteAnnouncementCrawler crawler,
    DiscordSocketClient client,
    ILogger<OfficialsiteAnnouncementCrawlingWorker> logger
) : AnnouncementCrawlingWorker(crawler, client, logger);
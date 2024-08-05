using Discord.WebSocket;
using Eevee.Sleep.Bot.Workers.Crawlers;

namespace Eevee.Sleep.Bot.Workers.Announcement;

public class OfficialSiteAnnouncementCrawlingWorker(
    OfficialSiteAnnouncementCrawler crawler,
    DiscordSocketClient client,
    ILogger<OfficialSiteAnnouncementCrawlingWorker> logger
) : AnnouncementCrawlingWorker(crawler, client, logger);
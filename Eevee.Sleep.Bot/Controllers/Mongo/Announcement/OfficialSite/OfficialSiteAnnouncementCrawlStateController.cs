using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.Announcement.OfficialSite;

public class OfficialSiteAnnouncementCrawlStateController(
    IMongoCollection<OfficialSiteAnnouncementCrawlStateModel> collection
) {
    public async Task<IReadOnlyDictionary<string, DateTime>> FindModifiedTimesAsync(
        IEnumerable<string> urls,
        CancellationToken cancellationToken
    ) {
        var states = await collection.Find(Builders<OfficialSiteAnnouncementCrawlStateModel>.Filter.In(x => x.Url, urls))
            .ToListAsync(cancellationToken);

        return states.ToDictionary(state => state.Url, state => state.LastModifiedUtc);
    }

    public Task Upsert(OfficialSiteNewsEntry entry, CancellationToken cancellationToken) {
        var state = new OfficialSiteAnnouncementCrawlStateModel {
            Url = entry.Url,
            LastModifiedUtc = entry.LastModifiedUtc,
        };

        return collection.ReplaceOneAsync(
            x => x.Url == entry.Url,
            state,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken
        );
    }
}

using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.Announcement.OfficialSite;

public class OfficialSiteAnnouncementCrawlStateController(
    IMongoCollection<OfficialSiteAnnouncementCrawlStateModel> collection
) {
    public DateTime? FindLastModifiedUtc(AnnouncementLanguage language) {
        return collection
            .Find(x => x.Language == language.ToString())
            .Project(x => (DateTime?)x.LastModifiedUtc)
            .FirstOrDefault();
    }

    public Task Upsert(AnnouncementLanguage language, DateTime lastModifiedUtc) {
        var languageName = language.ToString();
        var state = new OfficialSiteAnnouncementCrawlStateModel {
            Language = languageName,
            LastModifiedUtc = lastModifiedUtc,
        };

        return collection.ReplaceOneAsync(
            x => x.Language == languageName,
            state,
            new ReplaceOptions { IsUpsert = true }
        );
    }
}

using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.Announcement.OfficialSite;

public static class OfficialSiteAnnouncementIndexController {
    public static async Task BulkUpsert(OfficialSiteAnnouncementIndexModel[] model) {
        if (model.Length == 0) {
            // Nothing to do if `model` is empty
            return;
        }

        var models = model.Select(
            index => new ReplaceOneModel<OfficialSiteAnnouncementIndexModel>(
                Builders<OfficialSiteAnnouncementIndexModel>.Filter.And(
                    Builders<OfficialSiteAnnouncementIndexModel>.Filter.Where(x => x.AnnouncementId == index.AnnouncementId),
                    Builders<OfficialSiteAnnouncementIndexModel>.Filter.Where(x => x.Language == index.Language)
                ),
                index
            ) {
                IsUpsert = true,
            }
        ).ToList();

        await MongoConst.OfficialSiteAnnouncementIndexCollection.BulkWriteAsync(models);
    }

    public static IEnumerable<OfficialSiteAnnouncementIndexModel> FindAllByLanguage(AnnouncementLanguage language) {
        return MongoConst.OfficialSiteAnnouncementIndexCollection.FindAllByLanguage(language);
    }
}
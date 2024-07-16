using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Models.InGameAnnouncement.OfficialSite;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.InGameAnnouncement.OfficialSite;

public static class OfficialSiteAnnouncememntIndexController {
    public static Task BulkUpsert(OfficialSiteAnnouncementIndexModel[] model) {
        if (model.Length == 0) {
            // Nothing to do if `model` is empty
            return Task.CompletedTask;
        }

        var models = model.Select(index => new ReplaceOneModel<OfficialSiteAnnouncementIndexModel>(
            Builders<OfficialSiteAnnouncementIndexModel>.Filter.Where(x => x.AnnouncementId == index.AnnouncementId),
            index
        ) {
            IsUpsert = true
        }).ToList();
        
        return MongoConst.InGameAnnouncementOfficialSiteIndexCollection.BulkWriteAsync(models);
    }

    public static IEnumerable<OfficialSiteAnnouncementIndexModel> FindAllByLanguage(InGameAnnoucementLanguage language) {
        return MongoConst.InGameAnnouncementOfficialSiteIndexCollection.Find(
            Builders<OfficialSiteAnnouncementIndexModel>.Filter.Where(x => x.Language == language)
        ).ToEnumerable();
    }
}
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Models.InGameAnnouncement.OfficialSite;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.InGameAnnouncement.OfficialSite;

public static class OfficialSiteAnnouncementDetailController {
    public static Task BulkUpsert(OfficialSiteAnnouncementDetailModel[] model) {
        if (model.Length == 0) {
            // Nothing to do if `model` is empty
            return Task.CompletedTask;
        }

        var details = model.Select(detail => new ReplaceOneModel<OfficialSiteAnnouncementDetailModel>(
            Builders<OfficialSiteAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == detail.AnnouncementId),
            detail
        ) {
            IsUpsert = true
        }).ToList();
        
        return MongoConst.InGameAnnouncementOfficialSiteDetailCollection.BulkWriteAsync(details);
    }

    public static IEnumerable<OfficialSiteAnnouncementDetailModel> FindAllByIds(IEnumerable<string> ids) {
        return MongoConst.InGameAnnouncementOfficialSiteDetailCollection.Find(
            Builders<OfficialSiteAnnouncementDetailModel>.Filter.In(x => x.AnnouncementId, ids)
        ).ToEnumerable();
    }

    public static OfficialSiteAnnouncementDetailModel? FindById(InGameAnnoucementLanguage language, string id) {
        return MongoConst.InGameAnnouncementOfficialSiteDetailCollection.Find(
            Builders<OfficialSiteAnnouncementDetailModel>.Filter.And(
                Builders<OfficialSiteAnnouncementDetailModel>.Filter.Where(x => x.Language == language),
                Builders<OfficialSiteAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == id)
            )
        ).FirstOrDefault();
    }
}
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Models.InGameAnnouncement;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.InGameAnnouncement;

public static class InGameAnnouncementDetailController {
    public static Task BulkUpsert(InGameAnnouncementDetailModel[] model) {
        if (model.Length == 0) {
            // Nothing to do if `model` is empty
            return Task.CompletedTask;
        }

        var details = model.Select(detail => new ReplaceOneModel<InGameAnnouncementDetailModel>(
            Builders<InGameAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == detail.AnnouncementId),
            detail
        ) {
            IsUpsert = true
        }).ToList();
        
        return MongoConst.InGameAnnouncementDetailCollection.BulkWriteAsync(details);
    }

    public static IEnumerable<InGameAnnouncementDetailModel> FindAllByIds(IEnumerable<string> ids) {
        return MongoConst.InGameAnnouncementDetailCollection.Find(
            Builders<InGameAnnouncementDetailModel>.Filter.In(x => x.AnnouncementId, ids)
        ).ToEnumerable();
    }

    public static InGameAnnouncementDetailModel? FindById(InGameAnnoucementLanguage language, string id) {
        return MongoConst.InGameAnnouncementDetailCollection.Find(
            Builders<InGameAnnouncementDetailModel>.Filter.And(
                Builders<InGameAnnouncementDetailModel>.Filter.Where(x => x.Language == language),
                Builders<InGameAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == id)
            )
        ).FirstOrDefault();
    }
}
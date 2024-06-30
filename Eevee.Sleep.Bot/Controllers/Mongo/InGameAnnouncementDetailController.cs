using Eevee.Sleep.Bot.Models;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

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
}
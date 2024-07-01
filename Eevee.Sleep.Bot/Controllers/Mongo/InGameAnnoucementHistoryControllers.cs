using Eevee.Sleep.Bot.Models;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class InGameAnnouncememntHistoryController {
    public static async Task Upsert(InGameAnnouncementDetailModel model) {
        await MongoConst.InGameAnnouncementHistoryCollection.InsertOneAsync(model);

        var count = await MongoConst.InGameAnnouncementHistoryCollection
            .CountDocumentsAsync(Builders<InGameAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == model.AnnouncementId));

        if (count > 3) {
            var oldestRecord = await MongoConst.InGameAnnouncementHistoryCollection
                .Find(Builders<InGameAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == model.AnnouncementId))
                .Sort(Builders<InGameAnnouncementDetailModel>.Sort.Ascending(x => x.RecordCreated))
                .Limit(1)
                .FirstOrDefaultAsync();

            await MongoConst.InGameAnnouncementHistoryCollection.DeleteOneAsync(
                Builders<InGameAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == oldestRecord.AnnouncementId)
            );
        }   
    }

    public static async Task BulkUpsert(InGameAnnouncementDetailModel[] models) {
        foreach (var model in models) {
            await Upsert(model);
        }
    }
}
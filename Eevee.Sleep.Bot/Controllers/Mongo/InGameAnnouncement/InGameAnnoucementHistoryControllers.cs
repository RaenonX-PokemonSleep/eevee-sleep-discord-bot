using Eevee.Sleep.Bot.Models.InGameAnnouncement;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.InGameAnnouncement;

public static class InGameAnnouncememntHistoryController {
    public static async Task Upsert(InGameAnnouncementDetailModel model) {
        using var session = await MongoConst.Client.StartSessionAsync();
        session.StartTransaction();

        try{
            await MongoConst.InGameAnnouncementHistoryCollection.InsertOneAsync(model);
            var count = await MongoConst.InGameAnnouncementHistoryCollection
                .CountDocumentsAsync(Builders<InGameAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == model.AnnouncementId));

            if (count > 3)
            {
                var oldestRecord = await MongoConst.InGameAnnouncementHistoryCollection
                    .Find(Builders<InGameAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == model.AnnouncementId))
                    .Sort(Builders<InGameAnnouncementDetailModel>.Sort.Ascending(x => x.RecordCreated))
                    .Limit(1)
                    .FirstOrDefaultAsync();

                await MongoConst.InGameAnnouncementHistoryCollection.DeleteOneAsync(
                    Builders<InGameAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == oldestRecord.AnnouncementId)
                );
            }
            await session.CommitTransactionAsync();
        } catch (Exception) {
            await session.AbortTransactionAsync();
            throw;
        }
    }

    public static async Task BulkUpsert(InGameAnnouncementDetailModel[] models) {
        foreach (var model in models) {
            await Upsert(model);
        }
    }
}
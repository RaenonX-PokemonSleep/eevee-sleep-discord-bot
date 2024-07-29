using Eevee.Sleep.Bot.Models.InGameAnnouncement.InGame;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.InGameAnnouncement.InGame;

public static class InGameAnnouncememntHistoryController {
    private const int MaxHistoryCount = 3;

    public static async Task InSert(InGameAnnouncementDetailModel model) {
        using var session = await MongoConst.Client.StartSessionAsync();
        session.StartTransaction();

        try {
            await MongoConst.InGameAnnouncementHistoryCollection.InsertOneAsync(model);
            var count = await MongoConst.InGameAnnouncementHistoryCollection
                .CountDocumentsAsync(Builders<InGameAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == model.AnnouncementId));

            if (count > MaxHistoryCount) {
                var oldestRecord = await MongoConst.InGameAnnouncementHistoryCollection
                    .Find(Builders<InGameAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == model.AnnouncementId))
                    .Sort(Builders<InGameAnnouncementDetailModel>.Sort.Ascending(x => x.RecordCreatedUtc))
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

    public static async Task BulkInsert(InGameAnnouncementDetailModel[] models) {
        foreach (var model in models) {
            await InSert(model);
        }
    }
}
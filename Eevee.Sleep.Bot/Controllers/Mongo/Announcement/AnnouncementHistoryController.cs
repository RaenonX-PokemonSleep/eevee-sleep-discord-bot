using Eevee.Sleep.Bot.Models.Announcement;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.Announcement;

public class AnnouncementHistoryController<T>(
    IMongoCollection<T> collection
) where T : AnnouncementMetaModel {
    private const int MaxHistoryCount = 3;

    public async Task Insert(T model) {
        using var session = await MongoConst.Client.StartSessionAsync();
        session.StartTransaction();

        try {
            await collection.InsertOneAsync(model);
            var count = await collection
                .CountDocumentsAsync(Builders<T>.Filter.Where(x => x.AnnouncementId == model.AnnouncementId));

            if (count > MaxHistoryCount) {
                var oldestRecord = await collection
                    .Find(Builders<T>.Filter.Where(x => x.AnnouncementId == model.AnnouncementId))
                    .Sort(Builders<T>.Sort.Ascending(x => x.RecordCreatedUtc))
                    .Limit(1)
                    .FirstOrDefaultAsync();

                await collection.DeleteOneAsync(
                    Builders<T>.Filter.Where(x => x.AnnouncementId == oldestRecord.AnnouncementId)
                );
            }
            await session.CommitTransactionAsync();
        } catch (Exception) {
            await session.AbortTransactionAsync();
            throw;
        }
    }

    public async Task BulkInsert(T[] models) {
        foreach (var model in models) {
            await Insert(model);
        }
    }
}
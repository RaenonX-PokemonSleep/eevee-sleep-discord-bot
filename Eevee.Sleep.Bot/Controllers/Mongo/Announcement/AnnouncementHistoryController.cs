using Eevee.Sleep.Bot.Models.Announcement;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.Announcement;

public class AnnouncementHistoryController<T>(
    IMongoCollection<T> collection
) where T : AnnouncementMetaModel {
    private const int MaxHistoryCount = 3;

    private async Task Insert(T model) {
        using var session = await MongoConst.Client.StartSessionAsync();
        session.StartTransaction();

        try {
            await collection.InsertOneAsync(model);
            var count = await collection
                .CountDocumentsAsync(
                    Builders<T>.Filter.And(
                        Builders<T>.Filter.Where(x => x.AnnouncementId == model.AnnouncementId),
                        Builders<T>.Filter.Where(x => x.Language == model.Language)
                    )
                );

            if (count > MaxHistoryCount) {
                var oldestRecord = await collection
                    .Find(
                        Builders<T>.Filter.And(
                            Builders<T>.Filter.Where(x => x.AnnouncementId == model.AnnouncementId),
                            Builders<T>.Filter.Where(x => x.Language == model.Language)
                        )
                    )
                    .Sort(Builders<T>.Sort.Ascending(x => x.RecordCreatedUtc))
                    .Limit(1)
                    .FirstOrDefaultAsync();

                await collection.DeleteOneAsync(
                    Builders<T>.Filter.And(
                        Builders<T>.Filter.Where(x => x.AnnouncementId == oldestRecord.AnnouncementId),
                        Builders<T>.Filter.Where(x => x.Language == oldestRecord.Language)
                    )
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

    public async Task<T?> FindPreviousVersion(T current, Func<T, T, bool> hasSameContent) {
        var versions = await collection
            .Find(
                Builders<T>.Filter.And(
                    Builders<T>.Filter.Where(x => x.AnnouncementId == current.AnnouncementId),
                    Builders<T>.Filter.Where(x => x.Language == current.Language)
                )
            )
            .Sort(Builders<T>.Sort.Descending(x => x.RecordCreatedUtc))
            .Limit(MaxHistoryCount)
            .ToListAsync();

        return versions.FirstOrDefault(version =>
            version.RecordCreatedUtc != current.RecordCreatedUtc ||
            !hasSameContent(version, current)
        );
    }
}
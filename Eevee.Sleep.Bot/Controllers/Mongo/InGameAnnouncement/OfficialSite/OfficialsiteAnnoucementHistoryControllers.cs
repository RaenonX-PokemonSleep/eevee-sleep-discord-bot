using Eevee.Sleep.Bot.Models.InGameAnnouncement.OfficialSite;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.InGameAnnouncement.OfficialSite;

public static class OfficialSiteAnnouncememntHistoryController {
    private const int MaxHistoryCount = 3;

    public static async Task Upsert(OfficialSiteAnnouncementDetailModel model) {
        using var session = await MongoConst.Client.StartSessionAsync();
        session.StartTransaction();

        try {
            await MongoConst.InGameAnnouncementOfficialSiteHistoryCollection.InsertOneAsync(model);
            var count = await MongoConst.InGameAnnouncementOfficialSiteHistoryCollection
                .CountDocumentsAsync(Builders<OfficialSiteAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == model.AnnouncementId));

            if (count > MaxHistoryCount) {
                var oldestRecord = await MongoConst.InGameAnnouncementOfficialSiteHistoryCollection
                    .Find(Builders<OfficialSiteAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == model.AnnouncementId))
                    .Sort(Builders<OfficialSiteAnnouncementDetailModel>.Sort.Ascending(x => x.RecordCreatedUtc))
                    .Limit(1)
                    .FirstOrDefaultAsync();

                await MongoConst.InGameAnnouncementOfficialSiteHistoryCollection.DeleteOneAsync(
                    Builders<OfficialSiteAnnouncementDetailModel>.Filter.Where(x => x.AnnouncementId == oldestRecord.AnnouncementId)
                );
            }
            await session.CommitTransactionAsync();
        } catch (Exception) {
            await session.AbortTransactionAsync();
            throw;
        }
    }

    public static async Task BulkUpsert(OfficialSiteAnnouncementDetailModel[] models) {
        foreach (var model in models) {
            await Upsert(model);
        }
    }
}
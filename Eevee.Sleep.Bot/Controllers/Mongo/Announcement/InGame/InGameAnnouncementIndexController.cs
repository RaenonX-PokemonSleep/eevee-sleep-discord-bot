using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement.InGame;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.Announcement.InGame;

public static class InGameAnnouncementIndexController {
    /// <summary>
    /// Bulk upserts the given models into the database.
    /// </summary>
    /// <returns>A list of models that were modified or inserted.</returns>
    public static async Task<IEnumerable<InGameAnnouncementIndexModel>> BulkUpsert(InGameAnnouncementIndexModel[] models) {
        var announcements = models.Select(index => new ReplaceOneModel<InGameAnnouncementIndexModel>(
            Builders<InGameAnnouncementIndexModel>.Filter.Where(x => x.AnnouncementId == index.AnnouncementId),
            index
        ) {
            IsUpsert = true
        });
    
        var existingRecords = FindAllByAnnouncementIds(models.Select(x => x.AnnouncementId)).ToList();

        var result = await MongoConst.InGameAnnouncementIndexCollection.BulkWriteAsync(announcements);

        var modifiedRecords = models
            .Where(updated => existingRecords.Any(existing => existing.AnnouncementId == updated.AnnouncementId && existing.Hash != updated.Hash))
            .Select(x => x);

        return modifiedRecords
            .Concat(models.Where(x => !existingRecords.Any(y => y.AnnouncementId == x.AnnouncementId)));
    }

    public static IEnumerable<InGameAnnouncementIndexModel> FindAllByAnnouncementIds(IEnumerable<string> ids) {
        return MongoConst.InGameAnnouncementIndexCollection.Find(
            Builders<InGameAnnouncementIndexModel>.Filter.In(x => x.AnnouncementId, ids)
        ).ToEnumerable();
    }

    public static IEnumerable<InGameAnnouncementIndexModel> FindAllByLanguage(AnnouncementLanguage language) {
        return MongoConst.InGameAnnouncementIndexCollection.FindAllByLanguage(language);
    }
}
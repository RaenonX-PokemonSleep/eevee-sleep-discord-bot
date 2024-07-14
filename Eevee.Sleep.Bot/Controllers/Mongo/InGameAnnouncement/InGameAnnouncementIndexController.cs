using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Models.InGameAnnouncement;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.InGameAnnouncement;

public static class InGameAnnouncememntIndexController {
    public static Task BulkUpsert(InGameAnnouncementIndexModel[] model) {
        if (model.Length == 0) {
            // Nothing to do if `model` is empty
            return Task.CompletedTask;
        }

        var models = model.Select(index => new ReplaceOneModel<InGameAnnouncementIndexModel>(
            Builders<InGameAnnouncementIndexModel>.Filter.Where(x => x.AnnouncementId == index.AnnouncementId),
            index
        ) {
            IsUpsert = true
        }).ToList();
        
        return MongoConst.InGameAnnouncementIndexCollection.BulkWriteAsync(models);
    }

    public static IEnumerable<InGameAnnouncementIndexModel> FindAllByLanguage(InGameAnnoucementLanguage language) {
        return MongoConst.InGameAnnouncementIndexCollection.Find(
            Builders<InGameAnnouncementIndexModel>.Filter.Where(x => x.Language == language)
        ).ToEnumerable();
    }
}
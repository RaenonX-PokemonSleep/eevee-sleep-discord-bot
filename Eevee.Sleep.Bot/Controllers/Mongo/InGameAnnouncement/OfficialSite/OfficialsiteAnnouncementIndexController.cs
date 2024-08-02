using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Models.InGameAnnouncement.Officialsite;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.InGameAnnouncement.Officialsite;

public static class OfficialsiteAnnouncememntIndexController {
    public static Task BulkUpsert(OfficialsiteAnnouncementIndexModel[] model) {
        if (model.Length == 0) {
            // Nothing to do if `model` is empty
            return Task.CompletedTask;
        }

        var models = model.Select(index => new ReplaceOneModel<OfficialsiteAnnouncementIndexModel>(
            Builders<OfficialsiteAnnouncementIndexModel>.Filter.Where(x => x.AnnouncementId == index.AnnouncementId),
            index
        ) {
            IsUpsert = true
        }).ToList();
        
        return MongoConst.OfficialsiteAnnouncementIndexCollection.BulkWriteAsync(models);
    }

    public static IEnumerable<OfficialsiteAnnouncementIndexModel> FindAllByLanguage(InGameAnnoucementLanguage language) {
        return MongoConst.OfficialsiteAnnouncementIndexCollection.Find(
            Builders<OfficialsiteAnnouncementIndexModel>.Filter.Where(x => x.Language == language)
        ).ToEnumerable();
    }
}
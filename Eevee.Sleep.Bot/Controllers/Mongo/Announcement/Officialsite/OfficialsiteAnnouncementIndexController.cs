using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement.Officialsite;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.Announcement.Officialsite;

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

    public static IEnumerable<OfficialsiteAnnouncementIndexModel> FindAllByLanguage(AnnouncementLanguage language) {
        return MongoConst.OfficialsiteAnnouncementIndexCollection.FindAllByLanguage(language);
    }
}
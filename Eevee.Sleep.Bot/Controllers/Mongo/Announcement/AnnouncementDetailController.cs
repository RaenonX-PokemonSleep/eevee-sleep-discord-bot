using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Models.Announcement;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo.Announcement;

public class AnnouncementDetailController<T>(
    IMongoCollection<T> collection
) where T : AnnouncementMetaModel {
    public Task BulkUpsert(T[] model) {
        if (model.Length == 0) {
            // Nothing to do if `model` is empty
            return Task.CompletedTask;
        }

        var details = model.Select(
            detail => new ReplaceOneModel<T>(
                Builders<T>.Filter.Where(x => x.AnnouncementId == detail.AnnouncementId),
                detail
            ) {
                IsUpsert = true,
            }
        );

        return collection.BulkWriteAsync(details);
    }

    public IEnumerable<T> FindAllByIds(IEnumerable<string> ids) {
        return collection.Find(
            Builders<T>.Filter.In(x => x.AnnouncementId, ids)
        ).ToEnumerable();
    }

    public T? FindById(AnnouncementLanguage language, string id) {
        return collection.Find(
            Builders<T>.Filter.And(
                Builders<T>.Filter.Where(x => x.Language == language),
                Builders<T>.Filter.Where(x => x.AnnouncementId == id)
            )
        ).FirstOrDefault();
    }
}
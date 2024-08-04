using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Models.Announcement;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Extensions;

public static class IMongoCollectionExtensions {
    public static IEnumerable<T> FindAllByLanguage<T>(
        this IMongoCollection<T> collection, 
        AnnouncementLanguage language
    ) where T : AnnouncementMetaModel {
        return collection.Find(
            Builders<T>.Filter.Where(x => x.Language == language)
        ).ToEnumerable();
    }
}

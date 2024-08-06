using MongoDB.Bson;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class MongoCollectionConfigManager {
    public static void EnableChangeStreamPreAndPostImagesOnCollection<T>(IMongoCollection<T> collection) {
        collection.Database.RunCommand(
            new JsonCommand<BsonDocument>(
                $"{{" +
                $"  collMod: \"{collection.CollectionNamespace.CollectionName}\"" +
                $"  changeStreamPreAndPostImages: {{enabled: true}}" +
                $"}}"
            )
        );
    }
}
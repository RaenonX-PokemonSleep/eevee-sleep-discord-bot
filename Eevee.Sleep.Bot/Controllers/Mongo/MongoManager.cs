using Eevee.Sleep.Bot.Models.CustomSerializers;
using Eevee.Sleep.Bot.Utils;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class MongoManager {
    private static readonly ILogger Logger = LogHelper.CreateLogger(typeof(MongoManager));

    public static async Task Initialize() {
        MongoConst.Client.Ping();

        BsonSerializer.RegisterSerializer(new DateOnlySerializer());

        await Task.WhenAll(MongoIndexManager.Initialize());
    }

    private static void Ping(this IMongoClient client) {
        try {
            Logger.LogInformation("Testing connection to MongoDB at {MongoUrl}", MongoConst.Url);
            client.ListDatabaseNames().MoveNext();
        } catch (TimeoutException e) {
            Logger.LogError(e, "Error connecting to MongoDB at {MongoUrl}", MongoConst.Url);
            Environment.Exit(1);
            throw;
        }
    }
}
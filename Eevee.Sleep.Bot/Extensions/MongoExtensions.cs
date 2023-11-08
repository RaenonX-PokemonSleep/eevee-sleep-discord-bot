using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Extensions;


public static class MongoExtensions {
    public static IMongoClient Initialize(this IMongoClient client) {
        RegisterConvention();
        RegisterSerializer();

        return client;
    }

    private static void RegisterConvention() {
        ConventionRegistry.Register(
            name: "CamelCaseConvention",
            conventions: new ConventionPack { new CamelCaseElementNameConvention() },
            filter: _ => true
        );
    }

    private static void RegisterSerializer() {
        RegisterGlobalSerializer();
    }

    private static void RegisterGlobalSerializer() {
        // By default, `decimal` are stored in `string`, which is undesired
        BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
    }
}
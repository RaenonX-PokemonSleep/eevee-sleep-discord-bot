using Eevee.Sleep.Bot.Models;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class MongoIndexManager {
    public static IEnumerable<Task> Initialize() {
        return new[] {
            ActivationKeySourceIndex(),
            ActivationDataSourceIndex(),
            ActivationPresetSourceIndex(),
            ActivationPresetTagIndex(),
            ActivationPresetUuidIndex()
        };
    }

    private static async Task ActivationKeySourceIndex() {
        var indexKeys = Builders<ActivationKeyModel>.IndexKeys
            .Ascending(data => data.Source);

        var indexModel = new CreateIndexModel<ActivationKeyModel>(indexKeys);

        await MongoConst.AuthActivationKeyCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static async Task ActivationDataSourceIndex() {
        var indexKeys = Builders<ActivationDataModel>.IndexKeys
            .Ascending(data => data.Source);
        var indexModel = new CreateIndexModel<ActivationDataModel>(indexKeys);

        await MongoConst.AuthActivationDataCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static async Task ActivationPresetSourceIndex() {
        var indexKeys = Builders<ActivationPresetModel>.IndexKeys
            .Ascending(data => data.Source);
        var indexModel = new CreateIndexModel<ActivationPresetModel>(indexKeys);

        await MongoConst.AuthActivationPresetCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static async Task ActivationPresetTagIndex() {
        var indexOptions = new CreateIndexOptions {
            Unique = true
        };
        var indexKeys = Builders<ActivationPresetModel>.IndexKeys
            .Ascending(data => data.Source)
            .Ascending(data => data.Tag);
        var indexModel = new CreateIndexModel<ActivationPresetModel>(indexKeys, indexOptions);

        await MongoConst.AuthActivationPresetCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static async Task ActivationPresetUuidIndex() {
        var indexOptions = new CreateIndexOptions {
            Unique = true
        };
        var indexKeys = Builders<ActivationPresetModel>.IndexKeys
            .Ascending(data => data.Uuid);
        var indexModel = new CreateIndexModel<ActivationPresetModel>(indexKeys, indexOptions);

        await MongoConst.AuthActivationPresetCollection.Indexes.CreateOneAsync(indexModel);
    }
}
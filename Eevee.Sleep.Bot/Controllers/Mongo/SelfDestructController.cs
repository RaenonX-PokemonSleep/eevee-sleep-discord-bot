using Eevee.Sleep.Bot.Models;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class SelfDestructController {
    public static Task InsertManySelfDestructMessages(
        IEnumerable<SelfDestructMessageModel> models,
        IClientSessionHandle? session = null
    ) {
        return session is not null
            ? MongoConst.DiscordSelfDestructCollection.InsertManyAsync(session, models)
            : MongoConst.DiscordSelfDestructCollection.InsertManyAsync(models);
    }

    public static async Task<SelfDestructMessageModel[]> FindExpiredMessages() {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return (await MongoConst.DiscordSelfDestructCollection
            .Find(Builders<SelfDestructMessageModel>.Filter.Lte(x => x.DestructAtEpochSec, now))
            .ToListAsync())
            .ToArray();
    }

    public static Task DeleteByMessageId(ulong messageId) {
        return MongoConst.DiscordSelfDestructCollection
            .DeleteOneAsync(
                Builders<SelfDestructMessageModel>.Filter.Where(x => x.MessageId == messageId)
            );
    }

}

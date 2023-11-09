using Eevee.Sleep.Bot.Models;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class ActivationController {
    public static Task<DeleteResult[]> RemoveDiscordActivationData(string userId) {
        return Task.WhenAll(
            MongoConst.AuthActivationDataCollection.DeleteOneAsync(
                Builders<ActivationDataModel>.Filter.Where(x => x.Contact.Discord == userId)
            ),
            MongoConst.AuthActivationKeyCollection.DeleteOneAsync(
                Builders<ActivationKeyModel>.Filter.Where(x => x.Contact.Discord == userId)
            )
        );
    }
}
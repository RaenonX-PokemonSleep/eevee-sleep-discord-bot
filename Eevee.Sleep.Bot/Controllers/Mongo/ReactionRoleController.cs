using Eevee.Sleep.Bot.Models;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class ReactionRoleController {
    public static Task InsertReactionRoleMessage(
        ReactionRoleMessageModel model,
        IClientSessionHandle? session = null
    ) {
        return session is not null
            ? MongoConst.DiscordReactionRoleCollection.InsertOneAsync(session, model)
            : MongoConst.DiscordReactionRoleCollection.InsertOneAsync(model);
    }

    public static async Task<ReactionRoleMessageModel?> FindByMessageId(ulong messageId) {
        return await MongoConst.DiscordReactionRoleCollection
            .Find(Builders<ReactionRoleMessageModel>.Filter.Where(x => x.MessageId == messageId))
            .FirstOrDefaultAsync();
    }

    public static Task DeleteByMessageId(ulong messageId, IClientSessionHandle? session = null) {
        var filter = Builders<ReactionRoleMessageModel>.Filter.Where(x => x.MessageId == messageId);

        return session is not null
            ? MongoConst.DiscordReactionRoleCollection.DeleteOneAsync(session, filter)
            : MongoConst.DiscordReactionRoleCollection.DeleteOneAsync(filter);
    }

}

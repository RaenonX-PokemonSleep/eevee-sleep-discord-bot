using Eevee.Sleep.Bot.Models.ChesterMicroService;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class ChesterCurrentVersionController {
    public static async Task<ChesterCurrentVersion?> UpdateAndGetOriginal(ChesterCurrentVersion currentVersion) {
        using var session = await MongoConst.Client.StartSessionAsync();
        session.StartTransaction();

        var emptyFilter = Builders<ChesterCurrentVersion>.Filter.Empty;

        try {
            var original = await MongoConst.GameChesterCurrentVersionCollection
                .Find(emptyFilter)
                .FirstOrDefaultAsync();

            await MongoConst.GameChesterCurrentVersionCollection.FindOneAndReplaceAsync(
                session,
                emptyFilter,
                currentVersion,
                new FindOneAndReplaceOptions<ChesterCurrentVersion> { IsUpsert = true }
            );

            await session.CommitTransactionAsync();
            return original;
        } catch (Exception) {
            await session.AbortTransactionAsync();
            throw;
        }
    }
}
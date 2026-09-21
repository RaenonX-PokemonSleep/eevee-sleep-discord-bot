using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class ActivationController {
    public static async Task<TimeSpan?> RevokeDiscordActivationAndGetSubscriptionDuration(string userId) {
        var now = DateTime.UtcNow;
        var filter = Builders<ActivationDataModel>.Filter.Where(x =>
            x.Source == GlobalConst.SubscriptionSource.Discord &&
            x.Contact.Discord == userId && x.Revocation == null && x.ConsumedAt == null
        );
        var activations = await MongoConst.AuthActivationDataCollection.Find(filter).ToListAsync();
        if (activations.Count == 0) {
            return null;
        }

        await MongoConst.AuthActivationDataCollection.UpdateManyAsync(
            filter,
            Builders<ActivationDataModel>.Update.Set(x => x.Revocation, new ActivationRevocationModel {
                At = now,
                Reason = "Discord subscription role removed",
            })
        );

        return now - activations.Min(x => x.GeneratedAt);
    }

    public static async Task<ActivationPropertiesModel[]> GetExternalSubscribersWithDiscordContact() {
        var now = DateTime.UtcNow;
        var activations = await MongoConst.AuthActivationDataCollection.Find(x =>
            (
                x.Source == GlobalConst.SubscriptionSource.Github ||
                x.Source == GlobalConst.SubscriptionSource.Patreon ||
                x.Source == GlobalConst.SubscriptionSource.Stripe ||
                x.Source == GlobalConst.SubscriptionSource.Afdian
            ) &&
            x.Contact.Discord != null && x.Expiry > now &&
            x.Revocation == null && x.ConsumedAt == null
        ).ToListAsync();

        return [..activations];
    }
}

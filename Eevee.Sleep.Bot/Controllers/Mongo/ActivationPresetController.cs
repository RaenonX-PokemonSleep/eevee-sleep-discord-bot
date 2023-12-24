using Eevee.Sleep.Bot.Utils;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class ActivationPresetController {
    public static HashSet<string> GetTaggedRoleIds() {
        return MongoConst.AuthActivationPresetCollection
            .Find(x =>
                x.Source == GlobalConst.SubscriptionSourceOfDiscord ||
                x.Source == GlobalConst.SubscriptionSourceOfPatreon
            )
            .ToEnumerable()
            .Select(x => x.Tag)
            .ToHashSet();
    }
}
using Eevee.Sleep.Bot.Utils;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class ActivationPresetController {
    public static HashSet<ulong> GetTaggedRoleIds() {
        return MongoConst.AuthActivationPresetCollection
            .Find(x => x.Source == GlobalConst.SubscriptionSourceOfDiscord)
            .ToEnumerable()
            .Select(x => ulong.Parse(x.Tag))
            .ToHashSet();
    }
}
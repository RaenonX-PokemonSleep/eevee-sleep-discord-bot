using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class ActivationPresetController {
    public static HashSet<ActivationPresetRole> GetTaggedRoles() {
        return MongoConst.AuthActivationPresetCollection
            .Find(x => x.Source == GlobalConst.SubscriptionSourceOfDiscord)
            .ToEnumerable()
            .Select(x => new ActivationPresetRole {
                RoleId = ulong.Parse(x.Tag),
                Suspended = x.Suspended
            })
            .ToHashSet();
    }
}
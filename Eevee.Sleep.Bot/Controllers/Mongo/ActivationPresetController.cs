using System.Linq.Expressions;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class ActivationPresetController {
    private static HashSet<ActivationPresetRole> GetTaggedRoles(Expression<Func<ActivationPresetModel, bool>> filter) {
        return MongoConst.AuthActivationPresetCollection
            .Find(filter)
            .ToEnumerable()
            .Select(
                x => new ActivationPresetRole {
                    RoleId = ulong.Parse(x.Tag),
                    Suspended = x.Suspended,
                }
            )
            .ToHashSet();
    }

    public static HashSet<ActivationPresetRole> GetTaggedRolesAll() {
        return GetTaggedRoles(
            x =>
                x.Source == GlobalConst.SubscriptionSource.Discord ||
                x.Source == GlobalConst.SubscriptionSource.DiscordOneTime
        );
    }

    public static HashSet<ActivationPresetRole> GetTaggedRolesSubscribersOnly() {
        return GetTaggedRoles(
            x => x.Source == GlobalConst.SubscriptionSource.Discord
        );
    }

    public static ActivationPresetModel? GetPresetByUuid(string? presetUuid) {
        return presetUuid is null ?
            null :
            MongoConst.AuthActivationPresetCollection.Find(x => x.Uuid == presetUuid).FirstOrDefault();
    }

    public static Dictionary<string, ActivationPresetModel> GetPresetDictByUuid(IEnumerable<string> uuidList) {
        return MongoConst.AuthActivationPresetCollection
            .Find(Builders<ActivationPresetModel>.Filter.In(x => x.Uuid, uuidList))
            .ToEnumerable()
            .ToDictionary(x => x.Uuid);
    }
}
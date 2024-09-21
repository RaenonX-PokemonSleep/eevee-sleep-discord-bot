using Discord;
using Eevee.Sleep.Bot.Models;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class DiscordRestrictedRoleController {
    public static Task SaveRestrictedRole(IRole role, uint? minAccountAgeDays) {
        return MongoConst.DiscordRestrictedRoleCollection
            .UpdateOneAsync(
                Builders<RoleRestrictionModel>.Filter.Where(x => x.RoleId == role.Id),
                Builders<RoleRestrictionModel>.Update
                    .Set(x => x.RoleId, role.Id)
                    .Set(x => x.MinAccountAgeDays, minAccountAgeDays),
                new UpdateOptions { IsUpsert = true }
            );
    }

    public static Task AddWhiteListUserToRestrictedRole(ulong roleId, ulong userId) {
        return MongoConst.DiscordRestrictedRoleCollection
            .UpdateOneAsync(
                Builders<RoleRestrictionModel>.Filter.Where(x => x.RoleId == roleId),
                Builders<RoleRestrictionModel>.Update.AddToSet(x => x.WhitelistedUserIds, userId)
            );
    }

    public static Task RemoveRestrictedRole(ulong roleId) {
        return MongoConst.DiscordRestrictedRoleCollection
            .DeleteOneAsync(
                Builders<RoleRestrictionModel>.Filter.Where(x => x.RoleId == roleId)
            );
    }

    public static RoleRestrictionModel[] FindAllRestrictedRoleByRoleIds(ulong[] roleId) {
        return MongoConst.DiscordRestrictedRoleCollection
            .Find(Builders<RoleRestrictionModel>.Filter.In(x => x.RoleId, roleId))
            .ToEnumerable()
            .ToArray();
    }
}
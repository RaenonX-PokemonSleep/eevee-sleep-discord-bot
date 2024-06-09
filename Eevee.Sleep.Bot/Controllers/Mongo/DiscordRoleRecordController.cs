using Eevee.Sleep.Bot.Models;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class DiscordRoleRecordContoller {
    public static Task SaveRoles(ulong userId, ulong[] roles) {
        return MongoConst.DiscordRoleRecordCollection.UpdateOneAsync(
            Builders<RoleRecordModel>.Filter.Where(x => x.UserId == userId),
            Builders<RoleRecordModel>.Update.Set(x => x.Roles, roles), 
            new UpdateOptions { IsUpsert = true }
        );
    }

    public static Task RemoveRoles(ulong userId, ulong[] roles) {
        return MongoConst.DiscordRoleRecordCollection.UpdateOneAsync(
            Builders<RoleRecordModel>.Filter.Where(x => x.UserId == userId),
            Builders<RoleRecordModel>.Update.PullAll(x => x.Roles, roles)
        );
    }

    public static Task BulkRemoveRoles(ulong[] userIds, ulong[] roles) {
        return MongoConst.DiscordRoleRecordCollection.UpdateManyAsync(
            Builders<RoleRecordModel>.Filter.In(x => x.UserId, userIds),
            Builders<RoleRecordModel>.Update.PullAll(x => x.Roles, roles)
        );
    }

    public static Task AddRoles(ulong userId, ulong[] roles) {
        return MongoConst.DiscordRoleRecordCollection.UpdateOneAsync(
            Builders<RoleRecordModel>.Filter.Where(x => x.UserId == userId),
            Builders<RoleRecordModel>.Update.AddToSetEach(x => x.Roles, roles),
            new UpdateOptions { IsUpsert = true }
        );
    }

    public static Task BulkAddRoles(ulong[] userIds, ulong[] roles) {
        var bulkOps = new List<WriteModel<RoleRecordModel>>();
        foreach (var userId in userIds) {
            bulkOps.Add(
                new UpdateOneModel<RoleRecordModel>(
                    Builders<RoleRecordModel>.Filter.Where(r => r.UserId == userId),
                    Builders<RoleRecordModel>.Update.AddToSetEach(r => r.Roles, roles)
                ) { IsUpsert = true }
            );
        }

        return MongoConst.DiscordRoleRecordCollection.BulkWriteAsync(bulkOps);
    }

    public static ulong[] FindRoleIdsByUserId(ulong userId) {
        var result = MongoConst.DiscordRoleRecordCollection.Find(
            Builders<RoleRecordModel>.Filter.Where(x => x.UserId == userId)
        ).FirstOrDefault();

        return result?.Roles ?? Array.Empty<ulong>();
    }
}
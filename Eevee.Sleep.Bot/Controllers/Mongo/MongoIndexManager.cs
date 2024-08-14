using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Models.Announcement.InGame;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class MongoIndexManager {
    public static IEnumerable<Task> Initialize() {
        return new[] {
            ActivationKeySourceIndex(),
            ActivationDataSourceIndex(),
            ActivationPresetSourceIndex(),
            ActivationPresetTagIndex(),
            ActivationPresetUuidIndex(),
            DiscordRoleRecordUserIdIndex(),
            DiscordTrackedRoleRoleIdIndex(),
            OfficialSiteAnnouncementIndexAnnounceIdIndex(),
            OfficialSiteAnnouncementDetailAnnounceIdIndex(),
            OfficialSiteAnnouncementDetailLanguageAnnounceIdIndex(),
            OfficialSiteAnnouncementHistoryAnnounceIdIndex(),
            InGameAnnouncementIndexAnnounceIdIndex(),
            InGameAnnouncementDetailAnnounceIdIndex(),
            InGameAnnouncementHistoryAnnounceIdIndex(),
        };
    }

    private static Task<string> ActivationKeySourceIndex() {
        var indexKeys = Builders<ActivationKeyModel>.IndexKeys
            .Ascending(data => data.Source);

        var indexModel = new CreateIndexModel<ActivationKeyModel>(indexKeys);

        return MongoConst.AuthActivationKeyCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> ActivationDataSourceIndex() {
        var indexKeys = Builders<ActivationDataModel>.IndexKeys
            .Ascending(data => data.Source);
        var indexModel = new CreateIndexModel<ActivationDataModel>(indexKeys);

        return MongoConst.AuthActivationDataCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> ActivationPresetSourceIndex() {
        var indexKeys = Builders<ActivationPresetModel>.IndexKeys
            .Ascending(data => data.Source);
        var indexModel = new CreateIndexModel<ActivationPresetModel>(indexKeys);

        return MongoConst.AuthActivationPresetCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> ActivationPresetTagIndex() {
        var indexOptions = new CreateIndexOptions {
            Unique = true,
        };
        var indexKeys = Builders<ActivationPresetModel>.IndexKeys
            .Ascending(data => data.Source)
            .Ascending(data => data.Tag);
        var indexModel = new CreateIndexModel<ActivationPresetModel>(indexKeys, indexOptions);

        return MongoConst.AuthActivationPresetCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> ActivationPresetUuidIndex() {
        var indexOptions = new CreateIndexOptions {
            Unique = true,
        };
        var indexKeys = Builders<ActivationPresetModel>.IndexKeys
            .Ascending(data => data.Uuid);
        var indexModel = new CreateIndexModel<ActivationPresetModel>(indexKeys, indexOptions);

        return MongoConst.AuthActivationPresetCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> DiscordRoleRecordUserIdIndex() {
        var indexKeys = Builders<RoleRecordModel>.IndexKeys
            .Ascending(data => data.UserId);
        var indexModel = new CreateIndexModel<RoleRecordModel>(indexKeys);

        return MongoConst.DiscordRoleRecordCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> DiscordTrackedRoleRoleIdIndex() {
        var indexKeys = Builders<TrackedRoleModel>.IndexKeys
            .Ascending(data => data.RoleId);
        var indexModel = new CreateIndexModel<TrackedRoleModel>(indexKeys);

        return MongoConst.DiscordTrackedRoleCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> OfficialSiteAnnouncementIndexAnnounceIdIndex() {
        var indexKeys = Builders<OfficialSiteAnnouncementIndexModel>.IndexKeys
            .Ascending(data => data.AnnouncementId);
        var indexModel = new CreateIndexModel<OfficialSiteAnnouncementIndexModel>(
            indexKeys,
            new CreateIndexOptions { Unique = true }
        );

        return MongoConst.OfficialSiteAnnouncementIndexCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> OfficialSiteAnnouncementDetailAnnounceIdIndex() {
        var indexKeys = Builders<OfficialSiteAnnouncementDetailModel>.IndexKeys
            .Ascending(data => data.AnnouncementId);
        var indexModel = new CreateIndexModel<OfficialSiteAnnouncementDetailModel>(
            indexKeys,
            new CreateIndexOptions { Unique = true }
        );

        return MongoConst.OfficialSiteAnnouncementDetailCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> OfficialSiteAnnouncementDetailLanguageAnnounceIdIndex() {
        var indexKeys = Builders<OfficialSiteAnnouncementDetailModel>.IndexKeys
            .Ascending(data => data.Language)
            .Ascending(data => data.AnnouncementId);
        var indexModel = new CreateIndexModel<OfficialSiteAnnouncementDetailModel>(
            indexKeys,
            new CreateIndexOptions { Unique = true }
        );

        return MongoConst.OfficialSiteAnnouncementDetailCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> OfficialSiteAnnouncementHistoryAnnounceIdIndex() {
        var indexKeys = Builders<OfficialSiteAnnouncementDetailModel>.IndexKeys
            .Ascending(data => data.AnnouncementId)
            .Ascending(data => data.RecordCreatedUtc);
        var indexModel = new CreateIndexModel<OfficialSiteAnnouncementDetailModel>(indexKeys);

        return MongoConst.OfficialSiteAnnouncementHistoryCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> InGameAnnouncementIndexAnnounceIdIndex() {
        var indexKeys = Builders<InGameAnnouncementIndexModel>.IndexKeys
            .Ascending(data => data.AnnouncementId);
        var indexModel = new CreateIndexModel<InGameAnnouncementIndexModel>(
            indexKeys,
            new CreateIndexOptions { Unique = true }
        );

        return MongoConst.InGameAnnouncementIndexCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> InGameAnnouncementDetailAnnounceIdIndex() {
        var indexKeys = Builders<InGameAnnouncementDetailModel>.IndexKeys
            .Ascending(data => data.AnnouncementId);
        var indexModel = new CreateIndexModel<InGameAnnouncementDetailModel>(
            indexKeys,
            new CreateIndexOptions { Unique = true }
        );

        return MongoConst.InGameAnnouncementDetailCollection.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> InGameAnnouncementHistoryAnnounceIdIndex() {
        var indexKeys = Builders<InGameAnnouncementDetailModel>.IndexKeys
            .Ascending(data => data.AnnouncementId)
            .Ascending(data => data.RecordCreatedUtc);
        var indexModel = new CreateIndexModel<InGameAnnouncementDetailModel>(indexKeys);

        return MongoConst.InGameAnnouncementHistoryCollection.Indexes.CreateOneAsync(indexModel);
    }
}
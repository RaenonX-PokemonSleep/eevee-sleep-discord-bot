﻿using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class MongoConst {
    public static readonly string Url = ConfigHelper.GetMongoDbUrl();

    public static readonly IMongoClient Client = new MongoClient(Url).Initialize();

    private static readonly IMongoDatabase AuthDatabase = Client.GetDatabase("auth");

    public static readonly IMongoCollection<ActivationDataModel> AuthActivationDataCollection =
        AuthDatabase.GetCollection<ActivationDataModel>("activation");

    public static readonly IMongoCollection<ActivationKeyModel> AuthActivationKeyCollection =
        AuthDatabase.GetCollection<ActivationKeyModel>("activationKey");

    public static readonly IMongoCollection<ActivationPresetModel> AuthActivationPresetCollection =
        AuthDatabase.GetCollection<ActivationPresetModel>("activationPreset");

    private static readonly IMongoDatabase DiscordDatabase = Client.GetDatabase("discord");

    public static readonly IMongoCollection<RoleRecordModel> DiscordRoleRecordCollection =
        DiscordDatabase.GetCollection<RoleRecordModel>("role/record");

    public static readonly IMongoCollection<TrackedRoleModel> DiscordTrackedRoleCollection =
        DiscordDatabase.GetCollection<TrackedRoleModel>("role/tracked");

    public static readonly IMongoDatabase GameDatabase = Client.GetDatabase("game");

    public static readonly IMongoCollection<InGameAnnouncementIndexModel> InGameAnnouncementIndexCollection =
        GameDatabase.GetCollection<InGameAnnouncementIndexModel>("announcement/index");

    public static readonly IMongoCollection<InGameAnnouncementDetailModel> InGameAnnouncementDetailCollection =
        GameDatabase.GetCollection<InGameAnnouncementDetailModel>("announcement/details");

    public static readonly IMongoCollection<InGameAnnouncementDetailModel> InGameAnnouncementHistoryCollection =
        GameDatabase.GetCollection<InGameAnnouncementDetailModel>("announcement/history");
}
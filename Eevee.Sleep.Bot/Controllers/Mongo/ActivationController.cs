using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class ActivationController {
    public static async Task<TimeSpan?> RemoveDiscordActivationAndGetSubscriptionDuration(string userId) {
        var removeActivationKeyTask = MongoConst.AuthActivationKeyCollection.FindOneAndDeleteAsync(
            Builders<ActivationKeyModel>.Filter.Where(x => x.Contact.Discord == userId)
        );
        var removeActivationDataTask = MongoConst.AuthActivationDataCollection.FindOneAndDeleteAsync(
            Builders<ActivationDataModel>.Filter.Where(x => x.Contact.Discord == userId)
        );

        await Task.WhenAll(removeActivationKeyTask, removeActivationDataTask);

        var earliestGeneration = new[] {
            (await removeActivationKeyTask)?.GeneratedAt,
            (await removeActivationDataTask)?.GeneratedAt,
        }.Min();

        if (earliestGeneration is null) {
            return null;
        }

        return DateTime.UtcNow - earliestGeneration.Value;
    }

    public static async Task<ActivationPropertiesModel[]> GetExternalSubscribersWithDiscordContact() {
        var activationKeyList = await MongoConst.AuthActivationKeyCollection.Find(
            x =>
                (
                    x.Source == GlobalConst.SubscriptionSourceOfGithub ||
                    x.Source == GlobalConst.SubscriptionSourceOfPatreon
                ) &&
                x.Contact.Discord != null
        ).ToListAsync();
        var activationDataList = await MongoConst.AuthActivationDataCollection.Find(
            x =>
                (
                    x.Source == GlobalConst.SubscriptionSourceOfGithub ||
                    x.Source == GlobalConst.SubscriptionSourceOfPatreon
                ) &&
                x.Contact.Discord != null
        ).ToListAsync();

        return [..activationKeyList, ..activationDataList];
    }
}
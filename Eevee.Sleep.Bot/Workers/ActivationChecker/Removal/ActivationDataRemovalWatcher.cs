using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Models;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Workers.ActivationChecker.Removal;

public class ActivationDataRemovalWatcher(
    DiscordSocketClient client,
    ILogger<ActivationDataRemovalWatcher> logger
) : ActivationRemovalWatcher<ActivationDataModel>(client, logger) {
    protected override IMongoCollection<ActivationDataModel> GetMongoCollection() {
        return MongoConst.AuthActivationDataCollection;
    }
}
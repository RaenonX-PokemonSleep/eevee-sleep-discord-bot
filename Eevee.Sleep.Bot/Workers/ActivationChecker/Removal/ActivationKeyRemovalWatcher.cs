using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Models;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Workers.ActivationChecker.Removal;

public class ActivationKeyRemovalWatcher(
    DiscordSocketClient client,
    ILogger<ActivationKeyRemovalWatcher> logger
) : ActivationRemovalWatcher<ActivationKeyModel>(client, logger) {
    protected override IMongoCollection<ActivationKeyModel> GetMongoCollection() {
        return MongoConst.AuthActivationKeyCollection;
    }
}
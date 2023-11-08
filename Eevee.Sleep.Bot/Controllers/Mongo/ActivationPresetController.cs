using Eevee.Sleep.Bot.Models;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class ActivationPresetController {
    public static Task<List<ActivationPresetModel>> GetTaggedPreset() {
        return MongoConst.AuthActivationPresetCollection
            .Find(x => x.Source == "discord")
            .ToListAsync();
    }
}
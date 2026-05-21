using Eevee.Sleep.Bot.Models;
using MongoDB.Driver;

namespace Eevee.Sleep.Bot.Controllers.Mongo;

public static class GitHubIssueSyncController {
    public static GitHubIssueSyncModel? FindByDiscordThreadId(ulong threadId) {
        return MongoConst.DiscordGithubIssueSyncCollection
            .Find(Builders<GitHubIssueSyncModel>.Filter.Where(x => x.DiscordThreadId == threadId))
            .FirstOrDefault();
    }

    public static Task Insert(GitHubIssueSyncModel model) {
        return MongoConst.DiscordGithubIssueSyncCollection.InsertOneAsync(model);
    }
}

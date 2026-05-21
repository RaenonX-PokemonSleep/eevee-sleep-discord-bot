using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models;

public class GitHubIssueSyncModel {
    [BsonId]
    public ObjectId Id { get; set; }

    public ulong DiscordThreadId { get; set; }

    public int GitHubIssueNumber { get; set; }

    public DateTime SyncedAtUtc { get; set; }
}

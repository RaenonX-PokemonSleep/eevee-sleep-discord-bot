using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Workers;

public class GitHubIssueSyncWorker(
    DiscordSocketClient client,
    ILogger<GitHubIssueSyncWorker> logger,
    IHostEnvironment env
) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
        logger.LogInformation("Starting GitHub issue sync worker.");
        cancellationToken.Register(
            () => logger.LogInformation("Stopping GitHub issue sync worker: Cancellation token received.")
        );

        while (!cancellationToken.IsCancellationRequested) {
            if (client.ConnectionState != ConnectionState.Connected) {
                logger.LogWarning(
                    "Skipped GitHub issue sync as Discord client is not ready yet ({ConnectionState})",
                    client.ConnectionState
                );
            } else if (env.IsDevelopment()) {
                logger.LogInformation(
                    "Skipping {MethodName}() - current environment is Development",
                    nameof(SyncAsync)
                );
            } else {
                try {
                    await SyncAsync();
                } catch (Exception e) when (e is not OperationCanceledException) {
                    logger.LogError(e, "An unexpected error occurred in GitHub issue sync worker.");
                }
            }

            await Task.Delay(GetCheckInterval(), cancellationToken);
        }
    }

    private async Task SyncAsync() {
        var feedbackChannelId = ConfigHelper.GetGithubIssueSyncFeedbackForumChannelId();
        var guild = client.GetCurrentWorkingGuild();
        var forumChannel = guild.GetForumChannel(feedbackChannelId);

        if (forumChannel is null) {
            logger.LogError("Forum channel #{ChannelId} not found.", feedbackChannelId);
            return;
        }

        var threads = await CollectAllThreadsAsync(forumChannel);
        logger.LogInformation("Found {Count} threads in feedback forum.", threads.Count);

        foreach (var thread in threads) {
            if (thread.Name.StartsWith("[D#")) {
                continue;
            }

            if (GitHubIssueSyncController.FindByDiscordThreadId(thread.Id) is not null) {
                continue;
            }

            await ProcessThreadAsync(guild, forumChannel, thread);
        }
    }

    private static async Task<List<IThreadChannel>> CollectAllThreadsAsync(SocketForumChannel forumChannel) {
        var all = new List<IThreadChannel>();

        var active = await forumChannel.GetActiveThreadsAsync();
        all.AddRange(active);

        DateTimeOffset? before = null;
        while (true) {
            var archived = await forumChannel.GetPublicArchivedThreadsAsync(100, before);
            if (archived.Count == 0) {
                break;
            }

            all.AddRange(archived);

            if (archived.Count < 100) {
                break;
            }

            // Archived threads are returned newest-first; use the oldest to paginate forward
            before = archived.Min(t => t.ArchiveTimestamp);
        }

        return all;
    }

    private async Task ProcessThreadAsync(SocketGuild guild, SocketForumChannel forumChannel, IThreadChannel thread) {
        logger.LogInformation("Syncing thread #{ThreadId} ({Title}) to GitHub.", thread.Id, thread.Name);

        try {
            // In Discord, a forum post's starter message has the same ID as the thread itself
            var firstMessage = await thread.GetMessageAsync(thread.Id);
            var tagNames = ResolveTagNames(forumChannel, thread.AppliedTags);
            var body = BuildIssueBody(guild.Id, thread, firstMessage);
            var originalTitle = thread.Name;

            var issueNumber = await GitHubApiClient.CreateIssueAsync(originalTitle, body, tagNames);

            await GitHubIssueSyncController.Insert(new GitHubIssueSyncModel {
                DiscordThreadId = thread.Id,
                GitHubIssueNumber = issueNumber,
                SyncedAtUtc = DateTime.UtcNow,
            });

            await thread.ModifyAsync((TextChannelProperties x) => x.Name = $"[D#{issueNumber}] {originalTitle}");

            logger.LogInformation(
                "Synced thread #{ThreadId} to GitHub issue #{IssueNumber}.",
                thread.Id,
                issueNumber
            );
        } catch (Exception e) {
            logger.LogError(e, "Failed to sync thread #{ThreadId} ({Title}).", thread.Id, thread.Name);
        }
    }

    private static IEnumerable<string> ResolveTagNames(
        SocketForumChannel forumChannel,
        IReadOnlyCollection<ulong> appliedTagIds
    ) {
        var tagLookup = forumChannel.Tags.ToDictionary(t => t.Id, t => t.Name);
        return appliedTagIds
            .Where(tagLookup.ContainsKey)
            .Select(id => tagLookup[id]);
    }

    private static TimeSpan GetCheckInterval() {
        return TimeSpan.FromMinutes(ConfigHelper.GetGithubIssueSyncCheckIntervalMinutes());
    }

    private static string BuildIssueBody(ulong guildId, IThreadChannel thread, IMessage? firstMessage) {
        var threadUrl = $"https://discord.com/channels/{guildId}/{thread.Id}";
        var poster = firstMessage?.Author.Username ?? "(unknown)";
        var content = firstMessage?.Content ?? "(no content)";

        return $"""
                **Discord Thread:** {threadUrl}
                **Posted by:** @{poster}

                ---

                {content}
                """;
    }
}

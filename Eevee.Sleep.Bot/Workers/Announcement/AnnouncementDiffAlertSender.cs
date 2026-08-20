using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo.Announcement;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

namespace Eevee.Sleep.Bot.Workers.Announcement;

public class AnnouncementDiffAlertSender<T>(
    AnnouncementHistoryController<T> historyController,
    DiscordSocketClient client,
    ILogger logger
) where T : AnnouncementMetaModel {
    public async Task SendAsync(
        T current,
        Func<T, T, bool> hasSameContent,
        Func<T, string> getContent,
        string source,
        string displayUrl
    ) {
        var previous = await historyController.FindPreviousVersion(current, hasSameContent);
        if (previous is null) {
            logger.LogWarning(
                "No previous {Source} announcement version found for {Language} #{Id}; skipping content diff",
                source,
                current.Language,
                current.AnnouncementId
            );
            return;
        }

        var messages = AnnouncementContentDiff.MakeDiscordMessages(getContent(previous), getContent(current));
        if (messages.Count == 0) return;

        var embed = DiscordMessageMakerForAnnouncement.MakeAnnouncementContentDiffMessage(
            source,
            displayUrl,
            previous,
            current
        );

        for (var index = 0; index < messages.Count; index++) {
            await client.SendMessageInAdminAlertChannel(
                message: messages[index],
                embed: index == 0 ? embed : null
            );
        }
    }
}
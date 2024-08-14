using Discord;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement.InGame;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;

namespace Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

public static class DiscordMessageMakerForAnnouncement {
    public static Embed MakeUpdateWatchingWorkerInitializeFailedMessage(DocumentProcessingException exception) {
        return new EmbedBuilder()
            .WithColor(Colors.Danger)
            .WithTitle("Failed to initialize in-game announcement watching worker")
            .WithDescription("Maximum number of data fetching attempts has been exceeded.")
            .AddField("Last exception message", exception.Message)
            .AddField("Last exception Context", exception.Context.ToJsonString())
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeDocumentProcessingErrorMessage(DocumentProcessingException exception) {
        return new EmbedBuilder()
            .WithColor(Colors.Danger)
            .WithTitle("Failed to retrieve in-game announcement")
            .WithDescription(exception.Message)
            .AddField("Context", exception.Context.ToJsonString())
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeOfficialSiteAnnouncementUpdateMessage(OfficialSiteAnnouncementDetailModel detail) {
        return new EmbedBuilder()
            .WithColor(Colors.Info)
            .WithTitle("Official Website Announcement Updated!")
            .AddField("Title", detail.Title)
            .AddField("Announcement ID", detail.AnnouncementId)
            .AddField("Url", detail.Url)
            .AddField("Updated", detail.OriginalUpdated)
            .AddField("Record Created", detail.RecordCreatedUtc)
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeInGameAnnouncementUpdateMessage(InGameAnnouncementDetailModel detail) {
        return new EmbedBuilder()
            .WithColor(Colors.Info)
            .WithTitle("In-game Announcement Updated!")
            .AddField("Title", detail.Title)
            .AddField("Announcement ID", detail.AnnouncementId)
            .AddField("Url", ConfigHelper.GetGameAnnouncementProxyUrl(detail.AnnouncementId))
            .AddField("Updated", detail.OriginalUpdatedUtc)
            .AddField("Record Created", detail.RecordCreatedUtc)
            .WithCurrentTimestamp()
            .Build();
    }
}
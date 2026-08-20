using Discord;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement;
using Eevee.Sleep.Bot.Models.Announcement.InGame;
using Eevee.Sleep.Bot.Models.Announcement.OfficialSite;

namespace Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

public static class DiscordMessageMakerForAnnouncement {
    public static Embed MakeDocumentProcessingErrorMessage(DocumentProcessingException exception) {
        return new EmbedBuilder()
            .WithColor(Colors.Danger)
            .WithTitle("Failed to retrieve in-game announcement")
            .WithDescription(exception.Message)
            .AddField("Context", exception.Context.ToJsonString())
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeOfficialSiteAnnouncementUpdateMessage(
        OfficialSiteAnnouncementDetailModel detail,
        bool isNew
    ) {
        return new EmbedBuilder()
            .WithColor(Colors.Info)
            .WithTitle(isNew ? "New Official Website Announcement!" : "Official Website Announcement Updated!")
            .AddField("Title", detail.Title)
            .AddField("Announcement ID", detail.AnnouncementId)
            .AddField("Url", detail.Url)
            .AddField("Updated", detail.OriginalUpdated)
            .AddField("Record Created", detail.RecordCreatedUtc)
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeInGameAnnouncementUpdateMessage(InGameAnnouncementDetailModel detail, bool isNew) {
        return new EmbedBuilder()
            .WithColor(Colors.Info)
            .WithTitle(isNew ? "New In-game Announcement!" : "In-game Announcement Updated!")
            .AddField("Title", detail.Title)
            .AddField("Announcement ID", detail.AnnouncementId)
            .AddField("Url", ConfigHelper.GetGameAnnouncementProxyUrl(detail.AnnouncementId))
            .AddField("Updated", detail.OriginalUpdatedUtc)
            .AddField("Record Created", detail.RecordCreatedUtc)
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeAnnouncementContentDiffMessage(
        string source,
        string displayUrl,
        AnnouncementMetaModel previous,
        AnnouncementMetaModel current
    ) {
        return new EmbedBuilder()
            .WithColor(Colors.Info)
            .WithTitle($"{source} Announcement Content Updated")
            .AddField("Title", current.Title)
            .AddField("Language", current.Language)
            .AddField("Announcement ID", current.AnnouncementId)
            .AddField("Url", displayUrl)
            .AddField("Previous Record", previous.RecordCreatedUtc)
            .AddField("Current Record", current.RecordCreatedUtc)
            .WithCurrentTimestamp()
            .Build();
    }
}
using Discord;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.InGameAnnouncement;

namespace Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

public static class DiscordMessageMakerForInGameAnnouncement {
    public static Embed MakeDocumentProcessingErrorMessage(DocumentProcessingException exception) {
        return new EmbedBuilder()
            .WithColor(Colors.Danger)
            .WithTitle("Failed to retrieve in-game announcement")
            .WithDescription(exception.Message)
            .AddField("Context", exception.Context.ToJsonString())
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeInGameAnnouncementUpdateMessage(InGameAnnouncementDetailModel detail) {
        var truncatedContent = detail.Content.Length > 1024 ? detail.Content[..1021] + "..." : detail.Content;

        return new EmbedBuilder()
            .WithColor(Colors.Info)
            .WithTitle("In-game Announcement Updated!")
            .AddField("Title", detail.Title)
            .AddField("Announcement ID", detail.AnnouncementId)
            .AddField("Url", detail.Url)
            .AddField("Updated", detail.Updated)
            .AddField("Record Created", detail.RecordCreatedUtc)
            .AddField("Content", truncatedContent)
            .WithCurrentTimestamp()
            .Build();
    }
}
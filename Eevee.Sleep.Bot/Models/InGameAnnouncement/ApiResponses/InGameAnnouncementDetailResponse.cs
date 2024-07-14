namespace Eevee.Sleep.Bot.Models.InGameAnnouncement.ApiResponses;

public record InGameAnnouncementDetailResponse: InGameAnnouncementResponse {
    public required string Content { get; init; }
}
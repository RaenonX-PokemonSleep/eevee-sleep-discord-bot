namespace Eevee.Sleep.Bot.Models.Announcement.ApiResponses;

public record AnnouncementDetailResponse: AnnouncementResponse {
    public required string Content { get; init; }
}
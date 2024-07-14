namespace Eevee.Sleep.Bot.Models.InGameAnnouncement.ApiResponses;

public record InGameAnnouncementResponse {
    public required string Title { get; init; }
    public required string OfficialLink { get; init; }
    public required InGameAnnouncementLastUpdatedResponse LastUpdated { get; init; }
}
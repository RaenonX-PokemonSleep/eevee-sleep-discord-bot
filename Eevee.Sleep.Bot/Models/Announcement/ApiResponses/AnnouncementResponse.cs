namespace Eevee.Sleep.Bot.Models.Announcement.ApiResponses;

public record AnnouncementResponse {
    public required string Title { get; init; }
    public required string OfficialLink { get; init; }
    public required AnnouncementLastUpdatedResponse LastUpdated { get; init; }
}
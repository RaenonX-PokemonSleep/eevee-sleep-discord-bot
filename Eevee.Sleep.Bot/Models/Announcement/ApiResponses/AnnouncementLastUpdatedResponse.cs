namespace Eevee.Sleep.Bot.Models.Announcement.ApiResponses;

public record AnnouncementLastUpdatedResponse {
    public DateTime Official { get; init; }
    public DateTime Server { get; init; }
}
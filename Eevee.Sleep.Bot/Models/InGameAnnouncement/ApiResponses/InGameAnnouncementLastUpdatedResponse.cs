namespace Eevee.Sleep.Bot.Models.InGameAnnouncement.ApiResponses;

public record InGameAnnouncementLastUpdatedResponse {
    public DateTime Official { get; init; }
    public DateTime Server { get; init; }
}
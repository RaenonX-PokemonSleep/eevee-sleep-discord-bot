using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Models.Announcement.ApiResponses;

public record AnnouncementLastUpdatedResponse {
    [UsedImplicitly]
    public DateTime Official { get; init; }

    [UsedImplicitly]
    public DateTime Server { get; init; }
}
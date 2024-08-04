using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Models.Announcement.ApiResponses;

public record AnnouncementResponse {
    [UsedImplicitly]
    public required string Title { get; init; }

    [UsedImplicitly]
    public required string OfficialLink { get; init; }

    [UsedImplicitly]
    public required AnnouncementLastUpdatedResponse LastUpdated { get; init; }
}
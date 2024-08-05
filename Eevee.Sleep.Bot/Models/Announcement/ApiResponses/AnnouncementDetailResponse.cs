using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Models.Announcement.ApiResponses;

public record AnnouncementDetailResponse : AnnouncementResponse {
    [UsedImplicitly]
    public required string Content { get; init; }
}
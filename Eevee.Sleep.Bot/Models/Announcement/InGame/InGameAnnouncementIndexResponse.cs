using System.Text.Json.Serialization;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement.InGame.Interfaces;
using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Models.Announcement.InGame;

public record InGameAnnouncementIndexResponse : IInGameAnnouncementResponse<InGameAnnouncementIndexModel> {
    [JsonPropertyName("id")]
    [UsedImplicitly]
    public required string Id { get; set; }

    [JsonPropertyName("title")]
    [UsedImplicitly]
    public required string Title { get; set; }

    [JsonPropertyName("hash")]
    [UsedImplicitly]
    public required string Hash { get; set; }

    [JsonPropertyName("category")]
    [UsedImplicitly]
    public required int Category { get; init; }

    [JsonPropertyName("imgUrl")]
    [UsedImplicitly]
    public required string ImageUrl { get; init; }

    [JsonPropertyName("url")]
    [UsedImplicitly]
    public required string Url { get; init; }

    [JsonPropertyName("stAt")]
    [UsedImplicitly]
    public required long StartAt { get; init; }

    [JsonPropertyName("endAt")]
    [UsedImplicitly]
    public required long EndAt { get; init; }

    [JsonPropertyName("createAt")]
    [UsedImplicitly]
    public required long CreateAt { get; init; }

    [JsonPropertyName("updateAt")]
    [UsedImplicitly]
    public required long UpdateAt { get; init; }

    public InGameAnnouncementIndexModel ToModel(string url, AnnouncementLanguage language) {
        return new InGameAnnouncementIndexModel {
            AnnouncementId = Id,
            Language = language,
            Title = Title,
            Url = Url,
            Hash = Hash,
            RecordCreatedUtc = DateTime.UtcNow,
            RecordUpdatedUtc = DateTime.UtcNow,
            OriginalUpdatedUtc = UpdateAt.ToDateTimeFromSecond(),
            OriginalCreatedUtc = CreateAt.ToDateTimeFromSecond(),
        };
    }
}
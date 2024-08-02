using System.Text.Json.Serialization;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.InGameAnnouncement.InGame.Interfaces;

namespace Eevee.Sleep.Bot.Models.InGameAnnouncement.InGame;

public record InGameAnnouncementIndexResponse : IInGameAnnouncementResponse<InGameAnnouncementIndexModel> {
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("hash")]
    public required string Hash { get; set; }

    [JsonPropertyName("category")]
    public required int Category { get; init; }

    [JsonPropertyName("imgUrl")]
    public required string ImgUrl { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("stAt")]
    public required long StAt { get; init; }

    [JsonPropertyName("endAt")]
    public required long EndAt { get; init; }

    [JsonPropertyName("createAt")]
    public required long CreateAt { get; init; }

    [JsonPropertyName("updateAt")]
    public required long UpdateAt { get; init; }

    [JsonPropertyName("dataUpdateAt")]
    public required long DataUpdateAt { get; init; }

    public InGameAnnouncementIndexModel ToModel(string url, InGameAnnoucementLanguage language) {
        return new InGameAnnouncementIndexModel {
            AnnouncementId = Id,
            Language = language,
            Title = Title,
            Url = Url,
            Hash = Hash,
            RecordCreatedUtc = DateTime.UtcNow,
            RecordUpdatedUtc = DateTime.UtcNow,
            OriginalUpdatedUtc = UpdateAt.ToDateTimeFromSecond(),
            OriginalCreatedUtc = CreateAt.ToDateTimeFromSecond()
        };
    }
}
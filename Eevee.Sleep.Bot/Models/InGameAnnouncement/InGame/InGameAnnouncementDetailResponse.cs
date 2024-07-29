using System.Text.Json.Serialization;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;

namespace Eevee.Sleep.Bot.Models.InGameAnnouncement.InGame;

public record InGameAnnouncementDetailResponse : IInGameAnnouncementResponse<InGameAnnouncementDetailModel> {
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("category")]
    public required int Category { get; init; }

    [JsonPropertyName("imgUrl")]
    public required string ImgUrl { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("createAt")]
    public required long CreateAt { get; init; }

    [JsonPropertyName("updateAt")]
    public required long UpdateAt { get; init; }

    [JsonPropertyName("dataUpdateAt")]
    public required long DataUpdateAt { get; init; }

    public InGameAnnouncementDetailModel ToModel(string url, InGameAnnoucementLanguage language) {
        return new InGameAnnouncementDetailModel {
            AnnouncementId = Id,
            Language = language,
            Title = Title,
            Url = url,
            Text = Text,
            RecordCreatedUtc = DateTime.UtcNow,
            OriginalUpdatedUtc = UpdateAt.ToDateTimeFromSecond(),
            OriginalCreatedUtc = CreateAt.ToDateTimeFromSecond()
        };
    }
}
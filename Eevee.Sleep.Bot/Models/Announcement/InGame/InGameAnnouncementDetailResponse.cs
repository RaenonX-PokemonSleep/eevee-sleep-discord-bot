using System.Text.Json.Serialization;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement.InGame.Interfaces;

namespace Eevee.Sleep.Bot.Models.Announcement.InGame;

public record InGameAnnouncementDetailResponse : IInGameAnnouncementResponse<InGameAnnouncementDetailModel> {
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("category")]
    public required int Category { get; init; }

    [JsonPropertyName("imgUrl")]
    public required string ImageUrl { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("createAt")]
    public required long CreateAt { get; init; }

    [JsonPropertyName("updateAt")]
    public required long UpdateAt { get; init; }

    [JsonPropertyName("dataUpdateAt")]
    public required long DataUpdateAt { get; init; }

    public InGameAnnouncementDetailModel ToModel(string url, AnnouncementLanguage language) {
        return new InGameAnnouncementDetailModel {
            AnnouncementId = Id,
            Language = language,
            Title = Title,
            Url = url,
            Text = Text,
            RecordCreatedUtc = DateTime.UtcNow,
            RecordUpdatedUtc = DateTime.UtcNow,
            OriginalUpdatedUtc = UpdateAt.ToDateTimeFromSecond(),
            OriginalCreatedUtc = CreateAt.ToDateTimeFromSecond()
        };
    }
}
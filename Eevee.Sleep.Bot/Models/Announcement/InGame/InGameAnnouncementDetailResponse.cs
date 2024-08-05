using System.Text.Json.Serialization;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.Announcement.InGame.Interfaces;
using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Models.Announcement.InGame;

public record InGameAnnouncementDetailResponse : IInGameAnnouncementResponse<InGameAnnouncementDetailModel> {
    [JsonPropertyName("id")]
    [UsedImplicitly]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    [UsedImplicitly]
    public required string Title { get; init; }

    [JsonPropertyName("category")]
    [UsedImplicitly]
    public required int Category { get; init; }

    [JsonPropertyName("imgUrl")]
    [UsedImplicitly]
    public required string ImageUrl { get; init; }

    [JsonPropertyName("text")]
    [UsedImplicitly]
    public required string Text { get; init; }

    [JsonPropertyName("createAt")]
    [UsedImplicitly]
    public required long CreateAt { get; init; }

    [JsonPropertyName("updateAt")]
    [UsedImplicitly]
    public required long UpdateAt { get; init; }

    [JsonPropertyName("dataUpdateAt")]
    [UsedImplicitly]
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
            OriginalCreatedUtc = CreateAt.ToDateTimeFromSecond(),
        };
    }
}
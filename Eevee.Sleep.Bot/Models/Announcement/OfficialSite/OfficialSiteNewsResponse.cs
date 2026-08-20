using System.Text.Json.Serialization;
using AngleSharp.Html.Parser;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;

namespace Eevee.Sleep.Bot.Models.Announcement.OfficialSite;

public record OfficialSiteNewsResponse {
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("slug")]
    public required string Slug { get; init; }

    [JsonPropertyName("link")]
    public required string Link { get; init; }

    [JsonPropertyName("date")]
    public required DateTime Date { get; init; }

    [JsonPropertyName("modified_gmt")]
    public required DateTime ModifiedGmt { get; init; }

    [JsonPropertyName("title")]
    public required RenderedText Title { get; init; }

    [JsonPropertyName("content")]
    public required RenderedText Content { get; init; }

    public (OfficialSiteAnnouncementIndexModel Index, OfficialSiteAnnouncementDetailModel Detail) ToModels(
        AnnouncementLanguage language
    ) {
        var now = DateTime.UtcNow;
        var title = NormalizeRenderedText(Title.Rendered);
        var content = NormalizeRenderedHtml(Content.Rendered);

        return (
            new OfficialSiteAnnouncementIndexModel {
                AnnouncementId = Slug,
                Title = title,
                Language = language,
                Url = Link,
                Hash = $"{title}{Link}".ToSha256Hash(),
                RecordCreatedUtc = now,
                RecordUpdatedUtc = now,
            },
            new OfficialSiteAnnouncementDetailModel {
                AnnouncementId = Slug,
                Title = title,
                Language = language,
                Url = Link,
                Content = content,
                ContentHash = content.ToSha256Hash(),
                OriginalUpdated = DateOnly.FromDateTime(Date),
                RecordCreatedUtc = now,
                RecordUpdatedUtc = now,
            }
        );
    }

    public DateTime GetModifiedUtc() {
        return DateTime.SpecifyKind(ModifiedGmt, DateTimeKind.Utc);
    }

    private static string NormalizeRenderedText(string html) {
        return CreateContainer(html).TextContent.Trim();
    }

    private static string NormalizeRenderedHtml(string html) {
        return CreateContainer(html).InnerHtml.Trim();
    }

    private static AngleSharp.Dom.IElement CreateContainer(string html) {
        var document = new HtmlParser().ParseDocument(string.Empty);
        var container = document.CreateElement("div");
        container.InnerHtml = html;
        return container;
    }

    public record RenderedText {
        [JsonPropertyName("rendered")]
        public required string Rendered { get; init; }
    }
}

using Eevee.Sleep.Bot.Enums;

namespace Eevee.Sleep.Bot.Models.Announcement.OfficialSite;

public record OfficialSiteNewsEntry {
    public required string Url { get; init; }

    public required AnnouncementLanguage Language { get; init; }

    public required DateTime LastModifiedUtc { get; init; }

    public string Slug => new Uri(Url).Segments[^1].TrimEnd('/');

    public bool NeedsFetch(IReadOnlyDictionary<string, DateTime> savedModifiedTimes) {
        return !savedModifiedTimes.TryGetValue(Url, out var saved) || saved != LastModifiedUtc;
    }
}

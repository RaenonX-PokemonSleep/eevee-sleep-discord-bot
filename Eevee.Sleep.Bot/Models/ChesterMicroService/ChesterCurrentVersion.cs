using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models.ChesterMicroService;

[BsonIgnoreExtraElements]
public record ChesterCurrentVersion {
    // Used for announcement
    [UsedImplicitly]
    public required uint InV { get; set; }

    [UsedImplicitly]
    public required string MsV { get; set; }

    [UsedImplicitly]
    public required string AsRv { get; set; }

    // Possibly master data version
    [UsedImplicitly]
    public required uint MdV { get; set; }

    // Used for webview templates
    // Webview Version
    [UsedImplicitly]
    public required uint WvV { get; set; }

    public bool IsVersionUpdated(ChesterCurrentVersion? old) {
        if (old is null) {
            return true;
        }

        return old.MsV != MsV || old.AsRv != AsRv || old.MdV != MdV || old.WvV != WvV;
    }
}
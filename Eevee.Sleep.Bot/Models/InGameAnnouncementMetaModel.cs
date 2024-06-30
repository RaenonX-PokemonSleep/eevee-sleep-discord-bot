using Eevee.Sleep.Bot.Enums;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Eevee.Sleep.Bot.Models;

[BsonIgnoreExtraElements]
public record InGameAnnouncementMetaModel {
    [UsedImplicitly]
    public required string AnnouncementId { get; init; }

    [UsedImplicitly]
    public required string Title { get; init; }

    [UsedImplicitly]
    [BsonRepresentation(BsonType.String)]
    public required InGameAnnoucementLanguages Language { get; init; }

    [UsedImplicitly]
    public required string Url { get; init; }
}
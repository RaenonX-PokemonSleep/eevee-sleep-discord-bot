using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Eevee.Sleep.Bot.Models.CustomSerializers;

public class DateOnlySerializer : SerializerBase<DateOnly> {
    public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
        var date = context.Reader.ReadDateTime();
        return DateOnly.FromDateTime(new DateTime(date));
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value) {
        // Serialize in milliseconds of UTC
        var dateTime = value.ToDateTime(new TimeOnly(0), DateTimeKind.Utc);
        var millisecondsSinceEpoch = (long)(dateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        context.Writer.WriteDateTime(millisecondsSinceEpoch);
    }
}
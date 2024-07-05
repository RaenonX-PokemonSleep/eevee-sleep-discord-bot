using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Eevee.Sleep.Bot.Models.CustomSerializers;

public class DateOnlySerializer : SerializerBase<DateOnly> {
    public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
        var millisecondsSinceEpoch = context.Reader.ReadDateTime();
        // Convert milliseconds to ticks (1 tick = 100 nanoseconds)
        var ticks = millisecondsSinceEpoch * 10000;
        var date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(ticks);
        return DateOnly.FromDateTime(date);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value) {
        // Serialize in milliseconds of UTC
        var dateTime = value.ToDateTime(new TimeOnly(0), DateTimeKind.Utc);
        var millisecondsSinceEpoch = (long)(dateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        context.Writer.WriteDateTime(millisecondsSinceEpoch);
    }
}
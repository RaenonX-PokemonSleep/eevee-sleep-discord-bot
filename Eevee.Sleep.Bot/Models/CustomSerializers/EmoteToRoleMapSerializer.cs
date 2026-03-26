using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Eevee.Sleep.Bot.Models.CustomSerializers;

public class EmoteToRoleMapSerializer : SerializerBase<Dictionary<string, ulong>> {
    public override Dictionary<string, ulong> Deserialize(
        BsonDeserializationContext context,
        BsonDeserializationArgs args
    ) {
        var result = new Dictionary<string, ulong>();
        context.Reader.ReadStartDocument();

        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument) {
            var key = context.Reader.ReadName(Utf8NameDecoder.Instance);
            var value = (ulong)context.Reader.ReadInt64();
            result[key] = value;
        }

        context.Reader.ReadEndDocument();
        return result;
    }

    public override void Serialize(
        BsonSerializationContext context,
        BsonSerializationArgs args,
        Dictionary<string, ulong> value
    ) {
        context.Writer.WriteStartDocument();

        foreach (var (key, roleId) in value) {
            context.Writer.WriteName(key);
            context.Writer.WriteInt64((long)roleId);
        }

        context.Writer.WriteEndDocument();
    }
}

using System.Text.Encodings.Web;
using System.Text.Json;

namespace Eevee.Sleep.Bot.Extensions;

public static class DictionaryExtensions {
    private static readonly JsonSerializerOptions Options = new() {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };
    
    public static string ToJsonString<K, V>(this IDictionary<K, V?> dict) {
        return JsonSerializer.Serialize(
            value: dict,
            options: Options
        );
    }
}
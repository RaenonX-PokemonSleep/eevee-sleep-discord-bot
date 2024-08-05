using System.Text.Encodings.Web;
using System.Text.Json;

namespace Eevee.Sleep.Bot.Extensions;

public static class DictionaryExtensions {
    private static readonly JsonSerializerOptions Options = new() {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    public static string ToJsonString<TK, TV>(this IDictionary<TK, TV?> dict) {
        return JsonSerializer.Serialize(dict, Options);
    }
}
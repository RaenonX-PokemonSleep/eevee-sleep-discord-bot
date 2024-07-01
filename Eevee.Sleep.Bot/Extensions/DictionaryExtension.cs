using System.Text.Json;

namespace Eevee.Sleep.Bot.Extensions;

public static class DictionaryExtensions {
    public static string ToJsonString<K, V>(this IDictionary<K, V?> dict) {
        return JsonSerializer.Serialize(dict);
    }
}
namespace Eevee.Sleep.Bot.Extensions;

public static class DictionaryExtensions {
    public static string GetString<K,V>(this IDictionary<K,V?> dict) {
        var items = dict.Select(kvp => kvp.ToString());
        dict.Select(k => k.Value?.ToString()).ToList().ForEach(Console.WriteLine);
        return string.Join(", ", items);
    }
}
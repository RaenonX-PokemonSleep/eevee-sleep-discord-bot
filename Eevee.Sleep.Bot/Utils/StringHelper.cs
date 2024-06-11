namespace Eevee.Sleep.Bot.Utils;

public static class StringHelper {
    public static string MergeLines(IEnumerable<string> lines) {
        return string.Join("\n", lines);
    }
}
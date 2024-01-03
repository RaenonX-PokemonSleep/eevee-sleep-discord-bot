namespace Eevee.Sleep.Bot.Extensions;

public static class LotteryExtensions {
    private static readonly Random Random = new();

    public static IEnumerable<T> GetRandomElements<T>(this IList<T> list, int count) {
        var result = new HashSet<T>();

        while (result.Count < count) {
            var idx = Random.Next(0, list.Count);
            result.Add(list[idx]);
        }

        return result;
    }
}
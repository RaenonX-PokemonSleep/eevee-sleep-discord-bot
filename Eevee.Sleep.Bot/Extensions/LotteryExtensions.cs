namespace Eevee.Sleep.Bot.Extensions;

public static class LotteryExtensions {
    private static readonly Random Random = new();

    public static IEnumerable<T> GetRandomElements<T>(this IList<T> list, int count) {
        var rolledCount = 0;

        foreach (var element in list) {
            var result = Random.Next(list.Count - rolledCount);

            if (result >= count - rolledCount) {
                continue;
            }

            yield return element;
            rolledCount += 1;
        }
    }
}
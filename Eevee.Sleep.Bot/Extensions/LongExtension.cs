namespace Eevee.Sleep.Bot.Extensions;

public static class LongExtension {
    public static DateTime ToDateTimeFromSecond(this long value) {
        return DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime;
    }

    public static ulong ToUlong(this long value) {
        if (value < 0) {
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative.");
        }

        return (ulong)value;
    }
}
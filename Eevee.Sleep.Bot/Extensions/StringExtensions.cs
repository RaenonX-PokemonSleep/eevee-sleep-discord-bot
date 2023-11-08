using Eevee.Sleep.Bot.Enums;

namespace Eevee.Sleep.Bot.Extensions;

public static class StringExtensions {
    private static T? EnumToString<T>(this string value) where T : struct, Enum {
        var converted = Enum.TryParse(value, out T outSummaryKey);

        if (!converted) {
            return null;
        }

        return outSummaryKey;
    }

    public static ModalId? ToModalId(this string value) {
        return EnumToString<ModalId>(value);
    }

    public static ModalFieldId? ToModalFieldId(this string value) {
        return EnumToString<ModalFieldId>(value);
    }
}
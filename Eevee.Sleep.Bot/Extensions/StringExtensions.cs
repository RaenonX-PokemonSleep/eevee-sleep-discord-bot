using System.Security.Cryptography;
using System.Text;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Utils;

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

    public static string MergeToSameLine(this IEnumerable<string> lines) {
        return string.Join(" / ", lines);
    }

    public static string MergeToSameLine(this IEnumerable<ulong> lines) {
        return string.Join(" / ", lines);
    }

    public static string MergeLines(this IEnumerable<string> lines) {
        return StringHelper.MergeLines(lines);
    }

    public static string ToSha256Hash(this string value) {
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        var builder = new StringBuilder();

        foreach (byte b in hashBytes) {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }
}
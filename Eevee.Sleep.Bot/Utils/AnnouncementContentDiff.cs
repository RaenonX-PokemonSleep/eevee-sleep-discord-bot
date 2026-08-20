using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using DiffPlex.Renderer;

namespace Eevee.Sleep.Bot.Utils;

public static partial class AnnouncementContentDiff {
    private const int MaxReadableLineLength = 160;
    private const int MaxDiscordMessageLength = 2000;
    private const string CodeBlockStart = "```diff\n";
    private const string CodeBlockEnd = "\n```";

    private static readonly HashSet<string> BlockTags = [
        "ADDRESS", "ARTICLE", "ASIDE", "BLOCKQUOTE", "DIV", "DL", "FIELDSET", "FIGCAPTION",
        "FIGURE", "FOOTER", "H1", "H2", "H3", "H4", "H5", "H6", "HEADER", "HR", "LI",
        "MAIN", "NAV", "OL", "P", "PRE", "SECTION", "TABLE", "TBODY", "TD", "TFOOT", "TH",
        "THEAD", "TR", "UL",
    ];

    public static IReadOnlyList<string> MakeDiscordMessages(string previousHtml, string currentHtml) {
        var diff = UnidiffRenderer.GenerateUnidiff(
            NormalizeHtml(previousHtml),
            NormalizeHtml(currentHtml),
            "previous",
            "current",
            ignoreWhitespace: false,
            ignoreCase: false,
            contextLines: 3
        );

        if (string.IsNullOrWhiteSpace(diff)) return [];
        return SplitForDiscord(diff.Replace("```", "``\u200B`"));
    }

    private static string NormalizeHtml(string html) {
        var document = new HtmlParser().ParseDocument($"<body>{html}</body>");
        var content = new StringBuilder();
        AppendReadableText(document.Body!, content);

        var lines = content.ToString()
            .Replace('\u00A0', ' ')
            .Split('\n')
            .Select(line => InlineWhitespace().Replace(line, " ").Trim())
            .Where(line => line.Length > 0)
            .SelectMany(WrapLine);

        return string.Join('\n', lines);
    }

    private static void AppendReadableText(INode node, StringBuilder content) {
        if (node is IText text) {
            content.Append(text.Data);
            return;
        }

        if (node is IElement { TagName: "BR" }) {
            AppendLineBreak(content);
            return;
        }

        if (node is IElement { TagName: "SCRIPT" or "STYLE" }) {
            return;
        }

        var isBlock = node is IElement element && BlockTags.Contains(element.TagName);
        if (isBlock) AppendLineBreak(content);
        foreach (var child in node.ChildNodes) AppendReadableText(child, content);
        if (isBlock) AppendLineBreak(content);
    }

    private static void AppendLineBreak(StringBuilder content) {
        if (content.Length > 0 && content[^1] != '\n') content.AppendLine();
    }

    private static IEnumerable<string> WrapLine(string line) {
        while (line.Length > MaxReadableLineLength) {
            var splitAt = line.LastIndexOf(' ', MaxReadableLineLength);
            if (splitAt <= 0) splitAt = MaxReadableLineLength;
            yield return line[..splitAt].TrimEnd();
            line = line[splitAt..].TrimStart();
        }

        yield return line;
    }

    private static IReadOnlyList<string> SplitForDiscord(string diff) {
        var payloadLimit = MaxDiscordMessageLength - CodeBlockStart.Length - CodeBlockEnd.Length;
        var messages = new List<string>();
        var chunk = new StringBuilder();

        foreach (var line in diff.Replace("\r\n", "\n").Split('\n')) {
            if (chunk.Length > 0 && chunk.Length + line.Length + 1 > payloadLimit) {
                messages.Add($"{CodeBlockStart}{chunk.ToString().TrimEnd('\n')}{CodeBlockEnd}");
                chunk.Clear();
            }

            chunk.Append(line).Append('\n');
        }

        if (chunk.Length > 0) messages.Add($"{CodeBlockStart}{chunk.ToString().TrimEnd('\n')}{CodeBlockEnd}");
        return messages;
    }

    [GeneratedRegex(@"[^\S\r\n]+")]
    private static partial Regex InlineWhitespace();
}
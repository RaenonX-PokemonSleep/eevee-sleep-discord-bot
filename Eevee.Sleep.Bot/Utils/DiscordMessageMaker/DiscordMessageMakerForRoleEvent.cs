using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Models;

namespace Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

public static class DiscordMessageMakerForRoleEvent {
    private static readonly ulong[] LanguageRoleIds = [
        1140831457737199686,
        1140831505271246900,
        1148526383258157128,
    ];

    private const string TemplateAll = """
                                       <@&1140831457737199686> 
                                       ### For a limited time, Pokemon permanent roles are available to celebrate their arrivals!
                                       🔸 Available to grab until <t:END_EPOCH_SEC_UTC:F>, react to the message with their corresponding emotes to get it.
                                       🔸 Everyone is able to get this role.
                                       💡 Displayed role can be changed through the `/role` command with <@1172724671792295936> , all previously grabbed roles are still saved.
                                       ```
                                        
                                       ```
                                       <@&1140831505271246900> 
                                       ### <:iconfull:1170404181358678238> 寶可夢永久身份組
                                       配合 Pokemon Sleep 新登場的寶可夢，所有本伺服器的玩家可以領取下列的身份組，此身份組可以永久保留！
                                       ### 🔸領取期限
                                       開始：即日起
                                       結束：<t:END_EPOCH_SEC_UTC:f>
                                       ### 🔸領取方法
                                       對本公告點擊下列對應寶可夢的反應即可獲得
                                       ### 💡隱藏 / 顯示 身份組
                                       身份組可以透過 <@1172724671792295936> 的 `/role` 指令更換顯示，曾獲得的身份組不會因此消失。
                                       ```
                                        
                                       ```
                                       <@&1148526383258157128>
                                       ### ポケモン実装記念（全員配布）
                                       🔸<t:END_EPOCH_SEC_UTC:F>までの期間限定で、下記のポケモン絵文字でリアクションすると取得できます。
                                       💡表示アイコンを変えたい場合は <@1172724671792295936> の "`/role`"コマンドを使用して変更可能です。
                                       ------------------------------
                                       Designer: <DESIGNER>
                                       
                                       <ROLE_LINES>
                                       """;

    private const string TemplateSubscribers = """
                                               <@&1140831457737199686> 
                                               ## <:iconfull:1170404181358678238> SUBSCRIBERS ONLY
                                               ### For a limited time, Pokemon permanent roles are available to celebrate their arrivals!
                                               🔹Available to grab until <t:END_EPOCH_SEC_UTC:F>, react to the message with their corresponding emotes to get it.
                                               🔹Subscribers can grab all roles.
                                               🔹For ⚠️GitHub / Stripe / Afdian subscribers not having the subscriber roles yet, please contact <@503484431437398016> directly with your website user ID (starting with 6).
                                               💡Displayed role can be changed through the `/role` command with <@1172724671792295936> , all previously grabbed roles are still saved.
                                               ```
                                                
                                               ```
                                               <@&1140831505271246900> 
                                               ## <:iconfull:1170404181358678238> 寶可夢訂閱者特別身份組
                                               訂閱者既可獲得全員可以獲得的寶可夢身分組，還可獲得色違版的寶可夢身份組，此身份組可以永久保留！
                                               ### 🔹 領取期限
                                               開始：即日起
                                               結束：<t:END_EPOCH_SEC_UTC:f>
                                               ### 🔹 領取方法
                                               __訂閱者__對本公告點擊下列對應寶可夢的反應即可獲得
                                               ### ⚠️GitHub / Stripe / Afdian 訂閱者
                                               如果 ⚠️GitHub / Stripe / Afdian 訂閱者遇到身份組未成功更新的問題，請私訊 <@503484431437398016> 處理
                                               ### 💡隱藏 / 顯示 身份組
                                               身份組可以透過 <@1172724671792295936> 的 `/role` 指令更換顯示，曾獲得的身份組不會因此消失。
                                               ```
                                                
                                               ```
                                               <@&1148526383258157128> 
                                               ## ポケモン実装記念（サブスク会員限定配布）
                                               🔹 <t:END_EPOCH_SEC_UTC:F>までの期間限定で、下記のポケモン絵文字でリアクションすると取得できます。
                                               🔹 全てのサブスク会員が取得可能です。
                                               🔹 GitHub / Stripe / 愛發電 経由のサブスク会員の方で、サーバー内でサブスク会員専用ロール付与が済んでいない方は、先に <@503484431437398016> 宛に当サイトのユーザーID（6から始まるはずです）を添えてご一報ください。
                                               💡表示アイコンを変えたい場合は"`/role`"コマンドを使用して変更可能です。
                                               ------------------------------
                                               Designer: <DESIGNER>
                                               
                                               <ROLE_LINES>
                                               """;

    private const string TemplateLast = """
                                       For the free role icons, please go 1 message above the subscribers-only message - MESSAGE_LINK . 

                                       免費身份組請點擊訂閱戶專用訊息的上一則訊息的反應 - MESSAGE_LINK  。

                                       無料限定ロールアイコンはサブスク特典のメッセージより1つ上のメッセージです - MESSAGE_LINK  。
                                       """;

    private static string BuildRoleLine(RoleEventEntry entry, GuildEmote emote, IRole role) {
        var nameTriple = $"{entry.NameEn} / {entry.NameZh} / {entry.NameJp}";
        return $"{nameTriple}: {emote} {MentionUtils.MentionRole(role.Id)}";
    }

    private static string BuildRoleLines(
        IEnumerable<(RoleEventEntry Entry, GuildEmote Emote, IRole Role)> items
    ) {
        return string.Join("\n", items.Select(x => BuildRoleLine(x.Entry, x.Emote, x.Role)));
    }

    private static string StripLanguageRoleMentions(string content) {
        var lines = content.Split('\n');
        return string.Join(
            "\n",
            lines.Where(line =>
                !LanguageRoleIds.Any(roleId => line.TrimStart().StartsWith($"<@&{roleId}>"))
            )
        );
    }

    public static string BuildMessageAll(
        long expiryEpoch,
        string designer,
        IEnumerable<(RoleEventEntry Entry, GuildEmote Emote, IRole Role)> freeItems,
        bool omitLangRoles
    ) {
        var roleLines = BuildRoleLines(freeItems);
        var content = TemplateAll
            .Replace("END_EPOCH_SEC_UTC", expiryEpoch.ToString())
            .Replace("<DESIGNER>", designer)
            .Replace("<ROLE_LINES>", roleLines);

        return omitLangRoles ? StripLanguageRoleMentions(content) : content;
    }

    public static string BuildMessageSubscribers(
        long expiryEpoch,
        string designer,
        IEnumerable<(RoleEventEntry Entry, GuildEmote Emote, IRole Role)> subscriberItems,
        bool omitLangRoles
    ) {
        var roleLines = BuildRoleLines(subscriberItems);
        var content = TemplateSubscribers
            .Replace("END_EPOCH_SEC_UTC", expiryEpoch.ToString())
            .Replace("<DESIGNER>", designer)
            .Replace("<ROLE_LINES>", roleLines);

        return omitLangRoles ? StripLanguageRoleMentions(content) : content;
    }

    public static string BuildMessageLast(string messageLink) {
        return TemplateLast.Replace("MESSAGE_LINK", messageLink);
    }

    public static List<string> BuildEmoteCyclingMessages(GuildEmote[] allEmotes) {
        var messages = new List<string>();
        var cycleCount = Math.Max(1, (int)Math.Ceiling(50.0 / allEmotes.Length));

        var emoteStrings = Enumerable.Range(0, cycleCount)
            .SelectMany(_ => allEmotes.Select(e => e.ToString()))
            .ToList();

        var current = "";
        foreach (var emoteStr in emoteStrings) {
            var candidate = current.Length == 0 ? emoteStr : $"{current} {emoteStr}";

            if (candidate.Length > 2000) {
                messages.Add(current);
                current = emoteStr;
            } else {
                current = candidate;
            }
        }

        if (current.Length > 0) {
            messages.Add(current);
        }

        return messages;
    }

    public static Embed BuildPreviewEmbed(
        string messageAllPreview,
        string messageSubscribersPreview,
        int emoteCount,
        int roleCount,
        long expiryEpoch
    ) {
        return new EmbedBuilder()
            .WithTitle("Role Event Preview")
            .WithColor(Color.Gold)
            .AddField("Everyone", Truncate(messageAllPreview, 1024))
            .AddField("Subscribers Only", Truncate(messageSubscribersPreview, 1024))
            .AddField("Emotes to create", emoteCount.ToString(), true)
            .AddField("Roles to create", roleCount.ToString(), true)
            .AddField("Self-destruct", $"<t:{expiryEpoch}:F>", true)
            .Build();
    }

    public static Embed BuildSummaryEmbed(
        int emoteCount,
        int roleCount,
        string[] messageLinks,
        long expiryEpoch
    ) {
        return new EmbedBuilder()
            .WithTitle("Role Event Complete")
            .WithColor(Color.Green)
            .AddField("Emotes created", emoteCount.ToString(), true)
            .AddField("Roles created & tracked", roleCount.ToString(), true)
            .AddField("Self-destruct", $"<t:{expiryEpoch}:F>", true)
            .AddField("Messages", string.Join("\n", messageLinks.Select((l, i) => $"Message {i + 1}: {l}")))
            .Build();
    }

    private static string Truncate(string text, int maxLength) {
        const string suffix = "\n... (truncated)";
        return text.Length <= maxLength ? text : text[..(maxLength - suffix.Length)] + suffix;
    }
}

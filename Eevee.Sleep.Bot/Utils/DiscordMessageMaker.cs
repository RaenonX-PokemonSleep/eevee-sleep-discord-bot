using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Enums;
using IResult = Discord.Interactions.IResult;

namespace Eevee.Sleep.Bot.Utils;

public static class DiscordMessageMaker {
    public static Embed MakeError(IResult result) {
        return new EmbedBuilder()
            .WithColor(Colors.Danger)
            .WithTitle($"Error - {result.Error}")
            .WithDescription(result.ErrorReason)
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeErrorFromLog(LogMessage message) {
        return new EmbedBuilder()
            .WithColor(Colors.Warning)
            .WithTitle($"{message.Source}: {message.Message}")
            .WithDescription($"```{message.Exception}```")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed[] MakeActivationNote() {
        return [
            new EmbedBuilder()
                .WithColor(Colors.Danger)
                .WithTitle("中文")
                .WithDescription(
                    """
                    感謝支持！登入網站後，點擊連結即可啟用。
                    - 點擊連結後，如果有跳轉回首頁則代表已啟用。
                    - 連結只限點擊一次。重複點擊將會出現 "Activation Failed"。
                    - 如果無法啟用，請聯絡 <@503484431437398016> 協助處理。
                    - 如果付費內容在訂閱期間忽然失效的話，也請聯絡 <@503484431437398016>。
                    """
                )
                .Build(),
            new EmbedBuilder()
                .WithColor(Colors.Info)
                .WithTitle("English")
                .WithDescription(
                    """
                    Thanks for your support! Click on the link below to activate after logging in to the website.
                    - If it redirects to the homepage the link has been activated.
                    - The link is only valid for the first click. Subsequent clicks will show "Activation Failed."
                    - If the link keeps failing, please contact <@503484431437398016>.
                    - If the paid content suddenly becomes inaccessible please contact <@503484431437398016>.
                    """
                )
                .Build(),
            new EmbedBuilder()
                .WithColor(Colors.Warning)
                .WithTitle("日本語")
                .WithDescription(
                    """
                    ご支援ありがとうございます！ サイトでログインしてから、下記のリンクを開いてアクティベートしてください。
                    - サイトのトップページにリダイレクトされていれば、アクティベートは成功です。
                    - リンクは1回限り有効です。再び開いても"Activation Failed"と表示されます。
                    - アクティベートが何度もうまくいかない場合は、<@503484431437398016> までご連絡ください。
                    - サブスク限定コンテンツが突然失効してしまった場合も、同様に <@503484431437398016> までご連絡ください。
                    """
                )
                .Build(),
            new EmbedBuilder()
                .WithColor(Colors.Success)
                .WithTitle("한국어")
                .WithDescription(
                    """
                    여러분의 도움에 감사를 표합니다. 사이트 로그인 후 아래 링크를 클릭하여 활성화하세요.
                    - 홈페이지로 리디렉션된다면 광고 제거가 활성화된 것입니다.
                    - 이 링크는 처음 클릭했을 때만 유효합니다. 이후 클릭 시에는 "Activation Failed" 라고 표시됩니다.
                    - 링크를 계속 사용할 수 없다면 <@503484431437398016> 로 연락해주세요.
                    - 갑자기 구독으로 인한 혜택을 받을 수 없다면 <@503484431437398016> 로 연락해주세요.
                    """
                )
                .Build()
        ];
    }

    public static Embed MakeUserDataNotCached(ulong userId, IUser updated) {
        return new EmbedBuilder()
            .WithColor(Colors.Warning)
            .WithAuthor(updated)
            .WithTitle("Member Update - User data not cached")
            .AddField("User", updated.Mention)
            .WithFooter($"ID: {userId}")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeUserSubscribed(IUser user, IEnumerable<ulong> roleIds) {
        return MakeUserSubscribed(user, roleIds, Colors.Success);
    }

    public static Embed MakeUserSubscribed(IUser user, IEnumerable<ulong> roleIds, Color color) {
        return new EmbedBuilder()
            .WithColor(color)
            .WithAuthor(user)
            .WithTitle("Member Subscribed")
            .AddField("User", user.Mention)
            .AddField("Role", string.Join(" / ", roleIds.Select(MentionUtils.MentionRole)))
            .WithFooter($"ID: {user.Id}")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeUserUnsubscribed(IUser user, IEnumerable<ulong>? roleIds = null) {
        var builder = new EmbedBuilder()
            .WithColor(Colors.Danger)
            .WithAuthor(user)
            .WithTitle("Member Unsubscribed")
            .AddField("User", user.Mention)
            .WithFooter($"ID: {user.Id}")
            .WithCurrentTimestamp();

        if (roleIds is not null) {
            builder = builder.AddField("Role", string.Join(" / ", roleIds.Select(MentionUtils.MentionRole)));
        }

        return builder.Build();
    }

    public static Embed MakeLotteryResult(ulong roleId, int count, IEnumerable<SocketGuildUser> members) {
        return new EmbedBuilder()
            .WithColor(Colors.Info)
            .WithDescription(string.Join("\n", [
                $"# {MentionUtils.MentionRole(roleId)} x {count}",
                string.Join("\n", members.Select(x => $"- {MentionUtils.MentionUser(x.Id)}"))
            ]))
            .WithCurrentTimestamp()
            .Build();
    }
}
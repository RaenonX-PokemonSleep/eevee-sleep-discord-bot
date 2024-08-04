using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;

namespace Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

public static class DiscordMessageMakerForLottery {
    public static Embed MakeLotteryResult(ulong roleId, int count, IEnumerable<SocketGuildUser> members) {
        return new EmbedBuilder()
            .WithColor(Colors.Info)
            .WithDescription(
                StringHelper.MergeLines(
                    [
                        $"# {MentionUtils.MentionRole(roleId)} x {count}",
                        members.Select(x => $"- {MentionUtils.MentionUser(x.Id)}").MergeLines(),
                    ]
                )
            )
            .WithCurrentTimestamp()
            .Build();
    }
}
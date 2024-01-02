using Discord;
using Discord.Interactions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Utils;
using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Modules.SlashCommands;

[Group("lottery", "Commands for doing lottery.")]
public class LotterySlashModule : InteractionModuleBase<SocketInteractionContext> {
    [SlashCommand("role", "Do a role-based member lottery.")]
    [UsedImplicitly]
    public Task RoleBasedLotteryAsync(
        [Summary(name: "role", description: "Lottery target role.")] string roleExpression,
        [Summary(name: "count", description: "Count of members to pull.")] int count
    ) {
        try {
            var targetRoleId = MentionUtils.ParseRole(roleExpression);

            var result = Context.Client.GetGuild(ConfigHelper.GetDiscordWorkingGuild())
                .Roles
                .Single(x => x.Id == targetRoleId)
                .Members
                .ToArray()
                .GetRandomElements(count)
                .ToArray();

            return RespondAsync(embed: DiscordMessageMaker.MakeLotteryResult(targetRoleId, count, result));
        } catch (ArgumentException e) {
            return RespondAsync(e.Message, ephemeral: true);
        }
    }
}
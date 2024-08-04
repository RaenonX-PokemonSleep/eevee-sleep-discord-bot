using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Utils;
using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Modules.SlashCommands;

[Group("export", "Commands for exporting stuff.")]
public class ExportSlashModule : InteractionModuleBase<SocketInteractionContext> {
    [SlashCommand("role", "Export the list of users who have the given role.")]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [UsedImplicitly]
    public Task ExportRoleAsync(
        [Summary("role", "Export target role.")] SocketRole role
    ) {
        try {
            var targetRoleId = role.Id;

            var result = Context.Client.GetGuild(ConfigHelper.GetDiscordWorkingGuild())
                .Roles
                .Single(x => x.Id == targetRoleId)
                .Members
                .ToArray();

            return RespondWithFileAsync(
                new MemoryStream(
                    Encoding.UTF8.GetBytes(
                        string.Join(
                            "\n",
                            string.Join(",", "ID", "Username", "Display Name"),
                            result.Select(x => string.Join(",", x.Id, x.Username, x.DisplayName)).MergeLines()
                        )
                    )
                ),
                $"{targetRoleId}.csv",
                $"{result.Length} results."
            );
        } catch (ArgumentException e) {
            return RespondAsync(e.Message, ephemeral: true);
        }
    }
}
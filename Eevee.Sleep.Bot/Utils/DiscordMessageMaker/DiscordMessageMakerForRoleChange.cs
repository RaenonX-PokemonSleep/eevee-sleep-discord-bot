using Discord;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;

namespace Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

public static class DiscordMessageMakerForRoleChange {
    public static IEnumerable<string> MakeRoleSelectCorrespondenceList(
        TrackedRoleModel[] roles
    ) {
        return roles.Select((role, idx) => $"`{idx + 1}` - {MentionUtils.MentionRole(role.RoleId)}");
    }

    public static MessageComponent MakeRoleSelectButton(
        TrackedRoleModel[] roles,
        ButtonId buttonId
    ) {
        var builder = new ComponentBuilder();

        for (var idx = 0; idx < roles.Length; idx++) {
            var role = roles[idx];

            builder.WithButton(
                (idx + 1).ToString(),
                ButtonInteractionInfoSerializer.Serialize(
                    new ButtonInteractionInfo {
                        ButtonId = buttonId,
                        CustomId = role.RoleId,
                    }
                )
            );
        }

        return builder.Build();
    }

    public static Embed MakeChangeRoleResult(
        IUser user,
        ulong[] previousRoleIds,
        ulong[] currentRoleIds,
        Color color
    ) {
        return new EmbedBuilder()
            .WithColor(color)
            .WithAuthor(user)
            .WithTitle("Your owned roles")
            .AddField(
                "Roles before change",
                previousRoleIds.MentionAllRoles()
            )
            .AddField(
                "Roles after change",
                currentRoleIds.MentionAllRoles()
            )
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeShowRoleResult(IUser user, ulong[] roleIds) {
        return new EmbedBuilder()
            .WithColor(Colors.Success)
            .WithAuthor(user)
            .WithTitle("Your owned roles")
            .AddField(
                "Roles",
                roleIds.MentionAllRoles(),
                true
            )
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeDeleteRoleResult(IUser user, ulong roleId) {
        return new EmbedBuilder()
            .WithColor(Colors.Success)
            .WithAuthor(user)
            .WithTitle("Role deleted from database")
            .AddField("User", user.Mention)
            .AddField("Role", MentionUtils.MentionRole(roleId))
            .WithCurrentTimestamp()
            .WithFooter($"ID: {user.Id}")
            .Build();
    }

    public static Embed MakeTrackRoleResult(
        ulong roleId,
        int roleOwnedUserCount,
        string message,
        Color color
    ) {
        return new EmbedBuilder()
            .WithColor(color)
            .WithTitle(message)
            .AddField("Role", MentionUtils.MentionRole(roleId), true)
            .AddField("Role owner count", roleOwnedUserCount, true)
            .WithCurrentTimestamp()
            .Build();
    }
}
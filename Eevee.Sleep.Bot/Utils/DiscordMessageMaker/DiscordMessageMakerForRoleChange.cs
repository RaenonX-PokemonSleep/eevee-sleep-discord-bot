using Discord;
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
        ButtonId buttonId,
        int currentPage = 1,
        int itemsPerPage = GlobalConst.DiscordPaginationParams.ItemsPerPage
    ) {
        var builder = new ComponentBuilder();
        var totalPages = (int)Math.Ceiling((double)roles.Length / itemsPerPage);
        var startIndex = (currentPage - 1) * itemsPerPage;
        var pageRoles = roles.Skip(startIndex).Take(itemsPerPage).ToArray();

        // Add role buttons
        for (var idx = 0; idx < pageRoles.Length; idx++) {
            var role = pageRoles[idx];

            builder.WithButton(
                (startIndex + idx + 1).ToString(),
                ButtonInteractionInfoSerializer.Serialize(
                    new ButtonInteractionInfo {
                        ButtonId = buttonId,
                        CustomId = role.RoleId,
                    }
                )
            );
        }

        // Add pagination buttons if needed
        if (totalPages > 1) {
            var paginationRow = new ActionRowBuilder();

            paginationRow.WithButton(
                "◀",
                ButtonInteractionInfoSerializer.Serialize(
                    new ButtonInteractionInfo {
                        ButtonId = ButtonId.PagePrevious,
                        CustomId = (ulong)buttonId,
                    }
                ),
                disabled: currentPage == 1
            );

            paginationRow.WithButton(
                $"{currentPage}/{totalPages}",
                "page_info",
                ButtonStyle.Secondary,
                disabled: true
            );

            paginationRow.WithButton(
                "▶",
                ButtonInteractionInfoSerializer.Serialize(
                    new ButtonInteractionInfo {
                        ButtonId = ButtonId.PageNext,
                        CustomId = (ulong)buttonId,
                    }
                ),
                disabled: currentPage == totalPages
            );

            builder.AddRow(paginationRow);
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
using Discord;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;

namespace Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

public static class DiscordMessageMakerForRoleRestriction {
    public static Embed MakeRoleRestrictedNote(
        string roleName,
        IUser user,
        uint minAccountAgeDays,
        string guildName
    ) {
        var serverlink = $"[{guildName}](https://discord.com/channels/{ConfigHelper.GetDiscordWorkingGuild()})";
        var dueTo = user.CreatedAt.ToUniversalTime().AddDays(minAccountAgeDays);
        string[] messages = [
            $"The role {roleName} in {serverlink} requires account age of at least {minAccountAgeDays} days to prevent server raiding.",
            $"You will be able to have {roleName} after <t:{dueTo.ToUnixTimeSeconds()}:R>.",
            "",
            $"If you really want to join our Discord right away, please contact {MentionUtils.MentionUser(ConfigHelper.GetRoleRestrictionOwnerUserId())} for eligibility review."
        ];

        return new EmbedBuilder()
            .WithColor(Colors.Danger)
            .AddField("Message", messages.MergeLines())
            .AddField("Role", roleName)
            .AddField("User", user.Mention)
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeRestrictedRoleAddedMessage(
        ulong roleId,
        uint minAccountAgeDays
    ) {
        return new EmbedBuilder()
            .WithColor(Colors.Success)
            .WithTitle("Role restriction configured")
            .AddField("Role", MentionUtils.MentionRole(roleId))
            .AddField("Min. Account Age (Days)", minAccountAgeDays)
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeRestrictedRoleRemovedMessage(
        ulong roleId
    ) {
        return new EmbedBuilder()
            .WithColor(Colors.Danger)
            .WithTitle("Role restriction removed")
            .AddField("Role", MentionUtils.MentionRole(roleId))
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeRestrictedRoleAddedWhiteListMessage(
        ulong roleId,
        IUser user
    ) {
        return new EmbedBuilder()
            .WithColor(Colors.Success)
            .WithTitle("User added to role restriction whitelist")
            .AddField("Role", MentionUtils.MentionRole(roleId))
            .AddField("User", user.Mention)
            .WithCurrentTimestamp()
            .Build();
    }
}
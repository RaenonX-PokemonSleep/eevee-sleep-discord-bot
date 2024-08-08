using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Extensions;

public static class DiscordExtensions {
    private static async Task<IMessageChannel> GetMessageChannel(this IDiscordClient client, ulong channelId) {
        if (await client.GetChannelAsync(channelId) is not IMessageChannel channel) {
            throw new ArgumentException($"Not a message channel (#{channelId})");
        }

        return channel;
    }

    private static Task<IMessageChannel> GetAdminAlertChannelAsync(this IDiscordClient client) {
        return client.GetMessageChannel(ConfigHelper.GetDiscordAdminAlertChannelId());
    }

    public static async Task<IUserMessage> SendMessageInAdminAlertChannel(
        this IDiscordClient client,
        string? message = null,
        Embed? embed = null,
        Embed[]? embeds = null
    ) {
        return await (await client.GetAdminAlertChannelAsync())
            .SendMessageAsync(message, embed: embed, embeds: embeds);
    }

    private static Task<IMessageChannel> GetInGameAnnouncementNoticeChannelsAsync(
        this IDiscordClient client,
        AnnouncementLanguage language
    ) {
        return client.GetMessageChannel(ConfigHelper.GetInGameAnnouncementNotificationChannelId(language));
    }

    public static async Task SendMessageInInGameAnnouncementNoticeChannelAsync(
        this IDiscordClient client,
        AnnouncementLanguage language,
        string? message = null,
        Embed? embed = null,
        Embed[]? embeds = null
    ) {
        await (await client.GetInGameAnnouncementNoticeChannelsAsync(language))
            .SendMessageAsync(message, embed: embed, embeds: embeds);
    }

    private static Task<IMessageChannel> GetOfficialSiteAnnouncementNoticeChannelsAsync(
        this IDiscordClient client,
        AnnouncementLanguage language
    ) {
        return client.GetMessageChannel(ConfigHelper.GetOfficialSiteAnnouncementNotificationChannelId(language));
    }

    public static async Task SendMessageInOfficialSiteAnnouncementNoticeChannelAsync(
        this IDiscordClient client,
        AnnouncementLanguage language,
        string? message = null,
        Embed? embed = null,
        Embed[]? embeds = null
    ) {
        await (await client.GetOfficialSiteAnnouncementNoticeChannelsAsync(language))
            .SendMessageAsync(message, embed: embed, embeds: embeds);
    }

    public static SocketGuild GetCurrentWorkingGuild(this DiscordSocketClient client) {
        return client.GetGuild(ConfigHelper.GetDiscordWorkingGuild());
    }

    public static string MentionAllRoles(this ulong[] roles) {
        return roles.Length == 0 ? "(N/A)" : roles.Select(MentionUtils.MentionRole).MergeToSameLine();
    }

    public static SocketGuildUser AsGuildUser(this IUser user) {
        return user as SocketGuildUser ??
               throw new InvalidOperationException("User is not SocketGuildUser.");
    }
}
﻿using Discord;
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
}
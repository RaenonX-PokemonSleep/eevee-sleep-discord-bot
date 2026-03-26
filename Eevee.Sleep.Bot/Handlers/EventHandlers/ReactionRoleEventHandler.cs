using System.Collections.Concurrent;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Handlers.EventHandlers;

public static class ReactionRoleEventHandler {
    private static readonly ILogger Logger = LogHelper.CreateLogger(typeof(ReactionRoleEventHandler));

    // Cache: messageId -> model (null = known not-tracked)
    private static readonly ConcurrentDictionary<ulong, ReactionRoleMessageModel?> Cache = new();

    public static void InvalidateCache(ulong messageId) {
        Cache.TryRemove(messageId, out _);
    }

    public static async Task OnReactionAdded(
        DiscordSocketClient client,
        Cacheable<IUserMessage, ulong> cachedMessage,
        Cacheable<IMessageChannel, ulong> cachedChannel,
        SocketReaction reaction
    ) {
        if (reaction.UserId == client.CurrentUser.Id) {
            return;
        }

        var model = await GetReactionRoleModel(cachedMessage.Id);
        if (model is null) {
            return;
        }

        var channel = await cachedChannel.GetOrDownloadAsync();
        var message = await cachedMessage.GetOrDownloadAsync()
                      ?? await (channel as ITextChannel)!.GetMessageAsync(cachedMessage.Id) as IUserMessage;

        if (message is null) {
            return;
        }

        var emoteKey = GetEmoteKey(reaction);
        if (!model.EmoteToRoleMap.TryGetValue(emoteKey, out var roleId)) {
            await RemoveReaction(message, reaction);
            return;
        }

        var guild = (channel as SocketGuildChannel)?.Guild;

        var user = guild?.GetUser(reaction.UserId);
        if (user is null) {
            return;
        }

        if (!await ValidateWhitelist(model, user, message, reaction)) {
            return;
        }

        await GrantRole(user, roleId);
    }

    public static async Task OnReactionRemoved(
        DiscordSocketClient client,
        Cacheable<IUserMessage, ulong> cachedMessage,
        Cacheable<IMessageChannel, ulong> cachedChannel,
        SocketReaction reaction
    ) {
        if (reaction.UserId == client.CurrentUser.Id) {
            return;
        }

        var model = await GetReactionRoleModel(cachedMessage.Id);
        if (model is null) {
            return;
        }

        var emoteKey = GetEmoteKey(reaction);
        if (!model.EmoteToRoleMap.TryGetValue(emoteKey, out var roleId)) {
            return;
        }

        var channel = await cachedChannel.GetOrDownloadAsync();
        var guild = (channel as SocketGuildChannel)?.Guild;
        var user = guild?.GetUser(reaction.UserId);

        if (user is null) {
            return;
        }

        try {
            await user.RemoveRoleAsync(roleId);
            Logger.LogInformation(
                "Removed role {RoleId} from user {UserId} via reaction removal",
                roleId, user.Id
            );
        } catch (Exception ex) {
            Logger.LogError(ex, "Failed to remove role {RoleId} from user {UserId}", roleId, user.Id);
        }
    }

    private static async Task<ReactionRoleMessageModel?> GetReactionRoleModel(ulong messageId) {
        if (Cache.TryGetValue(messageId, out var cached)) {
            return cached;
        }

        var model = await ReactionRoleController.FindByMessageId(messageId);
        Cache[messageId] = model;

        return model;
    }

    private static string GetEmoteKey(SocketReaction reaction) {
        return reaction.Emote is Emote emote ? emote.ToString() : reaction.Emote.Name;
    }

    private static async Task RemoveReaction(IUserMessage message, SocketReaction reaction) {
        try {
            await message.RemoveReactionAsync(reaction.Emote, reaction.UserId);
        } catch (Exception ex) {
            Logger.LogError(
                ex, "Failed to remove unauthorized emote reaction from user {UserId}", reaction.UserId
            );
        }
    }

    private static async Task<bool> ValidateWhitelist(
        ReactionRoleMessageModel model,
        SocketGuildUser user,
        IUserMessage message,
        SocketReaction reaction
    ) {
        if (model.WhitelistedRoleIds is null) {
            return true;
        }

        if (user.Roles.Any(r => model.WhitelistedRoleIds.Contains(r.Id))) {
            return true;
        }

        await RemoveReaction(message, reaction);
        await SendSubscriberOnlyDm(user);
        return false;
    }

    private static async Task SendSubscriberOnlyDm(SocketGuildUser user) {
        try {
            var dmChannel = await user.CreateDMChannelAsync();
            await dmChannel.SendMessageAsync(
                "The reaction role you just selected is only available to subscribers.\n" +
                "Subscription plans: https://pks.raenonx.cc/subscription/plan"
            );
        } catch (HttpException) {
            // User has DMs disabled — silently ignore
        } catch (Exception ex) {
            Logger.LogError(ex, "Failed to send subscriber-only DM to user {UserId}", user.Id);
        }
    }

    private static async Task GrantRole(SocketGuildUser user, ulong roleId) {
        try {
            await user.AddRoleAsync(roleId);
            await DiscordRoleRecordController.AddRoles(user.Id, [roleId]);
            Logger.LogInformation(
                "Granted role {RoleId} to user {UserId} via reaction",
                roleId, user.Id
            );
        } catch (Exception ex) {
            Logger.LogError(ex, "Failed to grant role {RoleId} to user {UserId}", roleId, user.Id);
        }
    }
}

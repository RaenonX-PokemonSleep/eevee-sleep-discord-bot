using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using MongoDB.Driver;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

namespace Eevee.Sleep.Bot.Utils;

public static class RoleEventHelper {
    private static readonly ILogger Logger = LogHelper.CreateLogger(typeof(RoleEventHelper));

    public static async Task ExecuteRoleEvent(
        SocketGuild guild,
        IDiscordInteraction interaction,
        List<RoleEventEntry> freeEntries,
        List<RoleEventEntry> subscriberEntries,
        string designer,
        long expiryEpoch,
        bool omitLangRoles,
        Func<string, Task>? reportProgress = null
    ) {
        var createdEmotes = new List<GuildEmote>();
        var createdRoles = new List<IRole>();
        var sentMessages = new List<IUserMessage>();

        var totalEntries = freeEntries.Count + subscriberEntries.Count;

        using var session = await MongoConst.Client.StartSessionAsync();
        session.StartTransaction();

        try {
            await (reportProgress?.Invoke(
                $"Role event confirmed. Processing...\n" +
                $"⏳ (1/4) Creating {totalEntries} emote(s) and role(s)..."
            ) ?? Task.CompletedTask);

            var freeItems = await CreateEmotesAndRoles(guild, freeEntries, createdEmotes, createdRoles);
            var subItems = await CreateEmotesAndRoles(guild, subscriberEntries, createdEmotes, createdRoles);

            await (reportProgress?.Invoke(
                $"Role event confirmed. Processing...\n" +
                $"✅ (1/4) {totalEntries} emote(s) and role(s) created\n" +
                $"⏳ (2/4) Reordering and tracking roles..."
            ) ?? Task.CompletedTask);

            await ReorderRolesBelowAnchor(guild, createdRoles);
            await TrackAllRoles(guild, createdRoles);

            await (reportProgress?.Invoke(
                $"Role event confirmed. Processing...\n" +
                $"✅ (1/4) {totalEntries} emote(s) and role(s) created\n" +
                $"✅ (2/4) Roles reordered and tracked\n" +
                $"⏳ (3/4) Sending messages to channel..."
            ) ?? Task.CompletedTask);

            await SendAllMessages(
                guild, freeItems, subItems, designer, expiryEpoch,
                omitLangRoles, createdEmotes, sentMessages, session
            );

            await (reportProgress?.Invoke(
                $"Role event confirmed. Processing...\n" +
                $"✅ (1/4) {totalEntries} emote(s) and role(s) created\n" +
                $"✅ (2/4) Roles reordered and tracked\n" +
                $"✅ (3/4) Messages sent ({sentMessages.Count} total)\n" +
                $"⏳ (4/4) Committing and finalizing..."
            ) ?? Task.CompletedTask);

            await session.CommitTransactionAsync();
            await SendSummary(interaction, createdEmotes, createdRoles, sentMessages, expiryEpoch);
        } catch (Exception ex) {
            Logger.LogError(ex, "Role event failed, initiating full rollback");
            await RollbackAll(guild, session, createdEmotes, createdRoles, sentMessages);
            await interaction.FollowupAsync("Role event failed and was rolled back. Check logs.", ephemeral: true);
        }
    }

    private static async Task<List<(RoleEventEntry Entry, GuildEmote Emote, IRole Role)>> CreateEmotesAndRoles(
        SocketGuild guild,
        List<RoleEventEntry> entries,
        List<GuildEmote> createdEmotes,
        List<IRole> createdRoles
    ) {
        var items = new List<(RoleEventEntry Entry, GuildEmote Emote, IRole Role)>();
        using var httpClient = new HttpClient();

        foreach (var entry in entries) {
            var imageBytes = await httpClient.GetByteArrayAsync(entry.ImageUrl);

            var emote = await CreateEmoteFromBytes(guild, entry.EmoteName, imageBytes);
            createdEmotes.Add(emote);

            var role = await CreateRoleForEntry(guild, entry.NameEn, imageBytes);
            createdRoles.Add(role);

            items.Add((entry, emote, role));
        }

        return items;
    }

    private static async Task<GuildEmote> CreateEmoteFromBytes(
        SocketGuild guild,
        string emoteName,
        byte[] imageBytes
    ) {
        var resizedBytes = ResizeImageTo512(imageBytes);
        using var stream = new MemoryStream(resizedBytes);
        var emote = await guild.CreateEmoteAsync(emoteName, new Discord.Image(stream));

        Logger.LogInformation("Created emote {EmoteName} ({EmoteId})", emote.Name, emote.Id);
        return emote;
    }

    private const int MaxEmoteSizeBytes = 2 * 1024 * 1024; // 2 MB (Discord limit is 2048 KB)
    private static readonly int[] EmoteSizeCandidates = [512, 256, 128, 96];

    private static byte[] ResizeImageTo512(byte[] imageBytes) {
        foreach (var size in EmoteSizeCandidates) {
            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);
            image.Mutate(x => x.Resize(new ResizeOptions {
                Size = new Size(size, size),
                Mode = ResizeMode.Max,
            }));

            using var output = new MemoryStream();
            image.SaveAsPng(output);
            var result = output.ToArray();

            if (result.Length > MaxEmoteSizeBytes) {
                continue;
            }

            Logger.LogInformation(
                "Resized emote image to {Size}px, {Bytes} bytes",
                size, result.Length
            );
            return result;
        }

        throw new InvalidOperationException(
            $"Image cannot be reduced below 2 MB even at 96px. Original size: {imageBytes.Length} bytes."
        );
    }

    private static async Task<IRole> CreateRoleForEntry(
        SocketGuild guild,
        string roleName,
        byte[] imageBytes
    ) {
        var color = GetDominantColor(imageBytes);
        var resizedBytes = ResizeImageTo512(imageBytes);
        using var iconStream = new MemoryStream(resizedBytes);

        var role = await guild.CreateRoleAsync(
            roleName,
            color: color,
            isMentionable: false,
            icon: new Discord.Image(iconStream)
        );

        Logger.LogInformation(
            "Created role {RoleName} ({RoleId}) with dominant color #{R:X2}{G:X2}{B:X2}",
            role.Name, role.Id, color.R, color.G, color.B
        );

        return role;
    }

    private static Discord.Color GetDominantColor(byte[] imageBytes) {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);
        var counts = new Dictionary<(byte R, byte G, byte B), int>();

        image.ProcessPixelRows(accessor => {
            for (var y = 0; y < accessor.Height; y++) {
                var row = accessor.GetRowSpan(y);
                foreach (ref var pixel in row) {
                    if (pixel.A < 128) {
                        continue;
                    }

                    var key = (QuantizeChannel(pixel.R), QuantizeChannel(pixel.G), QuantizeChannel(pixel.B));
                    counts[key] = counts.GetValueOrDefault(key) + 1;
                }
            }
        });

        if (counts.Count == 0) {
            return Discord.Color.Default;
        }

        var (r, g, b) = counts.MaxBy(x => x.Value).Key;
        return new Discord.Color(r, g, b);
    }

    private static byte QuantizeChannel(byte value) => (byte)(value / 32 * 32);

    private static async Task ReorderRolesBelowAnchor(SocketGuild guild, List<IRole> roles) {
        var anchorRoleId = ConfigHelper.GetRoleEventAnchorRoleId();
        var anchorRole = guild.GetRole(anchorRoleId);

        if (anchorRole is null) {
            throw new InvalidOperationException($"Anchor role {anchorRoleId} not found in guild.");
        }

        var reorderProperties = roles
            .Select(r => new ReorderRoleProperties(r.Id, anchorRole.Position))
            .ToArray();

        await guild.ReorderRolesAsync(reorderProperties);
        Logger.LogInformation("Reordered {Count} roles below anchor {AnchorRoleId}", roles.Count, anchorRoleId);
    }

    private static async Task TrackAllRoles(
        SocketGuild guild,
        List<IRole> roles
    ) {
        foreach (var role in roles) {
            await DiscordTrackedRoleController.SaveTrackedRole(role);

            var roleOwnedUsers = guild.Users
                .Where(x => x.Roles.Any(r => r.Id == role.Id))
                .Select(x => x.Id)
                .ToArray();

            await DiscordRoleRecordController.BulkAddRoles(roleOwnedUsers, [role.Id]);
        }

        Logger.LogInformation("Tracked {Count} roles", roles.Count);
    }

    private static async Task SendAllMessages(
        SocketGuild guild,
        List<(RoleEventEntry Entry, GuildEmote Emote, IRole Role)> freeItems,
        List<(RoleEventEntry Entry, GuildEmote Emote, IRole Role)> subItems,
        string designer,
        long expiryEpoch,
        bool omitLangRoles,
        List<GuildEmote> allEmotes,
        List<IUserMessage> sentMessages,
        IClientSessionHandle session
    ) {
        var channel = GetTargetChannel(guild);

        var msg1 = await SendMessageAllReactions(
            channel, freeItems, designer, expiryEpoch, omitLangRoles
        );
        sentMessages.Add(msg1);

        var emoteCyclingMessages = await SendEmoteCyclingMessages(channel, allEmotes);
        sentMessages.AddRange(emoteCyclingMessages);

        IUserMessage? msg3 = null;
        if (subItems.Count > 0) {
            msg3 = await SendMessageSubscriberReactions(
                channel, subItems, designer, expiryEpoch, omitLangRoles
            );
            sentMessages.Add(msg3);
        }

        var msg4 = await channel.SendMessageAsync(
            DiscordMessageMakerForRoleEvent.BuildMessageLast(msg1.GetJumpUrl())
        );
        sentMessages.Add(msg4);

        await RegisterReactionRoleBindings(msg1, freeItems, null, session);
        if (msg3 is not null) {
            var whitelistedRoleIds = ConfigHelper.GetAllSubscriberRoleIds();
            await RegisterReactionRoleBindings(msg3, subItems, whitelistedRoleIds, session);
        }

        await RegisterSelfDestructMessages(
            msg1, msg3, channel.Id, expiryEpoch, session
        );
    }

    private static IMessageChannel GetTargetChannel(SocketGuild guild) {
        var channelId = ConfigHelper.GetRoleEventTargetChannelId();
        var channel = guild.GetChannel(channelId) as IMessageChannel;

        return channel ?? throw new InvalidOperationException($"Target channel {channelId} not found or is not a text channel.");
    }

    private static async Task<IUserMessage> SendMessageAllReactions(
        IMessageChannel channel,
        List<(RoleEventEntry Entry, GuildEmote Emote, IRole Role)> freeItems,
        string designer,
        long expiryEpoch,
        bool omitLangRoles
    ) {
        var content = DiscordMessageMakerForRoleEvent.BuildMessageAll(
            expiryEpoch, designer, freeItems, omitLangRoles
        );
        var message = await channel.SendMessageAsync(content);

        foreach (var (_, emote, _) in freeItems) {
            await message.AddReactionAsync(emote);
        }

        return message;
    }

    private static async Task<List<IUserMessage>> SendEmoteCyclingMessages(
        IMessageChannel channel,
        List<GuildEmote> allEmotes
    ) {
        var messages = DiscordMessageMakerForRoleEvent.BuildEmoteCyclingMessages(allEmotes.ToArray());
        var sent = new List<IUserMessage>();

        foreach (var text in messages) {
            sent.Add(await channel.SendMessageAsync(text));
        }

        return sent;
    }

    private static async Task<IUserMessage> SendMessageSubscriberReactions(
        IMessageChannel channel,
        List<(RoleEventEntry Entry, GuildEmote Emote, IRole Role)> subItems,
        string designer,
        long expiryEpoch,
        bool omitLangRoles
    ) {
        var content = DiscordMessageMakerForRoleEvent.BuildMessageSubscribers(
            expiryEpoch, designer, subItems, omitLangRoles
        );
        var message = await channel.SendMessageAsync(content);

        foreach (var (_, emote, _) in subItems) {
            await message.AddReactionAsync(emote);
        }

        return message;
    }

    private static async Task RegisterReactionRoleBindings(
        IUserMessage message,
        List<(RoleEventEntry Entry, GuildEmote Emote, IRole Role)> items,
        ulong[]? whitelistedRoleIds,
        IClientSessionHandle session
    ) {
        var emoteToRoleMap = items.ToDictionary(
            x => x.Emote.ToString(),
            x => x.Role.Id
        );

        var model = new ReactionRoleMessageModel {
            MessageId = message.Id,
            ChannelId = message.Channel.Id,
            EmoteToRoleMap = emoteToRoleMap,
            WhitelistedRoleIds = whitelistedRoleIds,
        };

        await ReactionRoleController.InsertReactionRoleMessage(model, session);
    }

    private static async Task RegisterSelfDestructMessages(
        IUserMessage msg1,
        IUserMessage? msg3,
        ulong channelId,
        long expiryEpoch,
        IClientSessionHandle session
    ) {
        var models = new List<SelfDestructMessageModel> {
            new() {
                MessageId = msg1.Id,
                ChannelId = channelId,
                DestructAtEpochSec = expiryEpoch,
            },
        };

        if (msg3 is not null) {
            models.Add(new SelfDestructMessageModel {
                MessageId = msg3.Id,
                ChannelId = channelId,
                DestructAtEpochSec = expiryEpoch,
            });
        }

        await SelfDestructController.InsertManySelfDestructMessages(models, session);
    }

    private static async Task SendSummary(
        IDiscordInteraction interaction,
        List<GuildEmote> emotes,
        List<IRole> roles,
        List<IUserMessage> messages,
        long expiryEpoch
    ) {
        var messageLinks = messages.Select(m => m.GetJumpUrl()).ToArray();
        var embed = DiscordMessageMakerForRoleEvent.BuildSummaryEmbed(
            emotes.Count, roles.Count, messageLinks, expiryEpoch
        );

        await interaction.FollowupAsync(embed: embed, ephemeral: true);
    }

    private static async Task RollbackAll(
        SocketGuild guild,
        IClientSessionHandle session,
        List<GuildEmote> emotes,
        List<IRole> roles,
        List<IUserMessage> messages
    ) {
        try {
            await session.AbortTransactionAsync();
        } catch (Exception ex) {
            Logger.LogError(ex, "Failed to abort MongoDB transaction");
        }

        await RollbackDiscordMessages(messages);
        await RollbackDiscordRoles(guild, roles);
        await RollbackDiscordEmotes(guild, emotes);
    }

    private static async Task RollbackDiscordMessages(List<IUserMessage> messages) {
        for (var i = messages.Count - 1; i >= 0; i--) {
            try {
                await messages[i].DeleteAsync();
            } catch (Exception ex) {
                Logger.LogError(ex, "Failed to rollback message {MessageId}", messages[i].Id);
            }
        }
    }

    private static async Task RollbackDiscordRoles(SocketGuild guild, List<IRole> roles) {
        for (var i = roles.Count - 1; i >= 0; i--) {
            try {
                var role = guild.GetRole(roles[i].Id);
                if (role is null) {
                    Logger.LogWarning("Role {RoleId} not found during rollback, skipping", roles[i].Id);
                    continue;
                }
                await role.DeleteAsync();
            } catch (Exception ex) {
                Logger.LogError(ex, "Failed to rollback role {RoleId}", roles[i].Id);
            }
        }
    }

    private static async Task RollbackDiscordEmotes(SocketGuild guild, List<GuildEmote> emotes) {
        for (var i = emotes.Count - 1; i >= 0; i--) {
            try {
                await guild.DeleteEmoteAsync(emotes[i]);
            } catch (Exception ex) {
                Logger.LogError(ex, "Failed to rollback emote {EmoteId}", emotes[i].Id);
            }
        }
    }
}

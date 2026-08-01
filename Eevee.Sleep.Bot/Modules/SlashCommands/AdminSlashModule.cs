using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Preconditions;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Modules.SlashCommands;

[Group("admin", "Admin-only commands.")]
[RequireUserPermission(GuildPermission.Administrator, Group = "AdminAccess")]
[RequireAdminRole(Group = "AdminAccess")]
[CommandContextType(InteractionContextType.Guild)]
public partial class AdminSlashModule : InteractionModuleBase<SocketInteractionContext> {
    private static readonly Regex EmoteNamePattern = EmoteNameRegex();
    private static readonly Regex MentionPattern = MentionRegex();
    private static readonly Regex UsernamePattern = UsernameRegex();

    private const string EntriesFormat = "ImageURL,EmoteName,NameEN,NameZH,NameJP";

    [SlashCommand("role-event", "Automates new Pokémon role release via emotes, roles, messages, and reactions.")]
    [UsedImplicitly]
    public Task RoleEventAsync() {
        var modal = new ModalBuilder()
            .WithTitle("New Pokémon Role Event")
            .WithCustomId(nameof(ModalId.RoleEventModal))
            .AddTextInput(
                "Designer(s)",
                nameof(ModalFieldId.RoleEventDesigner),
                placeholder: "Comma-separated: <@123>, <@456> or @username, @username2",
                required: true
            )
            .AddTextInput(
                "Entries",
                nameof(ModalFieldId.RoleEventEntries),
                TextInputStyle.Paragraph,
                placeholder: $"{EntriesFormat}\n(one row per line)",
                required: true
            )
            .AddTextInput(
                "Subscriber Entries (optional)",
                nameof(ModalFieldId.RoleEventSubscriberEntries),
                TextInputStyle.Paragraph,
                placeholder: "Same format as Entries — subscriber-only roles",
                required: false
            )
            .AddTextInput(
                "Expiry Epoch (blank = 14 days from now)",
                nameof(ModalFieldId.RoleEventExpiryEpoch),
                placeholder: "Unix timestamp in seconds (e.g. 1777286400)",
                required: false
            )
            .AddTextInput(
                "Omit language roles? (blank = auto)",
                nameof(ModalFieldId.RoleEventOmitLangRoles),
                placeholder: "true or false",
                required: false
            )
            .Build();

        return RespondWithModalAsync(modal);
    }

    internal static List<RoleEventEntry> ParseCsvEntries(string csv) {
        return csv
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(',').Select(f => f.Trim()).ToArray())
            .Where(fields => fields.Length == 5)
            .Select(f => new RoleEventEntry(f[0], f[1], f[2], f[3], f[4]))
            .ToList();
    }

    internal static string? ValidateAll(
        SocketGuild guild,
        string designer,
        List<RoleEventEntry> freeEntries,
        List<RoleEventEntry> subEntries
    ) {
        if (freeEntries.Count == 0) {
            return $"No valid entries found. Each row must have exactly 5 comma-separated fields:\n`{EntriesFormat}`";
        }

        var entryError = ValidateEntries(freeEntries, "entries");
        if (entryError is not null) {
            return entryError;
        }

        if (subEntries.Count > 0) {
            var subError = ValidateEntries(subEntries, "subscriber-entries");
            if (subError is not null) {
                return subError;
            }
        }

        var designerError = ValidateDesignerMentions(guild, designer);
        if (designerError is not null) {
            return designerError;
        }

        var channelId = ConfigHelper.GetRoleEventTargetChannelId();
        return guild.GetChannel(channelId) is not IMessageChannel ?
            $"Configured target channel {channelId} is not a valid text channel." :
            null;
    }

    private static string? ValidateEntries(List<RoleEventEntry> entries, string paramName) {
        for (var i = 0; i < entries.Count; i++) {
            var entry = entries[i];
            var row = i + 1;

            if (!Uri.TryCreate(entry.ImageUrl, UriKind.Absolute, out _)) {
                return $"`{paramName}` row {row}: Invalid URL `{entry.ImageUrl}`.";
            }

            if (!EmoteNamePattern.IsMatch(entry.EmoteName)) {
                return $"`{paramName}` row {row}: Emote name `{entry.EmoteName}` must match `[a-zA-Z0-9_]{{2,32}}`.";
            }

            if (string.IsNullOrWhiteSpace(entry.NameEn) ||
                string.IsNullOrWhiteSpace(entry.NameZh) ||
                string.IsNullOrWhiteSpace(entry.NameJp)) {
                return $"`{paramName}` row {row}: All three name fields (EN, ZH, JP) must be non-empty.";
            }
        }

        return null;
    }

    private static string? ValidateDesignerMentions(SocketGuild guild, string designer) {
        var tokens = designer.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToList();

        if (tokens.Count == 0) {
            return "Designer field must not be empty.";
        }

        foreach (var token in tokens) {
            var mentionMatch = MentionPattern.Match(token);
            if (mentionMatch.Success && mentionMatch.Value == token) {
                if (!ulong.TryParse(mentionMatch.Groups[1].Value, out var userId)) {
                    return $"Invalid designer mention: `{token}`.";
                }

                if (guild.GetUser(userId) is null) {
                    return $"Designer mention `{token}` does not resolve to a guild member.";
                }

                continue;
            }

            var usernameMatch = UsernamePattern.Match(token);
            if (!usernameMatch.Success || usernameMatch.Value != token) {
                return $"Designer `{token}` is not a valid user mention. Use `<@UserID>` or `@username` format.";
            }

            var username = usernameMatch.Groups[1].Value;
            var guildUser = guild.Users.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.DisplayName, username, StringComparison.OrdinalIgnoreCase)
            );
            if (guildUser is null) {
                return $"Designer `{token}` does not resolve to a guild member. Check the username is correct.";
            }
        }

        return null;
    }

    internal static string NormalizeDesigner(SocketGuild guild, string designer) {
        var tokens = designer.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim());

        var normalized = tokens.Select(token => {
                var usernameMatch = UsernamePattern.Match(token);
                if (!usernameMatch.Success || usernameMatch.Value != token) {
                    return token;
                }

                var username = usernameMatch.Groups[1].Value;
                var guildUser = guild.Users.FirstOrDefault(u =>
                    string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.DisplayName, username, StringComparison.OrdinalIgnoreCase)
                );
                return guildUser is not null ? MentionUtils.MentionUser(guildUser.Id) : token;
            }
        );

        return string.Join(", ", normalized);
    }

    internal static async Task ShowPreview(
        IDiscordInteraction interaction,
        ulong userId,
        List<RoleEventEntry> freeEntries,
        List<RoleEventEntry> subEntries,
        string designer,
        long expiryEpoch,
        bool omitLangRoles
    ) {
        var msgAllPreview = BuildPreviewContent(freeEntries, designer, expiryEpoch);
        var msgSubPreview = subEntries.Count > 0 ?
            BuildPreviewContent(subEntries, designer, expiryEpoch) :
            "(No subscriber entries)";

        var totalCount = freeEntries.Count + subEntries.Count;
        var embed = DiscordMessageMakerForRoleEvent.BuildPreviewEmbed(
            msgAllPreview,
            msgSubPreview,
            totalCount,
            totalCount,
            expiryEpoch
        );

        var confirmInfo = new ButtonInteractionInfo {
            ButtonId = ButtonId.RoleEventConfirm,
            CustomId = expiryEpoch.ToUlong(),
        };
        var cancelInfo = new ButtonInteractionInfo {
            ButtonId = ButtonId.RoleEventCancel,
            CustomId = 0,
        };
        var components = new ComponentBuilder()
            .WithButton("Confirm", ButtonInteractionInfoSerializer.Serialize(confirmInfo), ButtonStyle.Success)
            .WithButton("Cancel", ButtonInteractionInfoSerializer.Serialize(cancelInfo), ButtonStyle.Danger)
            .Build();

        await interaction.RespondAsync(embed: embed, components: components, ephemeral: true);

        RoleEventPendingStore.Set(
            userId,
            new RoleEventPendingData(freeEntries, subEntries, designer, expiryEpoch, omitLangRoles)
        );
    }

    private static string BuildPreviewContent(
        List<RoleEventEntry> entries,
        string designer,
        long expiryEpoch
    ) {
        var parsedDesigner = string.Join(
            ", ",
            MentionPattern.Matches(designer)
                .Select(m => MentionUtils.MentionUser(ulong.Parse(m.Groups[1].Value)))
        );

        var roleLines = string.Join(
            "\n",
            entries.Select(e => $"{e.NameEn} / {e.NameZh} / {e.NameJp}: :{e.EmoteName}:")
        );

        return $"Designer: {parsedDesigner}\nExpiry: <t:{expiryEpoch}:F>\n\n{roleLines}";
    }

    [GeneratedRegex("^[a-zA-Z0-9_]{2,32}$")]
    private static partial Regex EmoteNameRegex();

    [GeneratedRegex(@"<@(\d+)>")]
    private static partial Regex MentionRegex();

    [GeneratedRegex("^@(.+)$")]
    private static partial Regex UsernameRegex();
}

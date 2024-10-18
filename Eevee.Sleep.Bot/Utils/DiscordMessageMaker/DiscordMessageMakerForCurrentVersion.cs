using System.Text.Json;
using Discord;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.ChesterMicroService;

namespace Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

public static class DiscordMessageMakerForCurrentVersion {
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

    public static Embed MakeCurrentVersionUpdated(ChesterCurrentVersion? old, ChesterCurrentVersion @new) {
        return new EmbedBuilder()
            .WithColor(Colors.Info)
            .WithTitle("Data version updated!")
            .WithDescription(
                new[] {
                    "## Old",
                    "```json",
                    old is null ? "(N/A)" : JsonSerializer.Serialize(old, JsonSerializerOptions),
                    "```",
                    "## New",
                    "```json",
                    JsonSerializer.Serialize(@new, JsonSerializerOptions),
                    "```",
                }.MergeLines()
            )
            .WithCurrentTimestamp()
            .Build();
    }
}
using Discord;
using Eevee.Sleep.Bot.Enums;
using IResult = Discord.Interactions.IResult;

namespace Eevee.Sleep.Bot.Utils;

public static class DiscordMessageMaker {
    public static Embed MakeError(IResult result) {
        return new EmbedBuilder()
            .WithColor(Colors.Danger)
            .WithTitle($"Error - {result.Error}")
            .WithDescription(result.ErrorReason)
            .Build();
    }
}
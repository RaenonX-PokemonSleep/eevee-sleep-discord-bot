using Discord;
using Discord.Net;
using Eevee.Sleep.Bot.Enums;
using IResult = Discord.Interactions.IResult;

namespace Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

public static class DiscordMessageMakerForError {
    public static Embed MakeError(IResult result) {
        return new EmbedBuilder()
            .WithColor(Colors.Danger)
            .WithTitle($"Error - {result.Error}")
            .WithDescription(result.ErrorReason)
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeErrorFromLog(LogMessage message) {
        return new EmbedBuilder()
            .WithColor(Colors.Warning)
            .WithTitle($"{message.Source}: {message.Message}")
            .WithDescription($"```{message.Exception}```")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed MakeDiscordHttpException(HttpException e) {
        return new EmbedBuilder()
            .WithColor(Colors.Warning)
            .WithTitle($"{e.Source}: {e.Message} ({e.DiscordCode})")
            .WithDescription($"```{e.StackTrace}```")
            .WithCurrentTimestamp()
            .Build();
    }
}
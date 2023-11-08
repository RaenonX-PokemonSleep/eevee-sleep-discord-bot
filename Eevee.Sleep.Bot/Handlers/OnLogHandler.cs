using Discord;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Handlers;

public static class OnLogHandler {
    private static readonly ILogger Logger = LogHelper.CreateLogger(typeof(OnLogHandler));

    public static Task OnLogAsync(LogMessage message) {
        Logger.Log(
            message.Severity.ToLogLevel(),
            message.Exception,
            "{Source}: {Message}",
            message.Source,
            message.Message
        );

        return Task.CompletedTask;
    }
}
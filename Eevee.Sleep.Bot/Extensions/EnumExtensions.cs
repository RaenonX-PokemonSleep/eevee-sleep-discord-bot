using Discord;

namespace Eevee.Sleep.Bot.Extensions;

public static class EnumExtensions {
    public static LogLevel ToLogLevel(this LogSeverity severity) {
        return severity switch {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => throw new ArgumentOutOfRangeException(
                nameof(severity),
                severity,
                $"Unhandled Discord log severity: {severity}"
            ),
        };
    }
}
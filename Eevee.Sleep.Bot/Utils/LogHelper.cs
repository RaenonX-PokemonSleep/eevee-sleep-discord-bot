using Microsoft.Extensions.Logging.Console;

namespace Eevee.Sleep.Bot.Utils;

public static class LogHelper {
    public static readonly Action<SimpleConsoleFormatterOptions> LoggingConfigureAction = options => {
        // It's better NOT to enable single line because error logs are single line, making it harder to read
        options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
        options.UseUtcTimestamp = true;
    };

    internal static ILoggerFactory Factory { private get; set; } = LoggerFactory.Create(
        builder => builder.AddSimpleConsole(LoggingConfigureAction)
    );

    public static ILogger CreateLogger(Type @class) {
        return Factory.CreateLogger(@class.FullName ?? @class.Assembly.Location);
    }
}
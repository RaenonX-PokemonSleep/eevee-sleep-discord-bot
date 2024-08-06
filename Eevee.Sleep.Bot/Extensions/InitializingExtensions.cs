using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Extensions;

public static class InitializingExtensions {
    private static readonly ILogger Logger = LogHelper.CreateLogger(typeof(InitializingExtensions));

    public static WebApplicationBuilder BuildCommon(this WebApplicationBuilder builder) {
        ConfigHelper.Initialize(builder.Configuration);
        builder.Services.AddLogging(logging => logging.AddSimpleConsole(LogHelper.LoggingConfigureAction));

        return builder;
    }

    public static void AddCorsFromConfig(this IServiceCollection services) {
        services.AddCors(
            options => options.AddDefaultPolicy(
                policy => {
                    var origin = ConfigHelper.GetAllowedOrigin();
                    Logger.LogInformation("CORS Origin: {Origin}", origin);

                    policy.AllowCredentials().AllowAnyHeader().WithOrigins(origin);
                }
            )
        );
    }

    public static void ConfigureBackgroundServiceExceptionBehaviorToIgnore(this IServiceCollection services) {
        services.Configure<HostOptions>(
            options => {
                options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            }
        );
    }

    public static async Task BootAsync(this IHost host) {
        await MongoManager.Initialize();
        await host.RunAsync();
    }

    public static T InitLogging<T>(this T host) where T : IHost {
        LogHelper.Factory = host.Services.GetRequiredService<ILoggerFactory>();

        return host;
    }
}
using Eevee.Sleep.Bot.Extensions;

namespace Eevee.Sleep.Bot.Utils;

public static class ConfigHelper {
    private static IConfiguration? _config;

    private static IConfiguration Config => _config ?? throw new InvalidOperationException("Config not initialized");

    public static void Initialize(IConfiguration? configuration) {
        _config = configuration;
    }

    private static IConfigurationSection GetDiscordSection() {
        return Config.GetRequiredSection("Discord");
    }

    public static string GetDiscordToken() {
        return GetDiscordSection().GetRequiredValue<string>("Token");
    }
    
    public static ulong GetDiscordWorkingGuild() {
        return GetDiscordSection().GetRequiredValue<ulong>("Guild");
    }

    private static IConfigurationSection GetDiscordChannelSection() {
        return GetDiscordSection().GetRequiredSection("Channels");
    }

    public static ulong GetDiscordAdminAlertChannelId() {
        return GetDiscordChannelSection().GetRequiredValue<ulong>("AdminAlert");
    }


    public static string GetAllowedOrigin() {
        return Config.GetRequiredValue<string>("AllowedOrigin");
    }

    public static string GetMongoDbUrl() {
        return Config.GetRequiredSection("Mongo").GetRequiredValue<string>("Url");
    }
}
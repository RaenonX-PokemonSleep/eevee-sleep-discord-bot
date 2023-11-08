namespace Eevee.Sleep.Bot.Extensions;

public static class ConfigExtensions {
    public static T GetRequiredValue<T>(this IConfigurationSection section, string key) {
        return section.GetValue<T>(key) ??
               throw new InvalidOperationException(
                   $"Key `{key}` does not exist in the config section of `{section.Path}`"
               );
    }

    public static T GetRequiredValue<T>(this IConfiguration configuration, string key) {
        return configuration.GetValue<T>(key) ??
               throw new InvalidOperationException($"Key `{key}` does not exist in the config");
    }
}
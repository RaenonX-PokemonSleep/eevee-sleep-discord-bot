namespace Eevee.Sleep.Bot.Utils;

public static class GlobalConst {
    private static readonly Dictionary<string, (Func<ulong> GetRoleId, string SourceName)> PlatformSubscriberRoleMap =
        new() {
            {
                SubscriptionSource.Stripe,
                (ConfigHelper.GetDiscordStripeSubscriberRoleId, "Stripe")
            },
            {
                SubscriptionSource.Github,
                (ConfigHelper.GetDiscordGithubSubscriberRoleId, "GitHub")
            },
        };

    public static bool TryGetPlatformSubscriberRole(
        string? source,
        out ulong roleId,
        out string sourceName
    ) {
        if (source is not null && PlatformSubscriberRoleMap.TryGetValue(source, out var sourceRoleConfig)) {
            roleId = sourceRoleConfig.GetRoleId();
            sourceName = sourceRoleConfig.SourceName;
            return true;
        }

        roleId = 0;
        sourceName = string.Empty;
        return false;
    }

    public static string[] GetSupportedPlatformSubscriberSources() {
        return [..PlatformSubscriberRoleMap.Keys];
    }

    public static class SubscriptionSource {
        public const string Discord = "discord";

        public const string DiscordOneTime = "discordOneTime";

        public const string Patreon = "patreon";

        public const string Github = "github";

        public const string Stripe = "stripe";

        public const string Afdian = "afdian";
    }

    public static class DiscordPaginationParams {
        public const int Ttl = 3 * 60; // 3 minutes

        public const int ItemsPerPage = 10;
    }
}
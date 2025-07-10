namespace Eevee.Sleep.Bot.Utils;

public static class GlobalConst {
    public static class SubscriptionSource {
        public const string Discord = "discord";

        public const string DiscordOneTime = "discordOneTime";

        public const string Patreon = "patreon";

        public const string Github = "github";

        public const string Afdian = "afdian";
    }

    public static class DiscordPaginationParams {
        public const int Ttl = 3 * 60; // 3 minutes

        public const int ItemsPerPage = 4;
    }
}
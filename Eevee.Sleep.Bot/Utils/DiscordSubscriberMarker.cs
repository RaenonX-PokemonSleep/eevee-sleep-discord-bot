using Discord;

namespace Eevee.Sleep.Bot.Utils;

public static class DiscordSubscriberMarker {
    public static async Task MarkUserSubscribed(IUser user) {
        if (user is IGuildUser serverUser) {
            await serverUser.AddRoleAsync(ConfigHelper.GetDiscordSubscriberRoleId());
        }
    }
    
    public static async Task MarkUserUnsubscribed(IUser user) {
        if (user is IGuildUser serverUser) {
            await serverUser.RemoveRoleAsync(ConfigHelper.GetDiscordSubscriberRoleId());
        }
    }
}
using System.Collections.Concurrent;
using Eevee.Sleep.Bot.Models;

namespace Eevee.Sleep.Bot.Utils;

public record RoleEventPendingData(
    List<RoleEventEntry> FreeEntries,
    List<RoleEventEntry> SubscriberEntries,
    string Designer,
    long ExpiryEpoch,
    bool OmitLangRoles
);

public static class RoleEventPendingStore {
    private static readonly ConcurrentDictionary<ulong, (RoleEventPendingData Data, DateTime CreatedAt)> Store = new();

    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public static void Set(ulong userId, RoleEventPendingData data) {
        Cleanup();
        Store[userId] = (data, DateTime.UtcNow);
    }

    public static RoleEventPendingData? Get(ulong userId) {
        if (!Store.TryGetValue(userId, out var entry)) {
            return null;
        }

        if (DateTime.UtcNow - entry.CreatedAt <= Ttl) {
            return entry.Data;
        }

        Store.TryRemove(userId, out _);
        return null;

    }

    public static void Remove(ulong userId) {
        Store.TryRemove(userId, out _);
    }

    private static void Cleanup() {
        var now = DateTime.UtcNow;

        foreach (var (key, entry) in Store) {
            if (now - entry.CreatedAt > Ttl) {
                Store.TryRemove(key, out _);
            }
        }
    }
}

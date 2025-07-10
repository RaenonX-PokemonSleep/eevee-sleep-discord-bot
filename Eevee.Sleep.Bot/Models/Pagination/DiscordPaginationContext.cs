using Eevee.Sleep.Bot.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace Eevee.Sleep.Bot.Models.Pagination;

public static class DiscordPaginationContext<T> where T : class {
    // ReSharper disable once StaticMemberInGenericType
    private static readonly MemoryCache Cache = new(new MemoryCacheOptions());

    public static void SaveState(string key, PaginationState<T> state, TimeSpan ttl) {
        Cache.Set(
            key,
            state,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }
        );
    }

    public static PaginationState<T>? GetState(string key) {
        return Cache.TryGetValue(key, out var state) ? state as PaginationState<T> : null;
    }

    public static void RemoveState(string key) {
        Cache.Remove(key);
    }

    public static void GotoNextPage(string key) {
        var state = GetState(key);
        if (state == null) {
            return;
        }

        if (state.CurrentPage >= state.TotalPages) {
            return;
        }

        state.CurrentPage++;
        SaveState(key, state, TimeSpan.FromSeconds(GlobalConst.DiscordPaginationParams.Ttl));
    }

    public static void GotoPreviousPage(string key) {
        var state = GetState(key);
        if (state == null) {
            return;
        }

        if (state.CurrentPage <= 1) {
            return;
        }

        state.CurrentPage--;
        SaveState(key, state, TimeSpan.FromSeconds(GlobalConst.DiscordPaginationParams.Ttl));
    }

    public static void CleanupExpiredStates() {
        Cache.Compact(0);
    }
}
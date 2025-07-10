namespace Eevee.Sleep.Bot.Models.Pagination;

using Eevee.Sleep.Bot.Utils;
using Microsoft.Extensions.Caching.Memory;

public static class DiscordPaginationContext<T> where T : class {
    private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public static void SaveState(string key, PaginationState<T> state, TimeSpan ttl) {
        _cache.Set(key, state, new MemoryCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = ttl
        });
    }

    public static PaginationState<T>? GetState(string key) {
        return _cache.TryGetValue(key, out var state) ? state as PaginationState<T> : null;
    }

    public static void RemoveState(string key) {
        _cache.Remove(key);
    }

    public static void GotoNextPage(string key) {
        var state = GetState(key);
        if (state != null) {
            if (state.CurrentPage < state.TotalPages) {
                state.CurrentPage++;
                SaveState(key, state, TimeSpan.FromSeconds(GlobalConst.DiscordPaginationParams.ttl));
            }
        }
    }

    public static void GotoPreviousPage(string key) {
        var state = GetState(key);
        if (state != null) {
            if (state.CurrentPage > 1) {
                state.CurrentPage--;
                SaveState(key, state, TimeSpan.FromSeconds(GlobalConst.DiscordPaginationParams.ttl));
            }
        }
    }

    public static void CleanupExpiredStates() {
        _cache.Compact(0);
    }
}

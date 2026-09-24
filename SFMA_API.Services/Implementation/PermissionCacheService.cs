using Microsoft.Extensions.Caching.Memory;
using SFMA_API.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFMA_API.Services.Implementation
{
    public class PermissionCacheService : IPermissionCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ConcurrentDictionary<string, byte> _trackedUserIds = new();
        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(15);

        public PermissionCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public async Task<HashSet<string>> GetUserPermissionsAsync(string userId, Func<Task<HashSet<string>>> factory)
        {
            string cacheKey = $"user_permissions_{userId}";
            if (_memoryCache.TryGetValue(cacheKey, out HashSet<string>? cached) && cached != null)
            {
                return cached;
            }

            var permissions = await factory();
            _trackedUserIds.TryAdd(userId, 0);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DefaultCacheDuration,
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };

            _memoryCache.Set(cacheKey, permissions, cacheOptions);
            return permissions;
        }

        public void InvalidateUserPermissions(string userId)
        {
            string cacheKey = $"user_permissions_{userId}";
            _memoryCache.Remove(cacheKey);
            _trackedUserIds.TryRemove(userId, out _);
        }

        public void InvalidateAll()
        {
            foreach (var userId in _trackedUserIds.Keys)
            {
                _memoryCache.Remove($"user_permissions_{userId}");
            }
            _trackedUserIds.Clear();
        }
    }
}

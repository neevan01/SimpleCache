using System;
using System.Threading;
using System.Threading.Tasks;
using SimpleCache.Abstractions;

namespace SimpleCache.Extensions
{
    /// <summary>
    /// Fluent extension methods for ISimpleCache
    /// </summary>
    public static class SimpleCacheExtensions
    {
        /// <summary>
        /// Get or set with synchronous factory
        /// </summary>
        public static Task<T> GetOrSetAsync<T>(this ISimpleCache cache, string key, Func<T> factory, TimeSpan? expiration = null)
        {
            return cache.GetOrSetAsync(key, () => Task.FromResult(factory()), expiration);
        }

        /// <summary>
        /// Set with default expiration
        /// </summary>
        public static Task<bool> SetAsync<T>(this ISimpleCache cache, string key, T value)
        {
            return cache.SetAsync(key, value, null);
        }

        /// <summary>
        /// Get value or default
        /// </summary>
        public static async Task<T?> GetOrDefaultAsync<T>(this ISimpleCache cache, string key, T? defaultValue = default, CancellationToken cancellationToken = default)
        {
            var result = await cache.GetAsync<T>(key, cancellationToken);
            return result.Success && result.Value != null ? result.Value : defaultValue;
        }

        /// <summary>
        /// Try get value
        /// </summary>
        public static async Task<(bool success, T? value)> TryGetAsync<T>(this ISimpleCache cache, string key, CancellationToken cancellationToken = default)
        {
            var result = await cache.GetAsync<T>(key, cancellationToken);
            return (result.Success && result.FromCache, result.Value);
        }
    }
}

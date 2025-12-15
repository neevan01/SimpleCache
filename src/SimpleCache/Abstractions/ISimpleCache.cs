using SimpleCache.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleCache.Abstractions
{
    /// <summary>
    /// Main cache service interface
    /// </summary>
    public interface ISimpleCache
    {
        /// <summary>
        /// Get or set a cache value with automatic factory execution
        /// </summary>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a value from cache
        /// </summary>
        Task<CacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Set a value in cache
        /// </summary>
        Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove a value from cache
        /// </summary>
        Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a key exists in cache
        /// </summary>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear all cache entries (use with caution)
        /// </summary>
        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}

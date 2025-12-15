using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleCache.Abstractions
{
    /// <summary>
    /// Interface for cache provider implementations (Memory, Redis, etc.)
    /// </summary>
    public interface ICacheProvider
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task<bool> SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default);
        Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}

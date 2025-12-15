using System;
using System.Threading;
using System.Threading.Tasks;
using SimpleCache.Abstractions;
using Microsoft.Extensions.Logging;
using SimpleCache.Models;

namespace SimpleCache
{
    /// <summary>
    /// Main cache service implementation with logging and error handling
    /// </summary>
    public class SimpleCacheService : ISimpleCache
    {
        private readonly ICacheProvider _provider;
        private readonly CacheOptions _options;
        private readonly ILogger<SimpleCacheService>? _logger;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public SimpleCacheService(ICacheProvider provider, CacheOptions? options = null, ILogger<SimpleCacheService>? logger = null)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _options = options ?? new CacheOptions();
            _logger = logger;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var fullKey = BuildKey(key);

            try
            {
                // Try to get from cache first
                var cached = await _provider.GetAsync<T>(fullKey, cancellationToken);
                if (cached != null)
                {
                    _logger?.LogDebug("Cache hit for key: {Key}", fullKey);
                    return cached;
                }

                _logger?.LogDebug("Cache miss for key: {Key}", fullKey);

                // Use lock to prevent cache stampede
                await _lock.WaitAsync(cancellationToken);
                try
                {
                    // Double-check after acquiring lock
                    cached = await _provider.GetAsync<T>(fullKey, cancellationToken);
                    if (cached != null)
                        return cached;

                    // Execute factory and cache result
                    var value = await factory();
                    var exp = expiration ?? _options.DefaultExpiration;
                    
                    var setSuccess = await _provider.SetAsync(fullKey, value, exp, cancellationToken);
                    if (setSuccess)
                    {
                        _logger?.LogInformation("Cached value for key: {Key}, expiration: {Expiration}", fullKey, exp);
                    }
                    else
                    {
                        _logger?.LogWarning("Failed to cache value for key: {Key}", fullKey);
                    }

                    return value;
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in GetOrSetAsync for key: {Key}", fullKey);
                
                // On error, execute factory without caching
                return await factory();
            }
        }

        public async Task<CacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return CacheResult<T>.Error("Cache key cannot be null or empty");

            var fullKey = BuildKey(key);

            try
            {
                var value = await _provider.GetAsync<T>(fullKey, cancellationToken);
                
                if (value != null)
                {
                    _logger?.LogDebug("Cache hit for key: {Key}", fullKey);
                    return CacheResult<T>.Hit(value);
                }

                _logger?.LogDebug("Cache miss for key: {Key}", fullKey);
                return CacheResult<T>.Miss(default!);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting cache value for key: {Key}", fullKey);
                return CacheResult<T>.Error(ex.Message);
            }
        }

        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger?.LogWarning("Attempted to set cache with null or empty key");
                return false;
            }

            var fullKey = BuildKey(key);
            var exp = expiration ?? _options.DefaultExpiration;

            try
            {
                var success = await _provider.SetAsync(fullKey, value, exp, cancellationToken);
                
                if (success)
                    _logger?.LogInformation("Set cache value for key: {Key}, expiration: {Expiration}", fullKey, exp);
                else
                    _logger?.LogWarning("Failed to set cache value for key: {Key}", fullKey);

                return success;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting cache value for key: {Key}", fullKey);
                return false;
            }
        }

        public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            var fullKey = BuildKey(key);

            try
            {
                var success = await _provider.RemoveAsync(fullKey, cancellationToken);
                
                if (success)
                    _logger?.LogInformation("Removed cache value for key: {Key}", fullKey);
                else
                    _logger?.LogWarning("Failed to remove cache value for key: {Key}", fullKey);

                return success;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing cache value for key: {Key}", fullKey);
                return false;
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            var fullKey = BuildKey(key);

            try
            {
                return await _provider.ExistsAsync(fullKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking cache existence for key: {Key}", fullKey);
                return false;
            }
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _provider.ClearAsync(cancellationToken);
                _logger?.LogWarning("Cache cleared");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error clearing cache");
            }
        }

        private string BuildKey(string key)
        {
            return string.IsNullOrWhiteSpace(_options.KeyPrefix) 
                ? key 
                : $"{_options.KeyPrefix}:{key}";
        }
    }
}

using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using SimpleCache.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleCache.Providers
{
    /// <summary>
    /// In-memory cache provider using IMemoryCache
    /// </summary>
    public class MemoryCacheProvider : ICacheProvider
    {
        private readonly IMemoryCache _memoryCache;
        private readonly bool _enableSerialization;
        private readonly bool _useSlidingExpiration;

        public MemoryCacheProvider(IMemoryCache memoryCache, bool enableSerialization = true, bool useSlidingExpiration = false)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _enableSerialization = enableSerialization;
            _useSlidingExpiration = useSlidingExpiration;
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out var value))
                {
                    if (_enableSerialization && value is string json)
                    {
                        return Task.FromResult(JsonConvert.DeserializeObject<T>(json));
                    }
                    return Task.FromResult((T?)value);
                }
                return Task.FromResult(default(T));
            }
            catch
            {
                return Task.FromResult(default(T));
            }
        }

        public Task<bool> SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
        {
            try
            {
                var options = new MemoryCacheEntryOptions();
                if (_useSlidingExpiration)
                {
                    options.SlidingExpiration = expiration;
                }
                else
                {
                    options.AbsoluteExpirationRelativeToNow = expiration;
                }
                if (_enableSerialization)
                {
                    var json = JsonConvert.SerializeObject(value);
                    _memoryCache.Set(key, json, options);
                }
                else
                {
                    _memoryCache.Set(key, value, options);
                }

                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                _memoryCache.Remove(key);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_memoryCache.TryGetValue(key, out _));
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            // IMemoryCache doesn't support clearing all entries
            // This would require tracking keys separately
            return Task.CompletedTask;
        }
    }
}

using SimpleCache;
using SimpleCache.Models;
using SimpleCache.Providers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;
using Xunit;

namespace SimpleCache.Tests
{
    public class KeyPrefixTests
    {
        [Fact]
        public async Task GetOrSetAsync_WithKeyPrefix_UsesPrefix()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var provider = new MemoryCacheProvider(memoryCache);
            var options = new CacheOptions { KeyPrefix = "myapp" };
            var cache = new SimpleCacheService(provider, options);

            // Act
            var result = await cache.GetOrSetAsync("user:1", async () => "John");

            // Assert - verify the prefixed key exists in underlying cache
            var exists = await provider.ExistsAsync("myapp:user:1");
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task GetOrSetAsync_WithoutKeyPrefix_UsesRawKey()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var provider = new MemoryCacheProvider(memoryCache);
            var options = new CacheOptions { KeyPrefix = null };
            var cache = new SimpleCacheService(provider, options);

            // Act
            var result = await cache.GetOrSetAsync("user:1", async () => "John");

            // Assert - verify the raw key exists
            var exists = await provider.ExistsAsync("user:1");
            exists.Should().BeTrue();
        }
    }
}

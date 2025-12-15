using System;
using System.Threading.Tasks;
using SimpleCache.Providers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace SimpleCache.Tests
{
    public class MemoryCacheProviderTests
    {
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheProvider _provider;

        public MemoryCacheProviderTests()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _provider = new MemoryCacheProvider(_memoryCache);
        }

        [Fact]
        public async Task SetAsync_AndGetAsync_StoresAndRetrievesValue()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";

            // Act
            var setResult = await _provider.SetAsync(key, value, TimeSpan.FromMinutes(5));
            var getValue = await _provider.GetAsync<string>(key);

            // Assert
            setResult.Should().BeTrue();
            getValue.Should().Be(value);
        }

        [Fact]
        public async Task GetAsync_WithNonExistentKey_ReturnsNull()
        {
            // Act
            var result = await _provider.GetAsync<string>("non-existent");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task SetAsync_WithComplexObject_SerializesAndDeserializes()
        {
            // Arrange
            var key = "user-key";
            var user = new TestUser { Id = 1, Name = "John", Email = "john@test.com" };

            // Act
            await _provider.SetAsync(key, user, TimeSpan.FromMinutes(5));
            var retrieved = await _provider.GetAsync<TestUser>(key);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(user.Id);
            retrieved.Name.Should().Be(user.Name);
            retrieved.Email.Should().Be(user.Email);
        }

        [Fact]
        public async Task RemoveAsync_RemovesValueFromCache()
        {
            // Arrange
            var key = "test-key";
            await _provider.SetAsync(key, "value", TimeSpan.FromMinutes(5));

            // Act
            var removeResult = await _provider.RemoveAsync(key);
            var getValue = await _provider.GetAsync<string>(key);

            // Assert
            removeResult.Should().BeTrue();
            getValue.Should().BeNull();
        }

        [Fact]
        public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
        {
            // Arrange
            var key = "test-key";
            await _provider.SetAsync(key, "value", TimeSpan.FromMinutes(5));

            // Act
            var exists = await _provider.ExistsAsync(key);

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WhenKeyDoesNotExist_ReturnsFalse()
        {
            // Act
            var exists = await _provider.ExistsAsync("non-existent");

            // Assert
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task SetAsync_WithoutSerialization_StoresRawValue()
        {
            // Arrange
            var provider = new MemoryCacheProvider(_memoryCache, enableSerialization: false);
            var key = "test-key";
            var value = "test-value";

            // Act
            await provider.SetAsync(key, value, TimeSpan.FromMinutes(5));
            var retrieved = await provider.GetAsync<string>(key);

            // Assert
            retrieved.Should().Be(value);
        }

        private class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }
    }
}

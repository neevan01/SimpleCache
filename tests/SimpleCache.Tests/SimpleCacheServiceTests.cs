using System;
using System.Threading.Tasks;
using SimpleCache.Abstractions;
using FluentAssertions;
using Moq;
using SimpleCache.Models;
using Xunit;
using SimpleCache;

namespace SimpleCache.Tests
{
    public class SimpleCacheServiceTests
    {
        private readonly Mock<ICacheProvider> _mockProvider;
        private readonly SimpleCacheService _cache;

        public SimpleCacheServiceTests()
        {
            _mockProvider = new Mock<ICacheProvider>();
            _cache = new SimpleCacheService(_mockProvider.Object);
        }

        [Fact]
        public async Task GetOrSetAsync_WhenCacheHit_ReturnsFromCache()
        {
            // Arrange
            var key = "test-key";
            var cachedValue = "cached-data";
            _mockProvider.Setup(p => p.GetAsync<string>(key, default))
                .ReturnsAsync(cachedValue);

            var factoryCalled = false;

            // Act
            var result = await _cache.GetOrSetAsync(key, async () =>
            {
                factoryCalled = true;
                return "new-data";
            });

            // Assert
            result.Should().Be(cachedValue);
            factoryCalled.Should().BeFalse();
            _mockProvider.Verify(p => p.GetAsync<string>(key, default), Times.Once);
        }

        [Fact]
        public async Task GetOrSetAsync_WhenCacheMiss_ExecutesFactoryAndCaches()
        {
            // Arrange
            var key = "test-key";
            var newValue = "new-data";
            _mockProvider.Setup(p => p.GetAsync<string>(key, default))
                .ReturnsAsync((string?)null);
            _mockProvider.Setup(p => p.SetAsync(key, newValue, It.IsAny<TimeSpan>(), default))
                .ReturnsAsync(true);

            // Act
            var result = await _cache.GetOrSetAsync(key, async () => newValue);

            // Assert
            result.Should().Be(newValue);
            _mockProvider.Verify(p => p.SetAsync(key, newValue, It.IsAny<TimeSpan>(), default), Times.Once);
        }

        [Fact]
        public async Task GetOrSetAsync_WithNullKey_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _cache.GetOrSetAsync("", async () => "value"));
        }

        [Fact]
        public async Task GetOrSetAsync_WithNullFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _cache.GetOrSetAsync<string>("key", null!));
        }

        [Fact]
        public async Task GetAsync_WhenCacheHit_ReturnsSuccessResult()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            _mockProvider.Setup(p => p.GetAsync<string>(key, default))
                .ReturnsAsync(value);

            // Act
            var result = await _cache.GetAsync<string>(key);

            // Assert
            result.Success.Should().BeTrue();
            result.FromCache.Should().BeTrue();
            result.Value.Should().Be(value);
        }

        [Fact]
        public async Task GetAsync_WhenCacheMiss_ReturnsMissResult()
        {
            // Arrange
            var key = "test-key";
            _mockProvider.Setup(p => p.GetAsync<string>(key, default))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _cache.GetAsync<string>(key);

            // Assert
            result.Success.Should().BeTrue();
            result.FromCache.Should().BeFalse();
        }

        [Fact]
        public async Task GetAsync_WithEmptyKey_ReturnsErrorResult()
        {
            // Act
            var result = await _cache.GetAsync<string>("");

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("key cannot be null or empty");
        }

        [Fact]
        public async Task SetAsync_WithValidData_ReturnsTrue()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            _mockProvider.Setup(p => p.SetAsync(key, value, It.IsAny<TimeSpan>(), default))
                .ReturnsAsync(true);

            // Act
            var result = await _cache.SetAsync(key, value);

            // Assert
            result.Should().BeTrue();
            _mockProvider.Verify(p => p.SetAsync(key, value, It.IsAny<TimeSpan>(), default), Times.Once);
        }

        [Fact]
        public async Task SetAsync_WithEmptyKey_ReturnsFalse()
        {
            // Act
            var result = await _cache.SetAsync("", "value");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task RemoveAsync_WithValidKey_ReturnsTrue()
        {
            // Arrange
            var key = "test-key";
            _mockProvider.Setup(p => p.RemoveAsync(key, default))
                .ReturnsAsync(true);

            // Act
            var result = await _cache.RemoveAsync(key);

            // Assert
            result.Should().BeTrue();
            _mockProvider.Verify(p => p.RemoveAsync(key, default), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
        {
            // Arrange
            var key = "test-key";
            _mockProvider.Setup(p => p.ExistsAsync(key, default))
                .ReturnsAsync(true);

            // Act
            var result = await _cache.ExistsAsync(key);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WhenKeyDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var key = "test-key";
            _mockProvider.Setup(p => p.ExistsAsync(key, default))
                .ReturnsAsync(false);

            // Act
            var result = await _cache.ExistsAsync(key);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetOrSetAsync_WithCustomExpiration_UsesProvidedExpiration()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            var expiration = TimeSpan.FromMinutes(10);
            _mockProvider.Setup(p => p.GetAsync<string>(key, default))
                .ReturnsAsync((string?)null);

            // Act
            await _cache.GetOrSetAsync(key, async () => value, expiration);

            // Assert
            _mockProvider.Verify(p => p.SetAsync(key, value, expiration, default), Times.Once);
        }
    }
}

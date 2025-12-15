using System;
using FluentAssertions;
using SimpleCache.Models;
using Xunit;

namespace SimpleCache.Tests
{
    public class CacheOptionsTests
    {
        [Fact]
        public void CacheOptions_HasCorrectDefaults()
        {
            // Arrange & Act
            var options = new CacheOptions();

            // Assert
            options.DefaultExpiration.Should().Be(TimeSpan.FromMinutes(5));
            options.EnableSerialization.Should().BeTrue();
            options.KeyPrefix.Should().BeNull();
            options.EnableStatistics.Should().BeFalse();
        }

        [Fact]
        public void CacheOptions_CanBeCustomized()
        {
            // Arrange & Act
            var options = new CacheOptions
            {
                DefaultExpiration = TimeSpan.FromMinutes(30),
                EnableSerialization = false,
                KeyPrefix = "myapp",
                EnableStatistics = true
            };

            // Assert
            options.DefaultExpiration.Should().Be(TimeSpan.FromMinutes(30));
            options.EnableSerialization.Should().BeFalse();
            options.KeyPrefix.Should().Be("myapp");
            options.EnableStatistics.Should().BeTrue();
        }
    }
}

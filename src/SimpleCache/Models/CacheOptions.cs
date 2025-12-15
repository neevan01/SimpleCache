using System;

namespace SimpleCache.Models
{
    /// <summary>
    /// Configuration options for cache behavior
    /// </summary>
    public class CacheOptions
    {
        /// <summary>
        /// Default expiration time for cache entries (default: 5 minutes)
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Enable automatic serialization/deserialization (default: true)
        /// </summary>
        public bool EnableSerialization { get; set; } = true;

        /// <summary>
        /// Cache key prefix for namespacing (default: empty)
        /// </summary>
        public string? KeyPrefix { get; set; }

        /// <summary>
        /// Enable cache statistics tracking (default: false)
        /// </summary>
        public bool EnableStatistics { get; set; } = false;
    }
}

namespace SimpleCache.Models
{
    /// <summary>
    /// Represents the result of a cache operation
    /// </summary>
    /// <typeparam name="T">Type of cached value</typeparam>
    public class CacheResult<T>
    {
        public bool Success { get; set; }
        public T? Value { get; set; }
        public bool FromCache { get; set; }
        public string? ErrorMessage { get; set; }

        public static CacheResult<T> Hit(T value)
        {
            return new CacheResult<T> { Success = true, Value = value, FromCache = true };
        }

        public static CacheResult<T> Miss(T value)
        {
            return new CacheResult<T> { Success = true, Value = value, FromCache = false };
        }

        public static CacheResult<T> Error(string errorMessage)
        {
            return new CacheResult<T> { Success = false, ErrorMessage = errorMessage };
        }
    }
}
